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

    /// <summary>
    /// Base URL of the app (host dashboard), used to build Stripe success/cancel
    /// return URLs. Dev: "http://localhost:3000" · Prod: "https://wediframe.hr".
    /// </summary>
    public string AppBaseUrl { get; set; } = "";

    /// <summary>
    /// Origins allowed to call the API from a browser (CORS).
    /// Dev: ["http://localhost:3000"] · Prod: ["https://wediframe.hr"].
    /// </summary>
    public string[] AllowedOrigins { get; set; } = [];

    public string BuildGuestUrl(string guestToken) => GuestBaseUrl + guestToken;
}
