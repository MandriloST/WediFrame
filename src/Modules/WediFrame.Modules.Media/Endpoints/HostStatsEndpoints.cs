using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WediFrame.Modules.Events.Services;
using WediFrame.Modules.Media.Contracts;
using WediFrame.Modules.Media.Domain;
using WediFrame.Shared.Auth;

namespace WediFrame.Modules.Media.Endpoints;

/// <summary>
/// Host stats (ARCHITECTURE.md §4, GET /events/{id}/stats): how full is my event
/// vs the package limits. Reports CONFIRMED, non-deleted usage — the honest
/// "stored" number (pending/failed uploads don't count). Authenticated + ownership
/// via the Events module's <see cref="IHostEventAccess"/>; limits come from the
/// event's package on that same context (no direct Billing access here).
/// </summary>
public static class HostStatsEndpoints
{
    public static IEndpointRouteBuilder MapHostStatsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/events/{eventId:guid}/stats", GetStatsAsync).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> GetStatsAsync(
        Guid eventId,
        ClaimsPrincipal principal,
        IHostEventAccess hostEvents,
        DbContext db,
        CancellationToken ct)
    {
        if (principal.GetUserId() is not { } userId)
        {
            return Results.Unauthorized();
        }

        var ev = await hostEvents.FindOwnedAsync(eventId, userId, ct);
        if (ev is null)
        {
            return Results.NotFound();
        }

        var agg = await db.Set<MediaItem>()
            .Where(m => m.EventId == eventId
                && m.SoftDeletedAt == null
                && m.UploadStatus == MediaUploadStatus.Confirmed)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                PhotoCount = g.Count(m => m.Type == MediaType.Photo),
                TotalBytes = g.Sum(m => m.SizeBytes),
                VideoBytes = g.Sum(m => m.Type == MediaType.Video ? m.SizeBytes : 0L),
            })
            .FirstOrDefaultAsync(ct);

        return Results.Ok(new EventStatsResponse(
            agg?.PhotoCount ?? 0, ev.Limits?.MaxPhotoCount,
            agg?.VideoBytes ?? 0L, ev.Limits?.MaxVideoTotalBytes,
            agg?.TotalBytes ?? 0L, ev.Limits?.MaxTotalBytes,
            ev.PackageSlug, ev.PackageName,
            ev.UploadStartDate, ev.UploadEndsAt, ev.ExpiresAt));
    }
}
