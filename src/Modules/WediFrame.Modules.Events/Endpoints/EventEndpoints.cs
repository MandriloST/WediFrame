using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WediFrame.Modules.Billing.Services;
using WediFrame.Modules.Events.Contracts;
using WediFrame.Modules.Events.Domain;
using WediFrame.Modules.Events.Services;
using WediFrame.Shared.Audit;
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

    // Free-tier abuse guard: how many live (Active/UploadClosed) free events one
    // user may have at once. Assumption (owner may revise) — kept as a constant
    // for now; move to config if it needs changing without a redeploy.
    private const int MaxActiveFreeEventsPerUser = 1;

    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/events").RequireAuthorization();

        group.MapPost("/", CreateAsync);
        group.MapGet("/", ListAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("/{id:guid}/activate", ActivateAsync);
        group.MapPost("/{id:guid}/close-upload", CloseUploadAsync);
        group.MapPost("/{id:guid}/reopen-upload", ReopenUploadAsync);
        group.MapGet("/{id:guid}/qr", GetQrAsync);
        group.MapPost("/{id:guid}/cover", StartCoverUploadAsync);
        group.MapPost("/{id:guid}/cover/confirm", ConfirmCoverAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreateEventRequest request,
        ClaimsPrincipal principal,
        DbContext db,
        IPackageCatalog packages,
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

        // Package choice — defaults to Free/Trial when omitted (M3).
        var slug = string.IsNullOrWhiteSpace(request.PackageSlug) ? "free" : request.PackageSlug;
        var package = await packages.GetBySlugAsync(slug, ct);
        if (package is null)
        {
            errors["packageSlug"] = ["events.package_invalid"]; // unknown/inactive package
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
            PackageId = package!.Id,
            Status = EventStatus.Draft,
            GuestToken = GuestTokenGenerator.NewToken(),
            CreatedAt = timeProvider.GetUtcNow(),
        };

        db.Set<Event>().Add(entity);
        await db.SaveChangesAsync(ct);

        var response = await ToResponseAsync(entity, frontend.Value, storage, packages, ct);
        return Results.Created($"/api/v1/events/{entity.Id}", response);
    }

    private static async Task<IResult> ListAsync(
        ClaimsPrincipal principal,
        DbContext db,
        IPackageCatalog packages,
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
            responses.Add(await ToResponseAsync(e, frontend.Value, storage, packages, ct));
        }

        return Results.Ok(responses);
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        ClaimsPrincipal principal,
        DbContext db,
        IPackageCatalog packages,
        IOptions<FrontendOptions> frontend,
        IObjectStorage storage,
        CancellationToken ct)
    {
        var entity = await FindOwnedAsync(id, principal, db, ct);
        return entity is null
            ? Results.NotFound()
            : Results.Ok(await ToResponseAsync(entity, frontend.Value, storage, packages, ct));
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
        IPackageCatalog packages,
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

        return Results.Ok(await ToResponseAsync(entity, frontend.Value, storage, packages, ct));
    }

    /// <summary>Owner check by query — a foreign event id yields 404, never 403 (no existence leak).</summary>
    private static Task<Event?> FindOwnedAsync(Guid id, ClaimsPrincipal principal, DbContext db, CancellationToken ct)
        => principal.GetUserId() is not { } userId
            ? Task.FromResult<Event?>(null)
            : db.Set<Event>()
                .SingleOrDefaultAsync(e => e.Id == id && e.OwnerUserId == userId && e.Status != EventStatus.Deleted, ct);

    /// <summary>
    /// Free activation (MVP): Draft → Active so the guest link starts working.
    /// Once Billing (M3) exists, paid packages activate only after payment; Free
    /// stays immediate. Idempotent for already-Active events.
    /// </summary>
    private static async Task<IResult> ActivateAsync(
        Guid id,
        ClaimsPrincipal principal,
        DbContext db,
        IPackageCatalog packages,
        IOptions<FrontendOptions> frontend,
        IObjectStorage storage,
        CancellationToken ct)
    {
        if (principal.GetUserId() is not { } userId)
        {
            return Results.Unauthorized();
        }

        var entity = await db.Set<Event>()
            .SingleOrDefaultAsync(e => e.Id == id && e.OwnerUserId == userId, ct);

        if (entity is null)
        {
            return Results.NotFound();
        }

        if (entity.Status == EventStatus.Draft)
        {
            var package = entity.PackageId is { } pid ? await packages.GetByIdAsync(pid, ct) : null;

            // Paid packages activate only after payment (Stripe — next M3 step).
            if (package is { IsFree: false })
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["status"] = ["events.payment_required"],
                });
            }

            // Abuse guard for the Free tier: a user may run at most
            // MaxActiveFreeEventsPerUser live free events at once (Draft doesn't
            // count — it consumes nothing). Paid events are never limited.
            if (package is { IsFree: true })
            {
                var activeFree = await db.Set<Event>().CountAsync(
                    e => e.OwnerUserId == userId
                        && e.PackageId == package.Id
                        && (e.Status == EventStatus.Active || e.Status == EventStatus.UploadClosed),
                    ct);

                if (activeFree >= MaxActiveFreeEventsPerUser)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["status"] = ["events.free_limit_reached"],
                    });
                }
            }

            entity.Status = EventStatus.Active;

            // Lock the timeline from the package + T0. (Legacy events without a
            // package activate without derived dates — backward compatible.)
            if (package is not null)
            {
                entity.UploadEndsAt = entity.UploadStartDate.AddDays(package.UploadPeriodDays);
                entity.ExpiresAt = entity.UploadStartDate.AddDays(package.RetentionDays);
            }

            await db.SaveChangesAsync(ct);
        }
        else if (entity.Status != EventStatus.Active)
        {
            // Expired / UploadClosed / Deleted can't be (re)activated here.
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["status"] = ["events.cannot_activate"],
            });
        }

        return Results.Ok(await ToResponseAsync(entity, frontend.Value, storage, packages, ct));
    }

    /// <summary>
    /// Host closes the upload period early (Active → UploadClosed): guests keep
    /// the gallery but can no longer upload. Idempotent for already-closed events.
    /// Retention (M4) will flip the SAME status automatically once packages define
    /// the period; this manual control stays useful regardless.
    /// </summary>
    private static Task<IResult> CloseUploadAsync(
        Guid id, ClaimsPrincipal principal, DbContext db, IPackageCatalog packages,
        IOptions<FrontendOptions> frontend, IObjectStorage storage,
        TimeProvider timeProvider, CancellationToken ct)
        => SetUploadClosedAsync(id, closed: true, principal, db, packages, frontend, storage, timeProvider, ct);

    /// <summary>
    /// Host reopens a closed upload period (UploadClosed → Active). Idempotent for
    /// already-open events. Only Active/UploadClosed events can toggle here.
    /// </summary>
    private static Task<IResult> ReopenUploadAsync(
        Guid id, ClaimsPrincipal principal, DbContext db, IPackageCatalog packages,
        IOptions<FrontendOptions> frontend, IObjectStorage storage,
        TimeProvider timeProvider, CancellationToken ct)
        => SetUploadClosedAsync(id, closed: false, principal, db, packages, frontend, storage, timeProvider, ct);

    private static async Task<IResult> SetUploadClosedAsync(
        Guid id, bool closed, ClaimsPrincipal principal, DbContext db, IPackageCatalog packages,
        IOptions<FrontendOptions> frontend, IObjectStorage storage,
        TimeProvider timeProvider, CancellationToken ct)
    {
        if (principal.GetUserId() is not { } userId)
        {
            return Results.Unauthorized();
        }

        var entity = await db.Set<Event>()
            .SingleOrDefaultAsync(e => e.Id == id && e.OwnerUserId == userId, ct);

        if (entity is null)
        {
            return Results.NotFound();
        }

        var target = closed ? EventStatus.UploadClosed : EventStatus.Active;
        var source = closed ? EventStatus.Active : EventStatus.UploadClosed;

        if (entity.Status == source)
        {
            entity.Status = target;
            db.Set<AuditLogEntry>().Add(new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                OccurredAt = timeProvider.GetUtcNow(),
                ActorUserId = userId,
                Action = closed ? "event.upload_closed" : "event.upload_reopened",
                EntityType = nameof(Event),
                EntityId = entity.Id.ToString(),
            });
            await db.SaveChangesAsync(ct);
        }
        else if (entity.Status != target)
        {
            // Draft/Expired/Deleted can't toggle the upload period.
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["status"] = [closed ? "events.cannot_close_upload" : "events.cannot_reopen_upload"],
            });
        }

        return Results.Ok(await ToResponseAsync(entity, frontend.Value, storage, packages, ct));
    }

    private static async Task<EventResponse> ToResponseAsync(
        Event e, FrontendOptions frontend, IObjectStorage storage, IPackageCatalog packages, CancellationToken ct)
    {
        var coverUrl = e.CoverPhotoKey is null
            ? null
            : (await storage.PresignGetAsync(e.CoverPhotoKey, ViewUrlExpiry, ct)).ToString();

        var package = e.PackageId is { } pid ? await packages.GetByIdAsync(pid, ct) : null;

        return new EventResponse(e.Id, e.Title, e.Type, e.UploadStartDate, e.Status.ToString(),
            e.GuestToken, frontend.BuildGuestUrl(e.GuestToken), e.CoverPhotoKey, coverUrl, e.CreatedAt,
            package?.Slug, package?.Name, e.UploadEndsAt, e.ExpiresAt);
    }
}
