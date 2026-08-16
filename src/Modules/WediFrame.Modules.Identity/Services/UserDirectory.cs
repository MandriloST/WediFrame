using Microsoft.EntityFrameworkCore;
using WediFrame.Modules.Identity.Domain;
using WediFrame.Shared.Directory;

namespace WediFrame.Modules.Identity.Services;

/// <summary>
/// Identity's implementation of <see cref="IUserDirectory"/> (contract in Shared).
/// Reads the User table so other modules can resolve a host's email + language
/// without referencing Identity — e.g. the retention reminder (Retention module).
/// </summary>
public sealed class UserDirectory(DbContext db) : IUserDirectory
{
    public async Task<UserContact?> GetContactAsync(Guid userId, CancellationToken ct = default)
    {
        return await db.Set<User>()
            .Where(u => u.Id == userId)
            .Select(u => new UserContact(u.Id, u.Email, u.PreferredLanguage))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyDictionary<Guid, UserContact>> GetContactsAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken ct = default)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, UserContact>();
        }

        var ids = userIds.Distinct().ToArray();

        var contacts = await db.Set<User>()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new UserContact(u.Id, u.Email, u.PreferredLanguage))
            .ToListAsync(ct);

        return contacts.ToDictionary(c => c.UserId);
    }
}
