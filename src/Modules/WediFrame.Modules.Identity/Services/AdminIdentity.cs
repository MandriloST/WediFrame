using Microsoft.EntityFrameworkCore;
using WediFrame.Modules.Identity.Domain;
using WediFrame.Shared.Admin;

namespace WediFrame.Modules.Identity.Services;

/// <summary>
/// Identity-side implementation of the Shared <see cref="IAdminIdentity"/> contract.
/// Keeps user mutations inside the Identity module (module-boundary discipline); the
/// Admin module only holds the interface.
/// </summary>
public sealed class AdminIdentity(DbContext db) : IAdminIdentity
{
    public async Task<int> PromoteEmailsToAdminAsync(IReadOnlyCollection<string> emails, CancellationToken ct)
    {
        if (emails.Count == 0)
        {
            return 0;
        }

        var normalized = emails
            .Select(e => (e ?? "").Trim().ToLowerInvariant())
            .Where(e => e.Length > 0)
            .Distinct()
            .ToArray();

        if (normalized.Length == 0)
        {
            return 0;
        }

        var users = await db.Set<User>()
            .Where(u => normalized.Contains(u.Email) && u.Role != UserRole.Admin)
            .ToListAsync(ct);

        foreach (var user in users)
        {
            user.Role = UserRole.Admin;
        }

        if (users.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        return users.Count;
    }
}
