namespace WediFrame.Modules.Partners.Domain;

/// <summary>
/// A discount code issued to a partner. The couple enters it at checkout (P2) to get
/// a discount; each successful paid checkout increments <see cref="RedemptionCount"/>
/// and attributes the purchase to the partner (that wiring lands in P2). Discount is
/// modelled generically (type + value) so the owner sets concrete terms per code.
/// Cross-module references are plain Guids — no FK/navigation (monolith boundaries).
/// </summary>
public sealed class BonusCode
{
    public Guid Id { get; set; }

    public Guid PartnerId { get; set; }

    /// <summary>Human-facing code, stored upper-cased and unique.</summary>
    public required string Code { get; set; }

    public DiscountType DiscountType { get; set; } = DiscountType.Percentage;

    /// <summary>Percentage (1–100) when <see cref="DiscountType.Percentage"/>, else minor units (cents).</summary>
    public int DiscountValue { get; set; }

    /// <summary>Max successful redemptions; null = unlimited.</summary>
    public int? MaxRedemptions { get; set; }

    /// <summary>Last day the code is valid (inclusive); null = no expiry.</summary>
    public DateOnly? ExpiresAt { get; set; }

    /// <summary>Successful paid redemptions so far (incremented in P2).</summary>
    public int RedemptionCount { get; set; }

    /// <summary>Admin kill-switch; an inactive code is rejected at checkout regardless of limits.</summary>
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>How a bonus code's discount is computed.</summary>
public enum DiscountType
{
    /// <summary>Percentage off the package price (DiscountValue = 1–100).</summary>
    Percentage = 0,

    /// <summary>Fixed amount off, in minor units/cents (DiscountValue = cents).</summary>
    FixedAmount = 1,
}
