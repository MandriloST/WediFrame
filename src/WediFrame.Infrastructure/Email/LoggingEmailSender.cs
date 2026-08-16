using Microsoft.Extensions.Logging;
using WediFrame.Shared.Email;

namespace WediFrame.Infrastructure.Email;

/// <summary>
/// No-op <see cref="IEmailSender"/> used when email is not configured. It logs
/// the recipient + subject (never the body) so local dev and un-configured
/// deploys work end-to-end without sending anything or throwing.
/// </summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Email not configured — would send to {To}: \"{Subject}\".",
            message.ToEmail, message.Subject);
        return Task.CompletedTask;
    }
}
