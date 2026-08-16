using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WediFrame.Modules.Events.Services;
using WediFrame.Modules.Media.Contracts;
using WediFrame.Modules.Media.Domain;
using WediFrame.Shared.Auth;
using WediFrame.Shared.Storage;

namespace WediFrame.Modules.Media.Endpoints;

/// <summary>
/// Host-facing "download the whole gallery as a ZIP" (M2). Start creates (or
/// reuses) a background job; the actual archiving happens in <c>ExportWorker</c>.
/// The host polls status and, once Ready, gets a short-lived presigned download
/// URL. Authenticated + ownership-checked via <see cref="IHostEventAccess"/>.
/// </summary>
public static class HostExportEndpoints
{
    /// <summary>Download URLs for the finished ZIP are used right after the click.</summary>
    private static readonly TimeSpan DownloadUrlExpiry = TimeSpan.FromMinutes(10);

    public static IEndpointRouteBuilder MapHostExportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/events/{eventId:guid}/export").RequireAuthorization();

        group.MapPost("/", StartAsync);
        group.MapGet("/{jobId:guid}", StatusAsync);

        return endpoints;
    }

    /// <summary>
    /// Start an export. If one is already Pending/Running for this event we return
    /// it (no duplicate work); otherwise a new Pending job is queued for the worker.
    /// </summary>
    private static async Task<IResult> StartAsync(
        Guid eventId,
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

        var inFlight = await db.Set<MediaExport>()
            .Where(e => e.EventId == eventId
                && (e.Status == MediaExportStatus.Pending || e.Status == MediaExportStatus.Running))
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (inFlight is not null)
        {
            return Results.Ok(ToResponse(inFlight, null));
        }

        var job = new MediaExport
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            RequestedByUserId = userId,
            Status = MediaExportStatus.Pending,
            CreatedAt = timeProvider.GetUtcNow(),
        };

        db.Set<MediaExport>().Add(job);
        await db.SaveChangesAsync(ct);

        return Results.Ok(ToResponse(job, null));
    }

    /// <summary>Poll a job. When Ready and not expired, includes a presigned download URL.</summary>
    private static async Task<IResult> StatusAsync(
        Guid eventId,
        Guid jobId,
        ClaimsPrincipal principal,
        IHostEventAccess hostEvents,
        DbContext db,
        IObjectStorage storage,
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

        var job = await db.Set<MediaExport>()
            .SingleOrDefaultAsync(e => e.Id == jobId && e.EventId == eventId, ct);

        if (job is null)
        {
            return Results.NotFound();
        }

        string? downloadUrl = null;
        // Read-only status check — a plain UtcNow read is fine here (keeps this
        // handler within the analyzer's parameter limit; StartAsync owns writes).
        var expired = job.ExpiresAt is { } exp && exp <= DateTimeOffset.UtcNow;

        if (job.Status == MediaExportStatus.Ready && job.ObjectKey is not null && !expired)
        {
            var fileName = ExportRules.DownloadFileName(eventId);
            downloadUrl = (await storage.PresignDownloadAsync(job.ObjectKey, fileName, DownloadUrlExpiry, ct)).ToString();
        }

        return Results.Ok(ToResponse(job, downloadUrl));
    }

    private static ExportJobResponse ToResponse(MediaExport job, string? downloadUrl) =>
        new(
            job.Id,
            job.Status.ToString(),
            job.Status == MediaExportStatus.Ready ? job.ItemCount : null,
            job.Status == MediaExportStatus.Ready ? job.SizeBytes : null,
            downloadUrl,
            downloadUrl is null ? null : ExportRules.DownloadFileName(job.EventId),
            job.Status == MediaExportStatus.Failed ? job.Error : null);
}
