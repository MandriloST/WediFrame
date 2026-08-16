namespace WediFrame.Modules.Billing.Services;

/// <summary>Outcome of a fiscalization attempt.</summary>
public enum FiscalizationStatus
{
    /// <summary>A provider issued a fiscal invoice (JIR/ZKI/number available).</summary>
    Issued = 0,

    /// <summary>No automated provider — an invoice must be issued by hand (default).</summary>
    Manual = 1,

    /// <summary>Provider was engaged but failed.</summary>
    Failed = 2,
}

/// <summary>
/// Provider-agnostic fiscalization request. Built by Billing from a paid Purchase;
/// contains only what any HR fiscalization provider needs, so the concrete service
/// (Parra now, something else later, or our own) can be swapped without touching callers.
/// </summary>
public sealed record FiscalizationRequest(
    Guid PurchaseId,
    int AmountCents,
    string Currency,
    string Description,
    DateTimeOffset IssuedAt,
    bool IsCompany,
    string? BuyerName,
    string? BuyerOib,
    string? BuyerAddress);

/// <summary>
/// Provider-agnostic fiscalization result. Croatian fiscal identifiers (JIR/ZKI)
/// and the invoice number are optional because the "manual" provider returns none.
/// </summary>
public sealed record FiscalizationResult(
    FiscalizationStatus Status,
    string Provider,
    string? InvoiceNumber,
    string? Jir,
    string? Zki,
    DateTimeOffset? IssuedAt,
    string? Error)
{
    public static FiscalizationResult ManualPending(string provider, DateTimeOffset at) =>
        new(FiscalizationStatus.Manual, provider, null, null, null, at, null);

    public static FiscalizationResult Fail(string provider, string error) =>
        new(FiscalizationStatus.Failed, provider, null, null, null, null, error);
}

/// <summary>
/// The fiscalization PORT. Everything downstream depends on this interface, never
/// on a concrete provider. The active implementation is chosen by configuration
/// (Fiscalization:Provider) in <see cref="BillingModule"/>:
///   - "manual" (default) → <see cref="ManualFiscalizationService"/> (no external call)
///   - "parra"            → <see cref="ParraFiscalizationService"/> (api.parra.hr)
/// Adding a new provider = one class implementing this + one config value.
/// </summary>
public interface IFiscalizationService
{
    /// <summary>Short provider id, e.g. "manual" or "parra" (stored on the Purchase).</summary>
    string Provider { get; }

    /// <summary>Issue (or mark for manual issuing) the fiscal invoice for a paid purchase.</summary>
    Task<FiscalizationResult> IssueInvoiceAsync(FiscalizationRequest request, CancellationToken ct = default);
}
