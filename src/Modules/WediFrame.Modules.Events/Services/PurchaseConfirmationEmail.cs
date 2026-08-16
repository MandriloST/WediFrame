using System.Globalization;
using System.Net;
using WediFrame.Shared.Email;

namespace WediFrame.Modules.Events.Services;

/// <summary>
/// Builds the localized "payment received" email sent to the host after a paid
/// event activates (M4, Phase 4b). Copy lives in code (server-rendered, rarely
/// changes, needs HTML+plaintext). HR is the default; EN for "en". Includes the
/// package, amount and — when fiscalization has issued one — the invoice number.
/// </summary>
internal static class PurchaseConfirmationEmail
{
    public static EmailMessage Build(
        string language,
        string toEmail,
        string eventTitle,
        string packageName,
        int amountCents,
        string currency,
        string? invoiceNumber,
        string manageUrl)
    {
        var hr = !language.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        var amount = (amountCents / 100m).ToString("0.00", CultureInfo.InvariantCulture)
            + " " + currency.ToUpperInvariant();

        var title = WebUtility.HtmlEncode(eventTitle);
        var pkg = WebUtility.HtmlEncode(packageName);
        var url = WebUtility.HtmlEncode(manageUrl);
        var invoice = string.IsNullOrWhiteSpace(invoiceNumber) ? null : WebUtility.HtmlEncode(invoiceNumber);

        if (hr)
        {
            var subject = $"Plaćanje zaprimljeno — {eventTitle}";
            var invoiceText = invoice is null ? "" : $"Broj računa: {invoiceNumber}\n";
            var text =
                $"Bok,\n\nhvala! Plaćanje je zaprimljeno i tvoj event \"{eventTitle}\" je aktiviran.\n\n" +
                $"Paket: {packageName}\nIznos: {amount}\n{invoiceText}\n" +
                $"Upravljaj eventom: {manageUrl}\n\nWediFrame";
            var invoiceHtml = invoice is null ? "" : $"<li>Broj računa: <strong>{invoice}</strong></li>";
            var html =
                $"<p>Bok,</p><p>hvala! Plaćanje je zaprimljeno i tvoj event <strong>{title}</strong> je aktiviran.</p>" +
                $"<ul><li>Paket: <strong>{pkg}</strong></li><li>Iznos: <strong>{amount}</strong></li>{invoiceHtml}</ul>" +
                $"<p><a href=\"{url}\">Upravljaj eventom</a></p><p>WediFrame</p>";
            return new EmailMessage(toEmail, subject, html, text);
        }
        else
        {
            var subject = $"Payment received — {eventTitle}";
            var invoiceText = invoice is null ? "" : $"Invoice number: {invoiceNumber}\n";
            var text =
                $"Hi,\n\nthank you! Your payment was received and your event \"{eventTitle}\" is now active.\n\n" +
                $"Package: {packageName}\nAmount: {amount}\n{invoiceText}\n" +
                $"Manage your event: {manageUrl}\n\nWediFrame";
            var invoiceHtml = invoice is null ? "" : $"<li>Invoice number: <strong>{invoice}</strong></li>";
            var html =
                $"<p>Hi,</p><p>thank you! Your payment was received and your event <strong>{title}</strong> is now active.</p>" +
                $"<ul><li>Package: <strong>{pkg}</strong></li><li>Amount: <strong>{amount}</strong></li>{invoiceHtml}</ul>" +
                $"<p><a href=\"{url}\">Manage your event</a></p><p>WediFrame</p>";
            return new EmailMessage(toEmail, subject, html, text);
        }
    }
}
