namespace WediFrame.Modules.Identity.Domain;

/// <summary>
/// Opaque refresh token. Only the SHA-256 hash is stored — the raw value
/// is returned to the client once and never persisted. Tokens are rotated
/// on every use (old one revoked, new one issued).
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    /// <summary>Base64 SHA-256 of the raw token. Unique.</summary>
    public required string TokenHash { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Set when the token is rotated or explicitly revoked.</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && now < ExpiresAt;
}
