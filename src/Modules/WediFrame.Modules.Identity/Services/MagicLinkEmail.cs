using System.Net;
using WediFrame.Shared.Email;

namespace WediFrame.Modules.Identity.Services;

/// <summary>
/// Builds the localized passwordless "sign in" email. Copy lives in code (server-
/// rendered, rarely changes), mirroring the retention reminder. HR is default; EN
/// for "en". One clear action: click to sign in. Notes the expiry and that the
/// link can be ignored if they didn't request it.
/// </summary>
internal static class MagicLinkEmail
{
    public static EmailMessage Build(string language, string toEmail, string link, int lifetimeMinutes)
    {
        var hr = !language.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        var url = WebUtility.HtmlEncode(link);

        if (hr)
        {
            var subject = "Tvoj link za prijavu — WediFrame";
            var text =
                $"Bok,\n\nklikni na link ispod za prijavu na WediFrame:\n{link}\n\n" +
                $"Link vrijedi {lifetimeMinutes} minuta i može se iskoristiti jednom. " +
                "Ako nisi ti zatražio/la prijavu, slobodno zanemari ovaj email.\n\nWediFrame";
            var html =
                "<p>Bok,</p><p>klikni za prijavu na WediFrame:</p>" +
                $"<p><a href=\"{url}\">Prijava na WediFrame</a></p>" +
                $"<p style=\"color:#666;font-size:14px\">Link vrijedi {lifetimeMinutes} minuta i može se " +
                "iskoristiti jednom. Ako nisi ti zatražio/la prijavu, zanemari ovaj email.</p><p>WediFrame</p>";
            return new EmailMessage(toEmail, subject, html, text);
        }
        else
        {
            var subject = "Your sign-in link — WediFrame";
            var text =
                $"Hi,\n\nclick the link below to sign in to WediFrame:\n{link}\n\n" +
                $"The link is valid for {lifetimeMinutes} minutes and can be used once. " +
                "If you didn't request this, you can safely ignore this email.\n\nWediFrame";
            var html =
                "<p>Hi,</p><p>click to sign in to WediFrame:</p>" +
                $"<p><a href=\"{url}\">Sign in to WediFrame</a></p>" +
                $"<p style=\"color:#666;font-size:14px\">The link is valid for {lifetimeMinutes} minutes and can be " +
                "used once. If you didn't request this, ignore this email.</p><p>WediFrame</p>";
            return new EmailMessage(toEmail, subject, html, text);
        }
    }
}
