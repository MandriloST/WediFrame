using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WediFrame.Modules.Billing.Services;
using WediFrame.Modules.Events.Domain;
using WediFrame.Shared.Admin;
using WediFrame.Shared.Options;

namespace WediFrame.Modules.Events.Services;

/// <summary>
/// Events-side implementation of <see cref="IAdminEventDirectory"/>. Read-only across
/// all owners/statuses. Package name is resolved through Billing's IPackageCatalog
/// (deduped per page — a handful of lookups), keeping the Package entity out of Events.
/// </summary>
public sealed class AdminEventDirectory(
    DbContext db,
    IPackageCatalog packages,
    IOptions<FrontendOptions> frontend) : IAdminEventDirectory
{
    private const int MaxPageSize = 200;

    public async Task<AdminEventPage> ListAsync(AdminEventQuery query, CancellationToken ct)
    {
        var page = query.Page > 0 ? query.Page : 1;
        var size = query.PageSize is > 0 and <= MaxPageSize ? query.PageSize : 50;

        var q = db.Set<Event>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            q = q.Where(e => EF.Functions.ILike(e.Title, pattern));
        }

        if (!string.IsNullOrWhiteSpace(query.Status)
            && Enum.TryParse<EventStatus>(query.Status, ignoreCase: true, out var status))
        {
            q = q.Where(e => e.Status == status);
        }

        var total = await q.CountAsync(ct);

        var rows = await q
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(e => new Row(
                e.Id, e.Title, e.Type, e.Status, e.OwnerUserId,
                e.PackageId, e.UploadStartDate, e.UploadEndsAt, e.ExpiresAt,
                e.CoverPhotoKey, e.CreatedAt))
            .ToListAsync(ct);

        var packageMap = await ResolvePackagesAsync(rows.Select(r => r.PackageId), ct);

        var items = rows
            .Select(r =>
            {
                var pkg = r.PackageId is { } pid && packageMap.TryGetValue(pid, out var p) ? p : null;
                return new AdminEventRecord(
                    r.Id, r.Title, r.Type, r.Status.ToString(), r.OwnerUserId,
                    pkg?.Slug, pkg?.Name,
                    r.UploadStartDate, r.UploadEndsAt, r.ExpiresAt,
                    r.CoverPhotoKey is not null, r.CreatedAt);
            })
            .ToList();

        return new AdminEventPage(items, total);
    }

    public async Task<AdminEventDetail?> GetAsync(Guid id, CancellationToken ct)
    {
        var e = await db.Set<Event>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (e is null)
        {
            return null;
        }

        var pkg = e.PackageId is { } pid ? await packages.GetByIdAsync(pid, ct) : null;

        return new AdminEventDetail(
            e.Id, e.Title, e.Type, e.Status.ToString(), e.OwnerUserId,
            pkg?.Slug, pkg?.Name,
            e.UploadStartDate, e.UploadEndsAt, e.ExpiresAt,
            e.CoverPhotoKey is not null,
            e.GuestToken, frontend.Value.BuildGuestUrl(e.GuestToken),
            e.CreatedAt);
    }

    public async Task<IReadOnlyDictionary<string, int>> GetStatusCountsAsync(CancellationToken ct)
    {
        var grouped = await db.Set<Event>()
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return grouped.ToDictionary(x => x.Status.ToString(), x => x.Count);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetTitlesAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var idSet = ids.Distinct().ToArray();
        var rows = await db.Set<Event>()
            .Where(e => idSet.Contains(e.Id))
            .Select(e => new { e.Id, e.Title })
            .ToListAsync(ct);

        return rows.ToDictionary(x => x.Id, x => x.Title);
    }

    private async Task<Dictionary<Guid, PackageInfo>> ResolvePackagesAsync(
        IEnumerable<Guid?> packageIds, CancellationToken ct)
    {
        var map = new Dictionary<Guid, PackageInfo>();
        foreach (var id in packageIds.Where(x => x is not null).Select(x => x!.Value).Distinct())
        {
            var info = await packages.GetByIdAsync(id, ct);
            if (info is not null)
            {
                map[id] = info;
            }
        }
        return map;
    }

    private sealed record Row(
        Guid Id, string Title, string Type, EventStatus Status, Guid OwnerUserId,
        Guid? PackageId, DateOnly UploadStartDate, DateOnly? UploadEndsAt, DateOnly? ExpiresAt,
        string? CoverPhotoKey, DateTimeOffset CreatedAt);
}
