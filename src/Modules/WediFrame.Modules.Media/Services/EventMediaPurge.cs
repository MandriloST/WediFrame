using Microsoft.EntityFrameworkCore;
using WediFrame.Modules.Media.Domain;
using WediFrame.Shared.Storage;

namespace WediFrame.Modules.Media.Services;

/// <summary>
/// The Media module's public contract for PHYSICALLY erasing all of an event's
/// media — R2 objects (originals, thumbnails, in-flight multiparts, export ZIPs)
/// AND the database rows. Used by the Retention worker after the grace period
/// (M4, Phase 2) and, later, by host-requested full-event deletion (Phase 3).
///
/// It deletes media only. The event's cover photo and status are Events' concern
/// (the caller finalizes those via IEventRetention), which keeps this a clean,
/// one-way dependency: Retention → Media, Media → Events (no back edge).
///
/// Idempotent: an R2 delete is a no-op on a missing key and the row delete is a
/// filtered bulk delete, so re-running after a partial failure is safe — the
/// caller only marks the event Deleted once purge has fully succeeded.
/// </summary>
public interface IEventMediaPurge
{
    Task<EventMediaPurgeResult> PurgeAsync(Guid eventId, CancellationToken ct = default);
}

/// <summary>Counts from one purge, for logging and the audit trail.</summary>
public sealed record EventMediaPurgeResult(int MediaDeleted, int ExportsDeleted, int ObjectsDeleted)
{
    public static readonly EventMediaPurgeResult Empty = new(0, 0, 0);
}

public sealed class EventMediaPurge(DbContext db, IObjectStorage storage) : IEventMediaPurge
{
    public async Task<EventMediaPurgeResult> PurgeAsync(Guid eventId, CancellationToken ct = default)
    {
        // --- Media items: originals + thumbnails + any dangling multipart ------
        // Everything the event owns, regardless of status/visibility/soft-delete —
        // this is erasure, not moderation.
        var media = await db.Set<MediaItem>()
            .Where(m => m.EventId == eventId)
            .Select(m => new { m.ObjectKey, m.ThumbnailKey, m.MultipartUploadId, m.UploadStatus })
            .ToListAsync(ct);

        var objectsDeleted = 0;

        foreach (var m in media)
        {
            // An upload that never completed still holds parts on R2 under an
            // upload id; aborting frees that storage. Best-effort.
            if (m.MultipartUploadId is { Length: > 0 } uploadId
                && m.UploadStatus != MediaUploadStatus.Confirmed)
            {
                await storage.AbortMultipartUploadAsync(m.ObjectKey, uploadId, ct);
            }

            await storage.DeleteAsync(m.ObjectKey, ct);
            objectsDeleted++;

            if (m.ThumbnailKey is { Length: > 0 } thumbKey)
            {
                await storage.DeleteAsync(thumbKey, ct);
                objectsDeleted++;
            }
        }

        // --- Export ZIPs ------------------------------------------------------
        var exports = await db.Set<MediaExport>()
            .Where(e => e.EventId == eventId)
            .Select(e => new { e.ObjectKey })
            .ToListAsync(ct);

        foreach (var e in exports)
        {
            if (e.ObjectKey is { Length: > 0 } zipKey)
            {
                await storage.DeleteAsync(zipKey, ct);
                objectsDeleted++;
            }
        }

        // --- Metadata rows ----------------------------------------------------
        // Deleted only AFTER the R2 objects are gone, so a crash never leaves the
        // event marked Deleted with orphaned bytes (the caller flips the status
        // only after this returns).
        var mediaDeleted = await db.Set<MediaItem>()
            .Where(m => m.EventId == eventId)
            .ExecuteDeleteAsync(ct);

        var exportsDeleted = await db.Set<MediaExport>()
            .Where(e => e.EventId == eventId)
            .ExecuteDeleteAsync(ct);

        return new EventMediaPurgeResult(mediaDeleted, exportsDeleted, objectsDeleted);
    }
}
