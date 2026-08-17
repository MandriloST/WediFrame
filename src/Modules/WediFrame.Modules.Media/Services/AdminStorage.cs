using Microsoft.EntityFrameworkCore;
using WediFrame.Modules.Media.Domain;
using WediFrame.Shared.Admin;

namespace WediFrame.Modules.Media.Services;

/// <summary>
/// Media-side implementation of <see cref="IAdminStorage"/>. Aggregates only
/// Confirmed, non-soft-deleted items (matches gallery/quota semantics), so the
/// reported bytes reflect what actually occupies R2.
/// </summary>
public sealed class AdminStorage(DbContext db) : IAdminStorage
{
    public async Task<AdminStorageTotals> GetTotalsAsync(CancellationToken ct)
    {
        var confirmed = db.Set<MediaItem>()
            .Where(m => m.UploadStatus == MediaUploadStatus.Confirmed && m.SoftDeletedAt == null);

        // One grouped round-trip: totals + per-type counts.
        var byType = await confirmed
            .GroupBy(m => m.Type)
            .Select(g => new { Type = g.Key, Count = g.Count(), Bytes = g.Sum(x => x.SizeBytes) })
            .ToListAsync(ct);

        long totalBytes = 0;
        int itemCount = 0, photoCount = 0, videoCount = 0;
        foreach (var row in byType)
        {
            totalBytes += row.Bytes;
            itemCount += row.Count;
            if (row.Type == MediaType.Photo) photoCount += row.Count;
            else if (row.Type == MediaType.Video) videoCount += row.Count;
        }

        return new AdminStorageTotals(totalBytes, itemCount, photoCount, videoCount);
    }

    public async Task<IReadOnlyList<AdminEventStorage>> TopEventsByStorageAsync(int limit, CancellationToken ct)
    {
        var take = Math.Clamp(limit, 1, 100);

        var rows = await db.Set<MediaItem>()
            .Where(m => m.UploadStatus == MediaUploadStatus.Confirmed && m.SoftDeletedAt == null)
            .GroupBy(m => m.EventId)
            .Select(g => new AdminEventStorage(g.Key, g.Sum(x => x.SizeBytes), g.Count()))
            .OrderByDescending(x => x.Bytes)
            .Take(take)
            .ToListAsync(ct);

        return rows;
    }
}
