namespace WediFrame.Shared.Email;

/// <summary>
/// One transactional email. Always carries both an HTML and a plain-text body
/// (multipart/alternative) so it renders well and survives text-only clients.
/// </summary>
public sealed record EmailMessage(
    string ToEmail,
    string Subject,
    string HtmlBody,
    string TextBody,
    string? ToName = null);

/// <summary>
/// Cross-cutting port for sending transactional email. Implementation lives in
/// Infrastructure (SMTP), mirroring <see cref="Storage.IObjectStorage"/>. When
/// email is not configured a logging no-op is registered instead, so the app
/// runs with zero mail config and never sends by accident.
///
/// Sending is best-effort from the caller's perspective: failures throw, and
/// callers (e.g. the retention reminder) decide whether to retry — they must NOT
/// mark work as done until the send actually succeeds.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}
