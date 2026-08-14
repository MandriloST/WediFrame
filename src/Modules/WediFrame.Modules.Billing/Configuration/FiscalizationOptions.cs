namespace WediFrame.Modules.Billing.Configuration;

/// <summary>
/// Chooses and configures the fiscalization provider. Bound from the "Fiscalization"
/// config section. Default provider is "manual" (no external call, invoice issued by
/// hand) so the app runs with zero fiscalization credentials.
/// </summary>
public sealed class FiscalizationOptions
{
    public const string SectionName = "Fiscalization";

    /// <summary>"manual" (default) or "parra".</summary>
    public string Provider { get; set; } = "manual";

    public ParraOptions Parra { get; set; } = new();
}

/// <summary>Settings for the Parra provider (api.parra.hr). Secrets come from env/secrets, never the repo.</summary>
public sealed class ParraOptions
{
    public string BaseUrl { get; set; } = "https://api.parra.hr";

    /// <summary>API key issued per business subject (OIB). Server-side only.</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>Parra "poslovni prostor" id required on requests.</summary>
    public string WorkspaceId { get; set; } = "";
}
