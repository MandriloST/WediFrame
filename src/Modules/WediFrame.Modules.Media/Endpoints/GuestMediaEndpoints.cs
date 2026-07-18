using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WediFrame.Modules.Events.Services;
using WediFrame.Modules.Media.Contracts;
using WediFrame.Modules.Media.Domain;
using WediFrame.Shared.Storage;

namespace WediFrame.Modules.Media.Endpoints;

/// <summary>
/// Guest upload flow (the heart of the product) — NO authentication, the event
/// token is the only key. M1 scope: photos via single presigned PUT.
/// Video multipart is the next block; package quotas (photo count, total bytes)
/// arrive with Billing (M3); rate limiting is an M5 item.
/// </summary>
public static class GuestMediaEndpoints
{
    /// <summary>Generous expiry — wedding wifi is slow and guests pick many files at once.</summary>
    private static readonly TimeSpan UploadUrlExpiry = TimeSpan.FromMinutes(30);

    public static IEndpointRouteBuilder MapGuestMediaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Deliberately outside RequireAuthorization — token IS the authorization.
        endpoints.MapPost("/guest/{token}/uploads", StartUploadsAsync);
        endpoints.MapPost("/guest/{token}/uploads/{mediaId:guid}/confirm", ConfirmUploadAsync);

        return endpoints;
    }

    /// <summary>
    /// Step 1: validate the batch and hand out one presigned PUT URL per photo.
    /// Creates Pending MediaItems — a cleanup job (backlog, with multipart) will
    /// purge stale pendings that never confirm.
    /// </summary>
    private static async Task<IResult> StartUploadsAsync(
        string token,
        GuestUploadRequest request,
        IGuestEventAccess guestEvents,
        DbContext db,
        IObjectStorage storage,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        var ev = await guestEvents.FindByTokenAsync(token, ct);
        if (ev is null)
        {
            return Results.NotFound();
        }

        var now = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        if (!ev.IsUploadOpen(today))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["event"] = ["media.upload_closed"], // outside the upload period
            });
        }

        var items = request.Items ?? [];
        if (items.Count is < 1 or > PhotoRules.MaxItemsPerRequest)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["items"] = ["media.items_count_invalid"], // 1..30 per request
            });
        }

        var guestName = Truncate(request.GuestName, PhotoRules.MaxGuestNameLength);

        // Validate ALL items first (all-or-nothing, per-item error keys).
        var errors = new Dictionary<string, string[]>();
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var contentType = (item.ContentType ?? "").Trim();

            if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            {
                errors[$"items[{i}].contentType"] = ["media.video_not_supported_yet"]; // multipart flow, next block
            }
            else if (!PhotoRules.AllowedContentTypes.ContainsKey(contentType))
            {
                errors[$"items[{i}].contentType"] = ["media.type_unsupported"];
            }

            if (item.SizeBytes is <= 0 or > PhotoRules.MaxBytes)
            {
                errors[$"items[{i}].sizeBytes"] = ["media.file_too_large"]; // > 0 and <= 50 MB
            }
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        // Create Pending items + presigned URLs.
        var responses = new List<GuestUploadItemResponse>(items.Count);
        var expiresAt = now.Add(UploadUrlExpiry);

        foreach (var item in items)
        {
            var contentType = item.ContentType.Trim();
            var mediaId = Guid.NewGuid();
            var key = PhotoRules.NewKey(ev.EventId, mediaId, contentType);

            db.Set<MediaItem>().Add(new MediaItem
            {
                Id = mediaId,
                EventId = ev.EventId,
                Type = MediaType.Photo,
                ObjectKey = key,
                ContentType = contentType,
                SizeBytes = item.SizeBytes,
                FileName = Truncate(item.FileName, PhotoRules.MaxFileNameLength),
                GuestName = guestName,
                UploadStatus = MediaUploadStatus.Pending,
                Visibility = MediaVisibility.Visible,
                CreatedAt = now,
            });

            var uploadUrl = await storage.PresignPutAsync(key, contentType, UploadUrlExpiry, ct);
            responses.Add(new GuestUploadItemResponse(mediaId, key, uploadUrl.ToString(), contentType, expiresAt));
        }

        await db.SaveChangesAsync(ct);

        return Results.Ok(new GuestUploadResponse(responses));
    }

    /// <summary>
    /// Step 2: the browser finished the PUT — verify the object on R2 (exists,
    /// non-empty, within the per-file limit), then mark Confirmed and store the
    /// ACTUAL size. Idempotent: confirming a Confirmed item returns 200.
    /// M2 hooks in here (enqueue thumbnail job).
    /// </summary>
    private static async Task<IResult> ConfirmUploadAsync(
        string token,
        Guid mediaId,
        IGuestEventAccess guestEvents,
        DbContext db,
        IObjectStorage storage,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        var ev = await guestEvents.FindByTokenAsync(token, ct);
        if (ev is null)
        {
            return Results.NotFound();
        }

        // Scoped to the token's event — a mediaId from another event is a 404.
        var item = await db.Set<MediaItem>()
            .SingleOrDefaultAsync(m => m.Id == mediaId && m.EventId == ev.EventId && m.SoftDeletedAt == null, ct);

        if (item is null)
        {
            return Results.NotFound();
        }

        if (item.UploadStatus == MediaUploadStatus.Confirmed)
        {
            return Results.Ok(new GuestConfirmResponse(item.Id, item.UploadStatus.ToString(), item.SizeBytes));
        }

        var info = await storage.HeadAsync(item.ObjectKey, ct);
        if (info is null)
        {
            // Object never arrived — stays Pending so the guest can retry the PUT;
            // stale pendings are purged by the cleanup job (backlog).
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["mediaId"] = ["media.not_uploaded"],
            });
        }

        if (info.SizeBytes == 0 || info.SizeBytes > PhotoRules.MaxBytes)
        {
            // Broken or oversized upload that bypassed the declared size —
            // remove the object and mark the item Failed.
            await storage.DeleteAsync(item.ObjectKey, ct);
            item.UploadStatus = MediaUploadStatus.Failed;
            await db.SaveChangesAsync(ct);

            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["mediaId"] = [info.SizeBytes == 0 ? "media.file_empty" : "media.file_too_large"],
            });
        }

        item.UploadStatus = MediaUploadStatus.Confirmed;
        item.SizeBytes = info.SizeBytes; // trust the verified size, not the declared one
        item.ConfirmedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(ct);

        return Results.Ok(new GuestConfirmResponse(item.Id, item.UploadStatus.ToString(), item.SizeBytes));
    }

    private static string? Truncate(string? value, int max)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed[..Math.Min(trimmed.Length, max)];
    }
}
