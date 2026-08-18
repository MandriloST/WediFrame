namespace WediFrame.Modules.Identity.Domain;

/// <summary>
/// A host (or internal admin) account. Guests never have a User row —
/// they are authorized purely by the event access token (Events module).
///
/// A user may authenticate by password, by Google, and/or by magic link.
/// Password is therefore optional: a Google-only or magic-link-only account
/// has <see cref="PasswordHash"/> == null.
/// </summary>
public sealed class User
{
    public Guid Id { get; set; }

    /// <summary>Normalized (trimmed, lower-cased) email. Unique.</summary>
    public required string Email { get; set; }

    /// <summary>
    /// PBKDF2 hash produced by ASP.NET Core's PasswordHasher, or null when the
    /// account has no password (registered via Google or magic link only).
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// True once we have proof the user controls this email: they signed in with
    /// Google (email_verified) or consumed a magic link. Existing password
    /// accounts are backfilled to true by the AddUserAuthMethods migration.
    /// Used as the safety gate for linking a Google login to an existing account.
    /// </summary>
    public bool EmailVerified { get; set; }

    /// <summary>
    /// Google's stable subject identifier ("sub" claim) once the account is linked
    /// to a Google identity, otherwise null. Matched on future Google logins so a
    /// changed email does not break the link. Unique when set.
    /// </summary>
    public string? GoogleSubjectId { get; set; }

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
