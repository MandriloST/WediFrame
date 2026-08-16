using Microsoft.Extensions.Logging;

namespace WediFrame.Modules.Billing.Services;

/// <summary>
/// Default fiscalization provider: performs NO external call. It records that a
/// fiscal invoice still has to be issued by hand (owner issues it in Parra/other
/// until the automated adapter is wired). This keeps the paid flow fully working
/// without any external dependency or credentials — a safe, honest default.
/// </summary>
public sealed class ManualFiscalizationService(ILogger<ManualFiscalizationService> logger)
    : IFiscalizationService
{
    public string Provider => "manual";

    public Task<FiscalizationResult> IssueInvoiceAsync(FiscalizationRequest request, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Fiscalization pending (manual) for purchase {PurchaseId}: {AmountCents} {Currency}, R1={IsCompany}",
            request.PurchaseId, request.AmountCents, request.Currency, request.IsCompany);

        return Task.FromResult(FiscalizationResult.ManualPending(Provider, request.IssuedAt));
    }
}
