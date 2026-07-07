namespace WediFrame.Modules.Identity.Services;

/// <summary>
/// Bound from the "Jwt" configuration section. The signing key MUST come from
/// user-secrets locally and an env var (Jwt__SigningKey) on Railway — never the repo.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "wediframe";

    public string Audience { get; set; } = "wediframe";

    /// <summary>Symmetric HMAC-SHA256 key. Minimum 32 characters.</summary>
    public string SigningKey { get; set; } = "";

    public int AccessTokenMinutes { get; set; } = 30;

    public int RefreshTokenDays { get; set; } = 30;
}
