namespace WediFrame.Shared.Partners;

/// <summary>
/// Checkout-facing bonus-code operations. Implemented by the Partners module (owns
/// BonusCode), consumed by Events (validate at checkout start) and Billing (redeem on
/// paid webhook) through this Shared contract — neither references Partners directly.
///
/// One code per purchase (no stacking — there is exactly one package purchase per
/// event). Discount is computed against the package price; percentage is floored to
/// whole cents; the discount never exceeds the price.
/// </summary>
public interface IBonusCodeService
{
    /// <summary>Validate a code against a package price and compute the discount.</summary>
    Task<BonusCodeValidation> ValidateAsync(string code, int packagePriceCents, CancellationToken ct);

    /// <summary>Count one successful redemption (called once on the paid transition).</summary>
    Task RedeemAsync(Guid bonusCodeId, CancellationToken ct);
}

/// <summary>Why a code was accepted or rejected.</summary>
public enum BonusCodeOutcome
{
    Ok = 0,
    NotFound = 1,
    Inactive = 2,
    Expired = 3,
    Exhausted = 4,
}

/// <summary>
/// Result of validating a code. Discount fields are populated only on
/// <see cref="BonusCodeOutcome.Ok"/>. <see cref="ApproxPercent"/> is a display-only
/// rounded percentage (the couple sees "~20% off" even for a fixed-amount code).
/// </summary>
public sealed record BonusCodeValidation(
    BonusCodeOutcome Outcome,
    Guid? BonusCodeId,
    string? Code,
    string? DiscountType,
    int DiscountCents,
    int FinalCents,
    int ApproxPercent);
