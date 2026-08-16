using System.Net;
using WediFrame.Shared.Email;

namespace WediFrame.Modules.Retention.Services;

/// <summary>
/// Builds the localized "your gallery expires soon" email. Email copy lives in
/// code (not the frontend i18n JSON) — it's server-rendered and rarely changes.
/// HR is the default; EN for "en". Keep it short, warm, one clear action:
/// download the gallery before it's deleted.
/// </summary>
internal static class RetentionReminderEmail
{
    public static EmailMessage Build(string language, string toEmail, string eventTitle, DateOnly expiresAt, string manageUrl)
    {
        var hr = !language.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        var date = expiresAt.ToString("dd.MM.yyyy.");
        var title = WebUtility.HtmlEncode(eventTitle);
        var url = WebUtility.HtmlEncode(manageUrl);

        if (hr)
        {
            var subject = $"Galerija „{eventTitle}“ uskoro istječe";
            var text =
                $"Bok,\n\ngalerija za tvoj event \"{eventTitle}\" bit će dostupna do {date}. " +
                "Nakon toga se fotografije i videi trajno brišu.\n\n" +
                $"Preuzmi sve dok možeš: {manageUrl}\n\nWediFrame";
            var html =
                $"<p>Bok,</p><p>galerija za tvoj event <strong>{title}</strong> bit će dostupna do " +
                $"<strong>{date}</strong>. Nakon toga se fotografije i videi trajno brišu.</p>" +
                $"<p><a href=\"{url}\">Preuzmi sve dok možeš</a></p><p>WediFrame</p>";
            return new EmailMessage(toEmail, subject, html, text);
        }
        else
        {
            var subject = $"Your gallery \"{eventTitle}\" expires soon";
            var text =
                $"Hi,\n\nthe gallery for your event \"{eventTitle}\" is available until {date}. " +
                "After that, the photos and videos are permanently deleted.\n\n" +
                $"Download everything while you can: {manageUrl}\n\nWediFrame";
            var html =
                $"<p>Hi,</p><p>the gallery for your event <strong>{title}</strong> is available until " +
                $"<strong>{date}</strong>. After that, the photos and videos are permanently deleted.</p>" +
                $"<p><a href=\"{url}\">Download everything while you can</a></p><p>WediFrame</p>";
            return new EmailMessage(toEmail, subject, html, text);
        }
    }
}
