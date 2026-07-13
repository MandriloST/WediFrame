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

namespace WediFrame.Modules.Events.Endpoints;

/// <summary>
/// Host-facing event endpoints (M1 scope): create draft, list own, detail, QR.
/// Activate/PATCH/token-rotate/stats arrive with their backlog items.
/// </summary>
public static class EventEndpoints
{
    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/events").RequireAuthorization();

        group.MapPost("/", CreateAsync);
        group.MapGet("/", ListAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapGet("/{id:guid}/qr", GetQrAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreateEventRequest request,
        ClaimsPrincipal principal,
        DbContext db,
        IOptions<FrontendOptions> frontend,
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

        var response = ToResponse(entity, frontend.Value);
        return Results.Created($"/api/v1/events/{entity.Id}", response);
    }

    private static async Task<IResult> ListAsync(
        ClaimsPrincipal principal,
        DbContext db,
        IOptions<FrontendOptions> frontend,
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

        return Results.Ok(events.Select(e => ToResponse(e, frontend.Value)));
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        ClaimsPrincipal principal,
        DbContext db,
        IOptions<FrontendOptions> frontend,
        CancellationToken ct)
    {
        var entity = await FindOwnedAsync(id, principal, db, ct);
        return entity is null
            ? Results.NotFound()
            : Results.Ok(ToResponse(entity, frontend.Value));
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

    /// <summary>Owner check by query — a foreign event id yields 404, never 403 (no existence leak).</summary>
    private static Task<Event?> FindOwnedAsync(Guid id, ClaimsPrincipal principal, DbContext db, CancellationToken ct)
        => principal.GetUserId() is not { } userId
            ? Task.FromResult<Event?>(null)
            : db.Set<Event>()
                .SingleOrDefaultAsync(e => e.Id == id && e.OwnerUserId == userId && e.Status != EventStatus.Deleted, ct);

    private static EventResponse ToResponse(Event e, FrontendOptions frontend)
        => new(e.Id, e.Title, e.Type, e.UploadStartDate, e.Status.ToString(),
            e.GuestToken, frontend.BuildGuestUrl(e.GuestToken), e.CoverPhotoKey, e.CreatedAt);
}
