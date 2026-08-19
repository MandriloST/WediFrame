namespace WediFrame.Modules.Identity.Contracts;

// Error messages are machine-readable codes ("auth.email_taken", ...);
// the frontend maps them to localized strings (i18n lives client-side).

public sealed record RegisterRequest(string Email, string Password, string? Language);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

// Passwordless magic link. Request always answers 200 (no account enumeration);
// consume returns the same AuthResponse as password login.
public sealed record MagicLinkRequest(string Email, string? Language);

public sealed record MagicLinkConsumeRequest(string Token);

// Google Sign-In (approach B): the frontend obtains a Google ID token via Google
// Identity Services and posts it here; the backend verifies it and issues its own
// session. Returns the same AuthResponse as password login.
public sealed record GoogleSignInRequest(string IdToken);

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    UserResponse User);

public sealed record UserResponse(Guid Id, string Email, string Role, string Language);
