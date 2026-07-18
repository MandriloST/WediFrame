using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WediFrame.Modules.Events.Contracts;
using WediFrame.Modules.Events.Domain;
using WediFrame.Modules.Events.Services;
using WediFrame.Shared.Auth;
using WediFrame.Shared.Options;
using WediFrame.Shared.Storage;

namespace WediFrame.Modules.Events.Endpoints;

/// <summary>
/// Host-facing event endpoints (M1 scope): create draft, list own, detail, QR,
/// cover photo (presigned upload + confirm).
/// Activate/PATCH/token-rotate/stats arrive with their backlog items.
/// </summary>
public static class EventEndpoints
{
    /// <summary>Presigned PUT URLs are short-lived; cover display URLs live longer.</summary>
    private static readonly TimeSpan UploadUrlExpiry = TimeSpan.FromMinutes(15);
    internal static readonly TimeSpan ViewUrlExpiry = TimeSpan.FromHours(1);

    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/events").RequireAuthorization();

        group.MapPost("/", CreateAsync);
        group.MapGet("/", ListAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapGet("/{id:guid}/qr", GetQrAsync);
        group.MapPost("/{id:guid}/cover", StartCoverUploadAsync);
        group.MapPost("/{id:guid}/cover/confirm", ConfirmCoverAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreateEventRequest request,
        ClaimsPrincipal principal,
        DbContext db,
        IOptions<FrontendOptions> frontend,
        IObjectStorage storage,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        if (principal.GetUserId() is not { } userId)
        {
            return Results.Unauthorized();
        }

        var title = (request.Title ?? "").Trim();
        var errors = new Dictionary<string, string[]>();

        if (title.Length is < 1 or > 200)
        {
            errors["title"] = ["events.title_length"]; // 1–200 characters
        }

        var type = (request.Type ?? EventTypes.Wedding).Trim().ToLowerInvariant();
        if (type != EventTypes.Wedding)
        {
            errors["type"] = ["events.type_unsupported"]; // only "wedding" in MVP
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var entity = new Event
        {
            Id = Guid.NewGuid(),
            OwnerUserId = userId,
            Type = type,
            Title = title,
            UploadStartDate = request.UploadStartDate,
            Status = EventStatus.Draft,
            GuestToken = GuestTokenGenerator.NewToken(),
            CreatedAt = timeProvider.GetUtcNow(),
        };

        db.Set<Event>().Add(entity);
        await db.SaveChangesAsync(ct);

        var response = await ToResponseAsync(entity, frontend.Value, storage, ct);
        return Results.Created($"/api/v1/events/{entity.Id}", response);
    }

    private static async Task<IResult> ListAsync(
        ClaimsPrincipal principal,
        DbContext db,
        IOptions<FrontendOptions> frontend,
        IObjectStorage storage,
        CancellationToken ct)
    {
        if (principal.GetUserId() is not { } userId)
        {
            return Results.Unauthorized();
        }

        var events = await db.Set<Event>()
            .Where(e => e.OwnerUserId == userId && e.Status != EventStatus.Deleted)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);

        var responses = new List<EventResponse>(events.Count);
        foreach (var e in events)
        {
            responses.Add(await ToResponseAsync(e, frontend.Value, storage, ct));
        }

        return Results.Ok(responses);
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        ClaimsPrincipal principal,
        DbContext db,
        IOptions<FrontendOptions> frontend,
        IObjectStorage storage,
        CancellationToken ct)
    {
        var entity = await FindOwnedAsync(id, principal, db, ct);
        return entity is null
            ? Results.NotFound()
            : Results.Ok(await ToResponseAsync(entity, frontend.Value, storage, ct));
    }

    private static async Task<IResult> GetQrAsync(
        Guid id,
        string? format,
        int? size,
        ClaimsPrincipal principal,
        DbContext db,
        IQrCodeService qrCodeService,
        IOptions<FrontendOptions> frontend,
        CancellationToken ct)
    {
        var entity = await FindOwnedAsync(id, principal, db, ct);
        if (entity is null)
        {
            return Results.NotFound();
        }

        var guestUrl = frontend.Value.BuildGuestUrl(entity.GuestToken);
        var pixelsPerModule = Math.Clamp(size ?? 20, 4, 50);

        return (format ?? "png").ToLowerInvariant() switch
        {
            "svg" => Results.Text(qrCodeService.CreateSvg(guestUrl, pixelsPerModule), "image/svg+xml"),
            "png" => Results.File(qrCodeService.CreatePng(guestUrl, pixelsPerModule), "image/png",
                fileDownloadName: $"wediframe-qr-{entity.Id}.png"),
            _ => Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["format"] = ["events.qr_format_unsupported"], // png | svg
            }),
        };
    }

    /// <summary>
    /// Step 1 of the cover flow: validate + hand out a presigned PUT URL.
    /// The browser uploads directly to R2 (files never pass through the API).
    /// </summary>
    private static async Task<IResult> StartCoverUploadAsync(
        Guid id,
        CoverUploadRequest request,
        ClaimsPrincipal principal,
        DbContext db,
        IObjectStorage storage,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        var entity = await FindOwnedAsync(id, principal, db, ct);
        if (entity is null)
        {
            return Results.NotFound();
        }

        var errors = new Dictionary<string, string[]>();
        var contentType = (request.ContentType ?? "").Trim();

        if (!CoverPhotoRules.AllowedContentTypes.ContainsKey(contentType))
        {
            errors["contentType"] = ["events.cover_type_unsupported"]; // jpeg | png | webp
        }

        if (request.SizeBytes is <= 0 or > CoverPhotoRules.MaxBytes)
        {
            errors["sizeBytes"] = ["events.cover_too_large"]; // > 0 and <= 20 MB
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var key = CoverPhotoRules.NewKey(entity.Id, contentType);
        var uploadUrl = await storage.PresignPutAsync(key, contentType, UploadUrlExpiry, ct);

        return Results.Ok(new CoverUploadResponse(
            key,
            uploadUrl.ToString(),
            contentType,
            timeProvider.GetUtcNow().Add(UploadUrlExpiry),
            CoverPhotoRules.MaxBytes));
    }

    /// <summary>
    /// Step 2: the browser finished the PUT — verify the object really exists on R2
    /// and respects the rules, then attach it to the event (replacing the old cover).
    /// Stateless on purpose: the key travels with the request, ownership is proven
    /// by the enforced "events/{id}/cover/" prefix. Idempotent for the same key.
    /// </summary>
    private static async Task<IResult> ConfirmCoverAsync(
        Guid id,
        CoverConfirmRequest request,
        ClaimsPrincipal principal,
        DbContext db,
        IObjectStorage storage,
        IOptions<FrontendOptions> frontend,
        CancellationToken ct)
    {
        var entity = await FindOwnedAsync(id, principal, db, ct);
        if (entity is null)
        {
            return Results.NotFound();
        }

        var key = (request.Key ?? "").Trim();
        if (!key.StartsWith(CoverPhotoRules.KeyPrefix(entity.Id), StringComparison.Ordinal))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["key"] = ["events.cover_key_invalid"],
            });
        }

        var info = await storage.HeadAsync(key, ct);
        if (info is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["key"] = ["events.cover_not_uploaded"], // object not found on storage
            });
        }

        if (info.SizeBytes == 0)
        {
            // Empty object = broken upload (e.g. a client that sent no body).
            await storage.DeleteAsync(key, ct);
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["key"] = ["events.cover_empty"],
            });
        }

        if (info.SizeBytes > CoverPhotoRules.MaxBytes)
        {
            // Uploaded object bypassed the declared size — remove it and reject.
            await storage.DeleteAsync(key, ct);
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["key"] = ["events.cover_too_large"],
            });
        }

        var previousKey = entity.CoverPhotoKey;
        entity.CoverPhotoKey = key;
        await db.SaveChangesAsync(ct);

        // Best effort cleanup of the replaced cover (after the DB switch, so a
        // failure here can never leave the event pointing at a deleted object).
        if (previousKey is not null && previousKey != key)
        {
            await storage.DeleteAsync(previousKey, ct);
        }

        return Results.Ok(await ToResponseAsync(entity, frontend.Value, storage, ct));
    }

    /// <summary>Owner check by query — a foreign event id yields 404, never 403 (no existence leak).</summary>
    private static Task<Event?> FindOwnedAsync(Guid id, ClaimsPrincipal principal, DbContext db, CancellationToken ct)
        => principal.GetUserId() is not { } userId
            ? Task.FromResult<Event?>(null)
            : db.Set<Event>()
                .SingleOrDefaultAsync(e => e.Id == id && e.OwnerUserId == userId && e.Status != EventStatus.Deleted, ct);

    private static async Task<EventResponse> ToResponseAsync(
        Event e, FrontendOptions frontend, IObjectStorage storage, CancellationToken ct)
    {
        var coverUrl = e.CoverPhotoKey is null
            ? null
            : (await storage.PresignGetAsync(e.CoverPhotoKey, ViewUrlExpiry, ct)).ToString();

        return new EventResponse(e.Id, e.Title, e.Type, e.UploadStartDate, e.Status.ToString(),
            e.GuestToken, frontend.BuildGuestUrl(e.GuestToken), e.CoverPhotoKey, coverUrl, e.CreatedAt);
    }
}
