namespace WediFrame.Modules.Identity.Contracts;

// Error messages are machine-readable codes ("auth.email_taken", ...);
// the frontend maps them to localized strings (i18n lives client-side).

public sealed record RegisterRequest(string Email, string Password, string? Language);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    UserResponse User);

public sealed record UserResponse(Guid Id, string Email, string Role, string Language);
