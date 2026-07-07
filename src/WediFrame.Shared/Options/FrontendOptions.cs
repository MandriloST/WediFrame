namespace WediFrame.Shared.Options;

/// <summary>
/// Bound from the "Frontend" configuration section.
/// Used to build absolute links pointing at the Next.js frontend
/// (guest QR links now; email links in M4).
/// </summary>
public sealed class FrontendOptions
{
    public const string SectionName = "Frontend";

    /// <summary>
    /// Base URL of the guest page, token gets appended.
    /// Dev: "http://localhost:3000/e/" · Prod: "https://wediframe.hr/e/".
    /// </summary>
    public string GuestBaseUrl { get; set; } = "";

    public string BuildGuestUrl(string guestToken) => GuestBaseUrl + guestToken;
}
