namespace WediFrame.Modules.Identity.Domain;

/// <summary>
/// A host (or internal admin) account. Guests never have a User row —
/// they are authorized purely by the event access token (Events module).
/// </summary>
public sealed class User
{
    public Guid Id { get; set; }

    /// <summary>Normalized (trimmed, lower-cased) email. Unique.</summary>
    public required string Email { get; set; }

    /// <summary>PBKDF2 hash produced by ASP.NET Core's PasswordHasher.</summary>
    public required string PasswordHash { get; set; }

    public UserRole Role { get; set; } = UserRole.Host;

    /// <summary>UI language preference, e.g. "hr" or "en". Defaults to "hr".</summary>
    public string PreferredLanguage { get; set; } = "hr";

    public DateTimeOffset CreatedAt { get; set; }
}

public enum UserRole
{
    Host = 0,
    Admin = 1,
}
