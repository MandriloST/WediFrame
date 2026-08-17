using Microsoft.EntityFrameworkCore;
using WediFrame.Modules.Partners.Domain;
using WediFrame.Shared.Partners;

namespace WediFrame.Modules.Partners.Services;

/// <summary>
/// Partners-side implementation of <see cref="IBonusCodeService"/>. Resolves a code,
/// checks it's usable (active / not expired / not exhausted) and computes the discount
/// against the package price. Redemption is an atomic increment on the paid webhook.
/// </summary>
public sealed class BonusCodeService(DbContext db, TimeProvider timeProvider) : IBonusCodeService
{
    public async Task<BonusCodeValidation> ValidateAsync(string code, int packagePriceCents, CancellationToken ct)
    {
        var normalized = (code ?? "").Trim().ToUpperInvariant();
        if (normalized.Length == 0)
        {
            return Reject(BonusCodeOutcome.NotFound);
        }

        var bc = await db.Set<BonusCode>().AsNoTracking()
            .SingleOrDefaultAsync(c => c.Code == normalized, ct);

        if (bc is null)
        {
            return Reject(BonusCodeOutcome.NotFound);
        }

        if (!bc.IsActive)
        {
            return Reject(BonusCodeOutcome.Inactive);
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        if (bc.ExpiresAt is { } exp && today > exp)
        {
            return Reject(BonusCodeOutcome.Expired);
        }

        if (bc.MaxRedemptions is { } max && bc.RedemptionCount >= max)
        {
            return Reject(BonusCodeOutcome.Exhausted);
        }

        // Compute discount. Percentage is floored to whole cents; discount is capped
        // at the price so the final amount never goes negative.
        var rawDiscount = bc.DiscountType == DiscountType.Percentage
            ? packagePriceCents * bc.DiscountValue / 100 // integer division floors
            : bc.DiscountValue;

        var discountCents = Math.Clamp(rawDiscount, 0, packagePriceCents);
        var finalCents = packagePriceCents - discountCents;
        var approxPercent = packagePriceCents > 0
            ? (int)Math.Round(discountCents * 100.0 / packagePriceCents, MidpointRounding.AwayFromZero)
            : 0;

        return new BonusCodeValidation(
            BonusCodeOutcome.Ok, bc.Id, bc.Code, bc.DiscountType.ToString(),
            discountCents, finalCents, approxPercent);
    }

    public Task RedeemAsync(Guid bonusCodeId, CancellationToken ct) =>
        db.Set<BonusCode>()
            .Where(c => c.Id == bonusCodeId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.RedemptionCount, c => c.RedemptionCount + 1), ct);

    private static BonusCodeValidation Reject(BonusCodeOutcome outcome) =>
        new(outcome, null, null, null, 0, 0, 0);
}
