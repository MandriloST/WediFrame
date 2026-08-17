using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WediFrame.Modules.Billing.Domain;

namespace WediFrame.Modules.Billing.Services;

/// <summary>What Events passes to start a paid checkout (Events resolves ownership + package).</summary>
public sealed record CheckoutStart(
    Guid EventId,
    Guid PackageId,
    int AmountCents,
    string Currency,
    string Description,
    bool NeedsR1,
    string? CompanyName,
    string? CompanyOib,
    string? CompanyAddress,
    string SuccessUrl,
    string CancelUrl,
    Guid? BonusCodeId = null,
    int DiscountCents = 0);

public sealed record CheckoutResult(Guid PurchaseId, string Url);

/// <summary>What the caller (Events) needs to activate the event once paid and
/// to send the purchase confirmation email.</summary>
public sealed record CheckoutOutcome(
    Guid EventId,
    Guid PackageId,
    int AmountCents,
    string Currency,
    string? InvoiceNumber);

/// <summary>
/// Cross-module checkout PORT consumed by Events. Encapsulates the Purchase record,
/// the payment gateway and fiscalization — so Events only orchestrates activation
/// and never touches Stripe/Parra/Purchase directly (module boundary; no cycle).
/// </summary>
public interface ICheckoutService
{
    Task<CheckoutResult> StartAsync(CheckoutStart request, CancellationToken ct = default);

    /// <summary>Verify + process a gateway webhook. Returns the event to activate, or null.</summary>
    Task<CheckoutOutcome?> HandleWebhookAsync(string payload, string signatureHeader, CancellationToken ct = default);
}

public sealed class CheckoutService(
    DbContext db,
    IPaymentGateway payments,
    IFiscalizationService fiscalization,
    WediFrame.Shared.Partners.IBonusCodeService bonusCodes,
    TimeProvider clock,
    ILogger<CheckoutService> logger) : ICheckoutService
{
    public async Task<CheckoutResult> StartAsync(CheckoutStart r, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow();

        var purchase = new Purchase
        {
            Id = Guid.NewGuid(),
            EventId = r.EventId,
            PackageId = r.PackageId,
            AmountCents = r.AmountCents,
            Currency = r.Currency,
            BonusCodeId = r.BonusCodeId,
            DiscountCents = r.DiscountCents,
            Status = PurchaseStatus.Pending,
            PaymentProvider = payments.Provider,
            NeedsR1 = r.NeedsR1,
            CompanyName = r.CompanyName,
            CompanyOib = r.CompanyOib,
            CompanyAddress = r.CompanyAddress,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Set<Purchase>().Add(purchase);
        await db.SaveChangesAsync(ct);

        var checkout = await payments.CreateCheckoutAsync(
            new PaymentCheckoutRequest(
                purchase.Id, r.AmountCents, r.Currency, r.Description, r.SuccessUrl, r.CancelUrl),
            ct);

        purchase.PaymentReference = checkout.Reference;
        purchase.UpdatedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);

        return new CheckoutResult(purchase.Id, checkout.Url);
    }

    public async Task<CheckoutOutcome?> HandleWebhookAsync(
        string payload, string signatureHeader, CancellationToken ct = default)
    {
        var result = payments.ParseWebhook(payload, signatureHeader);
        if (result is null || !result.Paid)
        {
            return null;
        }

        var purchase = await db.Set<Purchase>()
            .SingleOrDefaultAsync(p => p.Id == result.PurchaseId, ct);

        if (purchase is null)
        {
            logger.LogWarning("Stripe webhook for unknown purchase {PurchaseId}", result.PurchaseId);
            return null;
        }

        // Idempotent: Stripe may deliver the same event more than once.
        if (purchase.Status == PurchaseStatus.Paid)
        {
            return new CheckoutOutcome(
                purchase.EventId, purchase.PackageId,
                purchase.AmountCents, purchase.Currency, purchase.FiscalInvoiceNumber);
        }

        purchase.Status = PurchaseStatus.Paid;
        purchase.PaymentReference = result.Reference;
        purchase.UpdatedAt = clock.GetUtcNow();

        // Count the bonus-code redemption exactly once, on the Pending→Paid transition
        // (repeated already-Paid webhooks returned above). Best-effort: a failure here
        // must not block activation — the couple paid.
        if (purchase.BonusCodeId is { } bonusCodeId)
        {
            try
            {
                await bonusCodes.RedeemAsync(bonusCodeId, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Bonus code redemption failed for purchase {PurchaseId}", purchase.Id);
            }
        }

        // Fiscalize (manual default / Parra when finalized). A failure is logged but
        // must NOT block activation — the couple paid; the invoice can be reissued.
        try
        {
            var fr = await fiscalization.IssueInvoiceAsync(
                new FiscalizationRequest(
                    purchase.Id, purchase.AmountCents, purchase.Currency,
                    "WediFrame paket", purchase.UpdatedAt,
                    purchase.NeedsR1, purchase.CompanyName, purchase.CompanyOib, purchase.CompanyAddress),
                ct);

            purchase.FiscalProvider = fr.Provider;
            purchase.FiscalStatus = fr.Status.ToString().ToLowerInvariant();
            purchase.FiscalInvoiceNumber = fr.InvoiceNumber;
            purchase.FiscalJir = fr.Jir;
            purchase.FiscalZki = fr.Zki;
            purchase.FiscalizedAt = fr.IssuedAt;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fiscalization failed for purchase {PurchaseId}", purchase.Id);
            purchase.FiscalStatus = "failed";
        }

        await db.SaveChangesAsync(ct);
        return new CheckoutOutcome(
            purchase.EventId, purchase.PackageId,
            purchase.AmountCents, purchase.Currency, purchase.FiscalInvoiceNumber);
    }
}
