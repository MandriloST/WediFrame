namespace WediFrame.Shared.Options;

/// <summary>
/// Bound from the "Email" configuration section. Provider-agnostic SMTP (works
/// with Resend/Postmark/SendGrid/Fastmail/own mail — all speak SMTP). Secrets
/// (Username/Password) come from user-secrets locally and env vars on Railway
/// (Email__Username, Email__Password). Pick an EU sending region (GDPR).
///
/// When <see cref="IsConfigured"/> is false the API registers a logging no-op
/// sender, so nothing is sent until you fill this in.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Master switch. Even fully filled in, Enabled=false keeps mail off.</summary>
    public bool Enabled { get; set; }

    /// <summary>SMTP host, e.g. "smtp.resend.com".</summary>
    public string Host { get; set; } = "";

    /// <summary>SMTP port. 587 = submission with STARTTLS (recommended default).</summary>
    public int Port { get; set; } = 587;

    /// <summary>Use STARTTLS on connect (true for port 587).</summary>
    public bool UseStartTls { get; set; } = true;

    public string Username { get; set; } = "";

    public string Password { get; set; } = "";

    /// <summary>Envelope/from address, e.g. "noreply@wediframe.hr". Must be a verified sender.</summary>
    public string FromEmail { get; set; } = "";

    /// <summary>Friendly from name shown in clients.</summary>
    public string FromName { get; set; } = "WediFrame";

    public bool IsConfigured =>
        Enabled
        && !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(FromEmail);
}
