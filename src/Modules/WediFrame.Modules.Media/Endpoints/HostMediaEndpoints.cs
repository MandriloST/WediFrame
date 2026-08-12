using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WediFrame.Modules.Events.Services;
using WediFrame.Modules.Media.Contracts;
using WediFrame.Modules.Media.Domain;
using WediFrame.Shared.Audit;
using WediFrame.Shared.Auth;
using WediFrame.Shared.Storage;

namespace WediFrame.Modules.Media.Endpoints;

/// <summary>
/// Host-facing gallery management (M2): the couple reviews everything guests
/// shared and curates it — hide/unhide a photo or video, or remove one. These
/// are authenticated (host JWT) and ownership-checked via the Events module's
/// <see cref="IHostEventAccess"/> contract; the read side mirrors the guest
/// gallery but also surfaces HIDDEN items so they can be brought back.
///
/// Delete is a SOFT delete (SoftDeletedAt): the object stays on R2 until the
/// retention grace period physically removes it (Decision Log 2026-07-04),
/// which keeps an accidental tap recoverable and the erasure trail auditable.
/// </summary>
public static class HostMediaEndpoints
{
    /// <summary>Display URLs live long enough for a management session; each page re-signs.</summary>
    private static readonly TimeSpan ViewUrlExpiry = TimeSpan.FromHours(1);

    private const int DefaultPageSize = 24;
    private const int MaxPageSize = 48;

    public static IEndpointRouteBuilder MapHostMediaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Lives under /events/{id}/media — the host perspective (ARCHITECTURE.md §4).
        // Separate route group from the guest endpoints; JWT + ownership, not token.
        var group = endpoints.MapGroup("/events/{eventId:guid}/media").RequireAuthorization();

        group.MapGet("/", ListAsync);
        group.MapPatch("/{mediaId:guid}", SetVisibilityAsync);
        group.MapDelete("/{mediaId:guid}", DeleteAsync);

        return endpoints;
    }

    /// <summary>
    /// The host gallery: every confirmed item in the event — visible AND hidden,
    /// photos and videos — newest first. Same deterministic order and offset
    /// pagination as the guest gallery; presigned GET URLs minted per page.
    /// </summary>
    private static async Task<IResult> ListAsync(
        Guid eventId,
        ClaimsPrincipal principal,
        IHostEventAccess hostEvents,
        DbContext db,
        IObjectStorage storage,
        int? offset,
        int? limit,
        CancellationToken ct)
    {
        if (principal.GetUserId() is not { } userId)
        {
            return Results.Unauthorized();
        }

        // Foreign / unknown / deleted event → 404 (no existence leak).
        if (await hostEvents.FindOwnedAsync(eventId, userId, ct) is null)
        {
            return Results.NotFound();
        }

        var skip = Math.Max(0, offset ?? 0);
        var take = Math.Clamp(limit ?? DefaultPageSize, 1, MaxPageSize);

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

        var items = new List<HostGalleryItem>(rows.Count);
        foreach (var r in rows)
        {
            var url = (await storage.PresignGetAsync(r.ObjectKey, ViewUrlExpiry, ct)).ToString();
            var thumbnailUrl = r.ThumbnailKey is null
                ? null
                : (await storage.PresignGetAsync(r.ThumbnailKey, ViewUrlExpiry, ct)).ToString();

            items.Add(new HostGalleryItem(
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

        return Results.Ok(new HostGalleryResponse(items, nextOffset));
    }

    /// <summary>
    /// Hide or unhide an item. Hidden items disappear from the guest gallery
    /// (which filters on Visibility) but stay visible to the host. Idempotent.
    /// </summary>
    private static async Task<IResult> SetVisibilityAsync(
        Guid eventId,
        Guid mediaId,
        UpdateMediaVisibilityRequest request,
        ClaimsPrincipal principal,
        IHostEventAccess hostEvents,
        DbContext db,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        if (principal.GetUserId() is not { } userId)
        {
            return Results.Unauthorized();
        }

        if (await hostEvents.FindOwnedAsync(eventId, userId, ct) is null)
        {
            return Results.NotFound();
        }

        if (!Enum.TryParse<MediaVisibility>((request.Visibility ?? "").Trim(), ignoreCase: true, out var visibility))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["visibility"] = ["media.visibility_invalid"], // Visible | Hidden
            });
        }

        var item = await db.Set<MediaItem>()
            .SingleOrDefaultAsync(m => m.Id == mediaId && m.EventId == eventId && m.SoftDeletedAt == null, ct);

        if (item is null)
        {
            return Results.NotFound();
        }

        if (item.Visibility != visibility)
        {
            item.Visibility = visibility;
            db.Set<AuditLogEntry>().Add(new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                OccurredAt = timeProvider.GetUtcNow(),
                ActorUserId = userId,
                Action = visibility == MediaVisibility.Hidden ? "media.hidden" : "media.unhidden",
                EntityType = nameof(MediaItem),
                EntityId = item.Id.ToString(),
            });
            await db.SaveChangesAsync(ct);
        }

        return Results.Ok(new HostMediaVisibilityResponse(item.Id, item.Visibility.ToString()));
    }

    /// <summary>
    /// Soft-delete an item (host's right to remove content, and part of GDPR
    /// erasure). The object is NOT removed from R2 here — the retention job
    /// physically deletes soft-deleted items after the grace period (M4), so
    /// a mistaken tap stays recoverable. Idempotent; audited.
    /// </summary>
    private static async Task<IResult> DeleteAsync(
        Guid eventId,
        Guid mediaId,
        ClaimsPrincipal principal,
        IHostEventAccess hostEvents,
        DbContext db,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        if (principal.GetUserId() is not { } userId)
        {
            return Results.Unauthorized();
        }

        if (await hostEvents.FindOwnedAsync(eventId, userId, ct) is null)
        {
            return Results.NotFound();
        }

        var item = await db.Set<MediaItem>()
            .SingleOrDefaultAsync(m => m.Id == mediaId && m.EventId == eventId, ct);

        if (item is null)
        {
            return Results.NotFound();
        }

        if (item.SoftDeletedAt is null)
        {
            item.SoftDeletedAt = timeProvider.GetUtcNow();
            db.Set<AuditLogEntry>().Add(new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                OccurredAt = timeProvider.GetUtcNow(),
                ActorUserId = userId,
                Action = "media.deleted",
                EntityType = nameof(MediaItem),
                EntityId = item.Id.ToString(),
            });
            await db.SaveChangesAsync(ct);
        }

        return Results.NoContent();
    }
}
