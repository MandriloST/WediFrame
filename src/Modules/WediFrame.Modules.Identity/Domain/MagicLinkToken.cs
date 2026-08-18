namespace WediFrame.Modules.Identity.Domain;

/// <summary>
/// One passwordless "magic link" login token. Like <see cref="RefreshToken"/>,
/// only the SHA-256 hash of the raw token is stored — the raw value travels once,
/// inside the emailed link, and is never persisted. Single use (consumed on first
/// successful use) and short-lived (see <c>Auth:MagicLink:TokenLifetimeMinutes</c>).
///
/// Keyed by <see cref="Email"/> rather than a user id, because a magic link may
/// register a brand-new account: at request time the user might not exist yet.
/// </summary>
public sealed class MagicLinkToken
{
    public Guid Id { get; set; }

    /// <summary>Normalized (trimmed, lower-cased) target email. May not yet map to a User.</summary>
    public required string Email { get; set; }

    /// <summary>Base64 SHA-256 of the raw URL-safe token. Unique.</summary>
    public required string TokenHash { get; set; }

    public MagicLinkPurpose Purpose { get; set; } = MagicLinkPurpose.Login;

    /// <summary>Language captured at request time — used to localize the email and, on
    /// registration, to seed the new user's <see cref="User.PreferredLanguage"/>.</summary>
    public string PreferredLanguage { get; set; } = "hr";

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Set the moment the link is consumed. Enforces single use.</summary>
    public DateTimeOffset? ConsumedAt { get; set; }

    public bool IsConsumable(DateTimeOffset now) => ConsumedAt is null && now < ExpiresAt;
}

public enum MagicLinkPurpose
{
    /// <summary>Passwordless login or first-time registration.</summary>
    Login = 0,
}
