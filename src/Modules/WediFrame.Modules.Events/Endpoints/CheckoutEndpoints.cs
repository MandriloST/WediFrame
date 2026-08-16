using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WediFrame.Modules.Billing.Services;
using WediFrame.Modules.Events.Contracts;
using WediFrame.Modules.Events.Domain;
using WediFrame.Modules.Events.Services;
using WediFrame.Shared.Auth;
using WediFrame.Shared.Directory;
using WediFrame.Shared.Email;
using WediFrame.Shared.Options;

namespace WediFrame.Modules.Events.Endpoints;

/// <summary>
/// Paid activation flow. Events orchestrates because it may call Billing (checkout,
/// packages) AND owns event activation — the only cycle-free place for this. Billing
/// keeps the Stripe/Parra/Purchase details behind ICheckoutService.
///   POST /events/{id}/checkout  → create Stripe session, return URL (host, auth)
///   POST /webhooks/stripe       → verify, mark paid, fiscalize, activate (public, signed)
/// </summary>
public static class CheckoutEndpoints
{
    public static IEndpointRouteBuilder MapCheckoutEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/events/{id:guid}/checkout", StartCheckoutAsync).RequireAuthorization();
        endpoints.MapPost("/webhooks/stripe", StripeWebhookAsync); // public; verified by signature
        return endpoints;
    }

    private static async Task<IResult> StartCheckoutAsync(
        Guid id,
        CheckoutRequest request,
        ClaimsPrincipal principal,
        IHostEventAccess hostEvents,
        IPackageCatalog packages,
        ICheckoutService checkout,
        IOptions<FrontendOptions> frontend,
        CancellationToken ct)
    {
        if (principal.GetUserId() is not { } userId)
        {
            return Results.Unauthorized();
        }

        var ev = await hostEvents.FindOwnedAsync(id, userId, ct);
        if (ev is null)
        {
            return Results.NotFound();
        }

        if (ev.Status != EventStatus.Draft)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["status"] = ["events.cannot_checkout"], // only a Draft can be purchased
            });
        }

        if (ev.PackageSlug is null || await packages.GetBySlugAsync(ev.PackageSlug, ct) is not { } package)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["package"] = ["events.package_invalid"],
            });
        }

        if (package.IsFree)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["package"] = ["events.package_free"], // Free activates without payment
            });
        }

        var appBase = frontend.Value.AppBaseUrl.TrimEnd('/');
        var successUrl = $"{appBase}/dashboard/events/{id}?checkout=success";
        var cancelUrl = $"{appBase}/dashboard/events/{id}?checkout=cancel";

        var result = await checkout.StartAsync(
            new CheckoutStart(
                id, package.Id, package.PriceCents, package.Currency, $"WediFrame — {package.Name}",
                request.NeedsR1, Trim(request.CompanyName), Trim(request.CompanyOib), Trim(request.CompanyAddress),
                successUrl, cancelUrl),
            ct);

        return Results.Ok(new CheckoutResponse(result.Url));
    }

    private static async Task<IResult> StripeWebhookAsync(
        HttpRequest httpRequest,
        DbContext db,
        IPackageCatalog packages,
        ICheckoutService checkout,
        IUserDirectory users,
        IEmailSender email,
        IOptions<FrontendOptions> frontend,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("StripeWebhook");

        using var reader = new StreamReader(httpRequest.Body);
        var payload = await reader.ReadToEndAsync(ct);
        var signature = httpRequest.Headers["Stripe-Signature"].ToString();

        CheckoutOutcome? outcome;
        try
        {
            outcome = await checkout.HandleWebhookAsync(payload, signature, ct);
        }
        catch (PaymentSignatureException ex)
        {
            // Bad/forged signature → 400 so Stripe retries (and we see why).
            logger.LogWarning(ex, "Stripe webhook signature verification failed");
            return Results.BadRequest();
        }
        catch (Exception ex)
        {
            // Processing error (DB, etc.) → 500 so Stripe retries with backoff.
            logger.LogError(ex, "Stripe webhook processing failed");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }

        // Not an event we act on (e.g. unpaid, or a type we ignore) — ack so Stripe stops.
        if (outcome is null)
        {
            return Results.Ok();
        }

        // Activate the paid event — Events' own concern — deriving the timeline from the package.
        var entity = await db.Set<Event>()
            .SingleOrDefaultAsync(e => e.Id == outcome.EventId, ct);

        if (entity is not null && entity.Status == EventStatus.Draft)
        {
            entity.Status = EventStatus.Active;
            var package = await packages.GetByIdAsync(outcome.PackageId, ct);
            if (package is { } p)
            {
                entity.UploadEndsAt = entity.UploadStartDate.AddDays(p.UploadPeriodDays);
                entity.ExpiresAt = entity.UploadStartDate.AddDays(p.RetentionDays);
            }

            await db.SaveChangesAsync(ct);

            // Purchase confirmation email. Best-effort: a failure must not fail the
            // webhook (Stripe would retry, but this block runs only on the
            // Draft→Active transition, so the host still gets at most one email).
            try
            {
                if (await users.GetContactAsync(entity.OwnerUserId, ct) is { } contact)
                {
                    var appBase = frontend.Value.AppBaseUrl.TrimEnd('/');
                    var manageUrl = $"{appBase}/dashboard/events/{entity.Id}";
                    var message = PurchaseConfirmationEmail.Build(
                        contact.Language, contact.Email, entity.Title,
                        package?.Name ?? "WediFrame", outcome.AmountCents, outcome.Currency,
                        outcome.InvoiceNumber, manageUrl);
                    await email.SendAsync(message, ct);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Purchase confirmation email failed for event {EventId}", entity.Id);
            }
        }

        return Results.Ok();
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
