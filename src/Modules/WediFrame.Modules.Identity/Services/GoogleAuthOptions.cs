namespace WediFrame.Modules.Identity.Services;

/// <summary>
/// Bound from the "Auth:Google" configuration section. Opt-in: the endpoint is
/// only active once a Google OAuth Client ID (Web application) is configured and
/// <see cref="Enabled"/> is true — otherwise <c>POST /auth/google</c> returns 404
/// and the frontend hides the button.
///
/// Only the Client ID is needed on the backend (to check the token's audience);
/// there is NO client secret, because verification uses the ID token directly
/// (approach B), not a server-side authorization-code exchange.
/// </summary>
public sealed class GoogleAuthOptions
{
    public const string SectionName = "Auth:Google";

    public bool Enabled { get; set; } = false;

    /// <summary>OAuth 2.0 Web Client ID from Google Cloud Console. Same value the
    /// frontend uses (NEXT_PUBLIC_GOOGLE_CLIENT_ID).</summary>
    public string ClientId { get; set; } = "";
}
