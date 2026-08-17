namespace WediFrame.Modules.Billing.Domain;

/// <summary>Lifecycle of a paid package purchase.</summary>
public enum PurchaseStatus
{
    /// <summary>Created, checkout started, payment not yet confirmed.</summary>
    Pending = 0,

    /// <summary>Payment confirmed by the gateway (webhook).</summary>
    Paid = 1,

    /// <summary>Payment failed or was declined.</summary>
    Failed = 2,

    /// <summary>Host cancelled / abandoned checkout.</summary>
    Cancelled = 3,

    /// <summary>Refunded after the fact.</summary>
    Refunded = 4,
}

/// <summary>
/// A single paid package purchase for an event (Free/Trial never creates one).
/// Payment (Stripe) and fiscalization (Parra or another provider) are recorded
/// here but performed behind swappable ports — this entity stays provider-agnostic
/// so the fiscalization/payment service can be changed later without a schema change.
///
/// Cross-module references (EventId, PackageId) are plain Guids — no FK/navigation,
/// consistent with the rest of the monolith's module boundaries.
/// </summary>
public sealed class Purchase
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public Guid PackageId { get; set; }

    /// <summary>Charged amount in minor units (cents), snapshot at purchase time.</summary>
    public int AmountCents { get; set; }

    public string Currency { get; set; } = "EUR";

    // ── Bonus code attribution (P2) ─────────────────────────────────────────
    /// <summary>Redeemed bonus code (Partners.BonusCode.Id), or null. Plain Guid — no FK.</summary>
    public Guid? BonusCodeId { get; set; }

    /// <summary>Discount applied in minor units (cents). 0 when no code. AmountCents is post-discount.</summary>
    public int DiscountCents { get; set; }

    public PurchaseStatus Status { get; set; } = PurchaseStatus.Pending;

    // ── Payment (gateway-agnostic) ──────────────────────────────────────────
    /// <summary>e.g. "stripe". Null until a gateway is engaged.</summary>
    public string? PaymentProvider { get; set; }

    /// <summary>Gateway reference (Stripe Checkout Session / PaymentIntent id).</summary>
    public string? PaymentReference { get; set; }

    // ── R1 invoice details (host ticked "Trebam R1" in checkout) ────────────
    public bool NeedsR1 { get; set; }

    public string? CompanyName { get; set; }

    public string? CompanyOib { get; set; }

    public string? CompanyAddress { get; set; }

    // ── Fiscalization (provider-agnostic result) ────────────────────────────
    /// <summary>Which provider fiscalized it, e.g. "manual" or "parra".</summary>
    public string? FiscalProvider { get; set; }

    /// <summary>Issued invoice number (from the fiscalization provider).</summary>
    public string? FiscalInvoiceNumber { get; set; }

    /// <summary>Croatian fiscal identifiers (JIR / ZKI), when available.</summary>
    public string? FiscalJir { get; set; }

    public string? FiscalZki { get; set; }

    /// <summary>When the fiscal invoice was issued (or marked manual).</summary>
    public DateTimeOffset? FiscalizedAt { get; set; }

    /// <summary>
    /// "manual" when an invoice must still be issued by hand (default provider),
    /// "issued" when a provider returned fiscal data, "failed" otherwise. Null
    /// until fiscalization is attempted.
    /// </summary>
    public string? FiscalStatus { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
