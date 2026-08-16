using Microsoft.EntityFrameworkCore;
using WediFrame.Modules.Identity.Domain;
using WediFrame.Shared.Admin;

namespace WediFrame.Modules.Identity.Services;

/// <summary>
/// Identity-side implementation of <see cref="IAdminUserDirectory"/>. Read-only;
/// keeps the User query inside the Identity module (boundary discipline).
/// </summary>
public sealed class AdminUserDirectory(DbContext db) : IAdminUserDirectory
{
    private const int MaxPageSize = 200;

    public async Task<AdminUserPage> ListAsync(AdminUserQuery query, CancellationToken ct)
    {
        var page = query.Page > 0 ? query.Page : 1;
        var size = query.PageSize is > 0 and <= MaxPageSize ? query.PageSize : 50;

        var q = db.Set<User>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // Emails are stored normalized (lower-cased), so lower the term too.
            var term = query.Search.Trim().ToLowerInvariant();
            q = q.Where(u => u.Email.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.Role)
            && Enum.TryParse<UserRole>(query.Role, ignoreCase: true, out var role))
        {
            q = q.Where(u => u.Role == role);
        }

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(u => new AdminUserRecord(
                u.Id,
                u.Email,
                u.Role.ToString(),
                u.PreferredLanguage,
                u.CreatedAt))
            .ToListAsync(ct);

        return new AdminUserPage(items, total);
    }
}
