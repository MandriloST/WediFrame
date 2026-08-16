using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using WediFrame.Shared.Email;
using WediFrame.Shared.Options;

namespace WediFrame.Infrastructure.Email;

/// <summary>
/// SMTP <see cref="IEmailSender"/> built on the framework's System.Net.Mail —
/// zero extra dependencies, fine for low-volume transactional mail over
/// submission + STARTTLS (port 587). If we ever need implicit TLS (465) or
/// OAuth, swap this for a MailKit-based sender; the port stays the same.
/// </summary>
public sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var opt = options.Value;

        using var mail = new MailMessage
        {
            From = new MailAddress(opt.FromEmail, opt.FromName),
            Subject = message.Subject,
            Body = message.TextBody,
            IsBodyHtml = false,
        };
        mail.To.Add(new MailAddress(message.ToEmail, message.ToName ?? message.ToEmail));

        // multipart/alternative: text is the body above; add the HTML view.
        mail.AlternateViews.Add(
            AlternateView.CreateAlternateViewFromString(message.HtmlBody, null, "text/html"));

        using var client = new SmtpClient(opt.Host, opt.Port)
        {
            EnableSsl = opt.UseStartTls,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Credentials = string.IsNullOrWhiteSpace(opt.Username)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(opt.Username, opt.Password),
        };

        await client.SendMailAsync(mail, ct);
    }
}
