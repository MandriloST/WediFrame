using Microsoft.EntityFrameworkCore;
using WediFrame.Modules.Media.Domain;
using WediFrame.Shared.Admin;
using WediFrame.Shared.Audit;
using WediFrame.Shared.Storage;

namespace WediFrame.Modules.Media.Services;

/// <summary>
/// Media-side implementation of <see cref="IAdminMedia"/>. Mirrors the host gallery
/// management (visibility toggle + soft delete) but WITHOUT an ownership check —
/// admin may moderate any event — and audits with distinct *_by_admin action codes.
/// </summary>
public sealed class AdminMedia(
    DbContext db,
    IObjectStorage storage,
    TimeProvider timeProvider) : IAdminMedia
{
    private static readonly TimeSpan ViewUrlExpiry = TimeSpan.FromHours(1);

    public async Task<AdminMediaPage> ListAsync(Guid eventId, int offset, int limit, CancellationToken ct)
    {
        var skip = Math.Max(0, offset);
        var take = Math.Clamp(limit, 1, 48);

        var rows = await db.Set<MediaItem>()
            .Where(m => m.EventId == eventId
                && m.UploadStatus == MediaUploadStatus.Confirmed
                && m.SoftDeletedAt == null)
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.ObjectKey)
            .Skip(skip)
            .Take(take)
            .Select(m => new
            {
                m.Id,
                m.Type,
                m.ObjectKey,
                m.ThumbnailKey,
                m.ContentType,
                m.GuestName,
                m.Visibility,
                m.SizeBytes,
                m.CreatedAt,
            })
            .ToListAsync(ct);

        var items = new List<AdminMediaItem>(rows.Count);
        foreach (var r in rows)
        {
            var url = (await storage.PresignGetAsync(r.ObjectKey, ViewUrlExpiry, ct)).ToString();
            var thumbnailUrl = r.ThumbnailKey is null
                ? null
                : (await storage.PresignGetAsync(r.ThumbnailKey, ViewUrlExpiry, ct)).ToString();

            items.Add(new AdminMediaItem(
                r.Id,
                r.Type.ToString(),
                url,
                thumbnailUrl,
                r.ContentType,
                r.GuestName,
                r.Visibility.ToString(),
                r.SizeBytes,
                r.CreatedAt));
        }

        var nextOffset = rows.Count == take ? skip + take : (int?)null;
        return new AdminMediaPage(items, nextOffset);
    }

    public async Task<AdminMediaVisibilityResult?> SetVisibilityAsync(
        Guid eventId, Guid mediaId, bool hidden, Guid adminUserId, CancellationToken ct)
    {
        var item = await db.Set<MediaItem>()
            .SingleOrDefaultAsync(m => m.Id == mediaId && m.EventId == eventId && m.SoftDeletedAt == null, ct);

        if (item is null)
        {
            return null;
        }

        var target = hidden ? MediaVisibility.Hidden : MediaVisibility.Visible;
        if (item.Visibility != target)
        {
            item.Visibility = target;
            db.Set<AuditLogEntry>().Add(new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                OccurredAt = timeProvider.GetUtcNow(),
                ActorUserId = adminUserId,
                Action = hidden ? "media.hidden_by_admin" : "media.unhidden_by_admin",
                EntityType = nameof(MediaItem),
                EntityId = item.Id.ToString(),
            });
            await db.SaveChangesAsync(ct);
        }

        return new AdminMediaVisibilityResult(item.Id, item.Visibility.ToString());
    }

    public async Task<bool> SoftDeleteAsync(Guid eventId, Guid mediaId, Guid adminUserId, CancellationToken ct)
    {
        var item = await db.Set<MediaItem>()
            .SingleOrDefaultAsync(m => m.Id == mediaId && m.EventId == eventId, ct);

        if (item is null)
        {
            return false;
        }

        if (item.SoftDeletedAt is null)
        {
            item.SoftDeletedAt = timeProvider.GetUtcNow();
            db.Set<AuditLogEntry>().Add(new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                OccurredAt = timeProvider.GetUtcNow(),
                ActorUserId = adminUserId,
                Action = "media.deleted_by_admin",
                EntityType = nameof(MediaItem),
                EntityId = item.Id.ToString(),
            });
            await db.SaveChangesAsync(ct);
        }

        return true;
    }
}
