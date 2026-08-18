namespace WediFrame.Modules.Identity.Services;

/// <summary>
/// Bound from the "Auth:MagicLink" configuration section. All values have safe
/// defaults, so magic link works with zero config. In dev with email unconfigured
/// the endpoint logs the link to the console (see AuthEndpoints) so it's testable
/// end-to-end without an SMTP server.
/// </summary>
public sealed class MagicLinkOptions
{
    public const string SectionName = "Auth:MagicLink";

    /// <summary>Master switch. When false, the magic-link endpoints return 404
    /// (the frontend hides the option).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How long an emailed link stays valid.</summary>
    public int TokenLifetimeMinutes { get; set; } = 15;

    /// <summary>Minimum gap between link requests for the same email — anti
    /// mail-bombing. Requests inside the window are silently ignored (still 200).</summary>
    public int PerEmailCooldownSeconds { get; set; } = 60;
}
