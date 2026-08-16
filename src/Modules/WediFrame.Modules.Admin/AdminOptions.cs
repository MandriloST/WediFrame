namespace WediFrame.Modules.Admin;

/// <summary>
/// Bound from the "Admin" configuration section. <see cref="BootstrapEmails"/> lists
/// the email addresses that should hold the Admin role; on startup any matching
/// EXISTING user is promoted (see AdminBootstrapService). There is deliberately no
/// public self-promotion path — becoming admin requires config + a restart.
/// </summary>
public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    /// <summary>
    /// Emails promoted to Admin on startup. Local: appsettings.Development.json or
    /// user-secrets. Railway: Admin__BootstrapEmails__0, Admin__BootstrapEmails__1, …
    /// </summary>
    public string[] BootstrapEmails { get; set; } = [];
}
