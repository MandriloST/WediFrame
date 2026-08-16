namespace WediFrame.Modules.Billing.Configuration;

/// <summary>
/// Stripe credentials. Bound from the "Stripe" config section. Secrets come from
/// env/user-secrets locally and platform env vars in prod — never the repo.
/// </summary>
public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    /// <summary>Secret API key (sk_test_… / sk_live_…). Server-side only.</summary>
    public string SecretKey { get; set; } = "";

    /// <summary>Publishable key (pk_…). Safe for the client; not used server-side yet.</summary>
    public string PublishableKey { get; set; } = "";

    /// <summary>Webhook signing secret (whsec_…) used to verify /webhooks/stripe.</summary>
    public string WebhookSecret { get; set; } = "";

    public bool Enabled => !string.IsNullOrWhiteSpace(SecretKey);
}
