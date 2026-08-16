using Microsoft.EntityFrameworkCore;
using WediFrame.Modules.Media.Domain;
using WediFrame.Shared.Media;
using WediFrame.Shared.Storage;

namespace WediFrame.Modules.Media.Services;

/// <summary>
/// Media-module implementation of <see cref="IEventMediaPurge"/> (contract in
/// Shared). Deletes media only — the event's cover photo and status are Events'
/// concern (the caller finalizes those via IEventRetention), which keeps this a
/// clean one-way dependency: Media → Events (no back edge), and callers depend
/// on the Shared port rather than on this module.
/// </summary>
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
