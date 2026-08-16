namespace WediFrame.Shared.Admin;

/// <summary>
/// Read-only admin view over identity users. Implemented by the Identity module,
/// consumed by the Admin module — Admin never references Identity directly, only
/// this Shared contract (same pattern as <see cref="IAdminIdentity"/>).
/// </summary>
public interface IAdminUserDirectory
{
    /// <summary>Paged, filtered list of users, newest first.</summary>
    Task<AdminUserPage> ListAsync(AdminUserQuery query, CancellationToken ct);
}

/// <summary>Filter/paging input for <see cref="IAdminUserDirectory.ListAsync"/>.</summary>
/// <param name="Search">Case-insensitive email substring, or null.</param>
/// <param name="Role">"Host" or "Admin" to filter by role; null/unknown = all.</param>
public sealed record AdminUserQuery(string? Search, string? Role, int Page, int PageSize);

/// <summary>One user row for the admin UI. No password/secret fields.</summary>
public sealed record AdminUserRecord(
    Guid Id,
    string Email,
    string Role,
    string PreferredLanguage,
    DateTimeOffset CreatedAt);

/// <summary>A page of users plus the total match count (for pagination).</summary>
public sealed record AdminUserPage(IReadOnlyList<AdminUserRecord> Items, int Total);
