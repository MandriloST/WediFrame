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
using WediFrame.Shared.Partners;

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
    /// <summary>Stripe won't create a session below its minimum (~€0.50); a bonus
    /// code may not drop the charge below this. Reject with a clear code instead.</summary>
    private const int MinChargeCents = 50;

    public static IEndpointRouteBuilder MapCheckoutEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/events/{id:guid}/checkout", StartCheckoutAsync).RequireAuthorization();
        endpoints.MapPost("/events/{id:guid}/bonus-code/preview", PreviewBonusCodeAsync).RequireAuthorization();
        endpoints.MapPost("/webhooks/stripe", StripeWebhookAsync); // public; verified by signature
        return endpoints;
    }

    private static async Task<IResult> StartCheckoutAsync(
        Guid id,
        CheckoutRequest request,
        ClaimsPrincipal principal,
        IHostEventAccess hostEvents,
        IPackageCatalog packages,
        IBonusCodeService bonusCodes,
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

        // Optional bonus code: validate + apply discount. One code per purchase.
        var amountCents = package.PriceCents;
        Guid? bonusCodeId = null;
        var discountCents = 0;

        if (!string.IsNullOrWhiteSpace(request.BonusCode))
        {
            var validation = await bonusCodes.ValidateAsync(request.BonusCode, package.PriceCents, ct);
            if (validation.Outcome != BonusCodeOutcome.Ok)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["bonusCode"] = [BonusCodeErrorCode(validation.Outcome)],
                });
            }

            if (validation.FinalCents < MinChargeCents)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["bonusCode"] = ["events.bonus_code_amount_too_low"],
                });
            }

            amountCents = validation.FinalCents;
            discountCents = validation.DiscountCents;
            bonusCodeId = validation.BonusCodeId;
        }

        var appBase = frontend.Value.AppBaseUrl.TrimEnd('/');
        var successUrl = $"{appBase}/dashboard/events/{id}?checkout=success";
        var cancelUrl = $"{appBase}/dashboard/events/{id}?checkout=cancel";

        var result = await checkout.StartAsync(
            new CheckoutStart(
                id, package.Id, amountCents, package.Currency, $"WediFrame — {package.Name}",
                request.NeedsR1, Trim(request.CompanyName), Trim(request.CompanyOib), Trim(request.CompanyAddress),
                successUrl, cancelUrl, bonusCodeId, discountCents),
            ct);

        return Results.Ok(new CheckoutResponse(result.Url));
    }

    /// <summary>
    /// Preview a bonus code against this event's package (before redirecting to Stripe),
    /// so the couple sees the discount, the approximate percentage and the new total.
    /// </summary>
    private static async Task<IResult> PreviewBonusCodeAsync(
        Guid id,
        BonusCodePreviewRequest request,
        ClaimsPrincipal principal,
        IHostEventAccess hostEvents,
        IPackageCatalog packages,
        IBonusCodeService bonusCodes,
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

        if (ev.PackageSlug is null || await packages.GetBySlugAsync(ev.PackageSlug, ct) is not { } package)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["package"] = ["events.package_invalid"],
            });
        }

        var currency = package.Currency;

        if (package.IsFree)
        {
            return Results.Ok(new BonusCodePreviewResponse(
                false, "events.package_free", package.PriceCents, 0, package.PriceCents, 0, currency));
        }

        var validation = await bonusCodes.ValidateAsync(request.Code ?? "", package.PriceCents, ct);
        if (validation.Outcome != BonusCodeOutcome.Ok)
        {
            return Results.Ok(new BonusCodePreviewResponse(
                false, BonusCodeErrorCode(validation.Outcome),
                package.PriceCents, 0, package.PriceCents, 0, currency));
        }

        if (validation.FinalCents < MinChargeCents)
        {
            return Results.Ok(new BonusCodePreviewResponse(
                false, "events.bonus_code_amount_too_low",
                package.PriceCents, validation.DiscountCents, validation.FinalCents,
                validation.ApproxPercent, currency));
        }

        return Results.Ok(new BonusCodePreviewResponse(
            true, null, package.PriceCents, validation.DiscountCents,
            validation.FinalCents, validation.ApproxPercent, currency));
    }

    private static string BonusCodeErrorCode(BonusCodeOutcome outcome) => outcome switch
    {
        BonusCodeOutcome.Expired => "events.bonus_code_expired",
        BonusCodeOutcome.Exhausted => "events.bonus_code_exhausted",
        BonusCodeOutcome.Inactive => "events.bonus_code_inactive",
        _ => "events.bonus_code_invalid",
    };

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
