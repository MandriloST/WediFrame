using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WediFrame.Modules.Billing.Configuration;

namespace WediFrame.Modules.Billing.Services;

/// <summary>
/// Fiscalization via Parra (api.parra.hr). Parra issues HR fiscal invoices — B2C
/// (natural person, our default: the couple) and B2B eRačun (R1 on a company OIB) —
/// and returns fiscal identifiers (invoice number, JIR, ZKI).
///
/// SCAFFOLD: the HTTP client, auth and config are wired and correct. The concrete
/// request/response MAPPING is intentionally NOT invented — it must be confirmed
/// against the live schema at https://api.parra.hr/docs before this provider is
/// switched on. Until then the default provider stays "manual", so this class is
/// never resolved unless Fiscalization:Provider = "parra" is set explicitly.
///
/// Parra specifics (from their help center):
///   - Auth: API key issued per business subject (OIB); server-side only.
///   - A workspaceId ("poslovni prostor") is required on requests.
///   - Company/bank/invoice settings are configured in the Parra app, not via API.
///   - No webhooks — issuing an invoice is a request/response that returns fiscal data.
/// </summary>
public sealed class ParraFiscalizationService(
    HttpClient http,
    IOptions<FiscalizationOptions> options,
    ILogger<ParraFiscalizationService> logger)
    : IFiscalizationService
{
    private readonly ParraOptions _parra = options.Value.Parra;

    public string Provider => "parra";

    public Task<FiscalizationResult> IssueInvoiceAsync(FiscalizationRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_parra.ApiKey) || string.IsNullOrWhiteSpace(_parra.WorkspaceId))
        {
            throw new InvalidOperationException(
                "Parra fiscalization is selected but not configured — set Fiscalization:Parra:ApiKey and :WorkspaceId.");
        }

        // The HttpClient (BaseAddress + Authorization) is set up in BillingModule.
        // What remains is the invoice payload + fiscal-data parsing, which depends
        // on Parra's exact schema and must NOT be guessed.
        logger.LogWarning(
            "Parra fiscalization invoked for purchase {PurchaseId} but the request mapping is not finalized.",
            request.PurchaseId);

        _ = http; // client is ready; wiring the call is the remaining step

        throw new NotImplementedException(
            "Parra adapter not finalized: implement the invoice request/response mapping per " +
            "https://api.parra.hr/docs (create B2C fiscal invoice / B2B eRačun, read back invoice number + JIR/ZKI). " +
            "Keep Fiscalization:Provider = \"manual\" until this is done.");
    }
}
