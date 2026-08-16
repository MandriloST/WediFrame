using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WediFrame.Modules.Events.Services;
using WediFrame.Modules.Media.Services;

namespace WediFrame.Modules.Retention.Services;

/// <summary>
/// Background worker that drives the event lifecycle end (M4). It owns only the
/// *schedule*; the actual work lives behind module contracts (Retention →
/// Events + Media, one-way, no cycle). Same shape as Media's thumbnail/export
/// workers: runs in the API process (single Railway service), the DB is the
/// source of truth, idempotent and self-healing (a crash just leaves work for
/// the next tick).
///
/// Each tick:
///   Phase 1 — status transitions (Active→UploadClosed→Expired) via IEventRetention.
///   Phase 2 — after grace, physically erase Expired events: IEventMediaPurge
///             deletes the media (R2 + rows), then IEventRetention finalizes
///             (cover + status Deleted + audit). Purge runs BEFORE finalize so a
///             failure never leaves a Deleted event with orphaned bytes.
/// </summary>
public sealed class RetentionWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<RetentionOptions> options,
    ILogger<RetentionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opt = options.Value;
        if (!opt.Enabled)
        {
            logger.LogInformation("Retention worker is disabled.");
            return;
        }

        try
        {
            await Task.Delay(opt.InitialDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(opt.PollInterval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepAsync(stoppingToken);
                await PurgeDueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Infrastructure hiccup (DB). Log and retry on the next tick.
                logger.LogError(ex, "Retention sweep failed; retrying next poll.");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task SweepAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var retention = scope.ServiceProvider.GetRequiredService<IEventRetention>();

        var result = await retention.SweepAsync(ct);
        if (result.Total > 0)
        {
            logger.LogInformation(
                "Retention sweep: {UploadClosed} upload-closed, {Expired} expired.",
                result.UploadClosed, result.Expired);
        }
    }

    private async Task PurgeDueAsync(CancellationToken ct)
    {
        var graceDays = options.Value.GraceDays;

        IReadOnlyList<Guid> due;
        using (var scope = scopeFactory.CreateScope())
        {
            var retention = scope.ServiceProvider.GetRequiredService<IEventRetention>();
            due = await retention.FindDueForPurgeAsync(graceDays, ct);
        }

        if (due.Count == 0)
        {
            return;
        }

        var purged = 0;
        foreach (var eventId in due)
        {
            if (ct.IsCancellationRequested)
            {
                break;
            }

            try
            {
                // Fresh scope per event: one failure never blocks the rest, and
                // each purge+finalize is its own unit of work.
                using var scope = scopeFactory.CreateScope();
                var purge = scope.ServiceProvider.GetRequiredService<IEventMediaPurge>();
                var retention = scope.ServiceProvider.GetRequiredService<IEventRetention>();

                // Erase media FIRST, mark the event Deleted only after — so a
                // failure never leaves a Deleted event with orphaned R2 bytes.
                var result = await purge.PurgeAsync(eventId, ct);
                await retention.FinalizeDeletionAsync(eventId, result.MediaDeleted, result.ExportsDeleted, ct);
                purged++;

                logger.LogInformation(
                    "Retention purge: event {EventId} erased ({Media} media, {Exports} exports, {Objects} R2 objects).",
                    eventId, result.MediaDeleted, result.ExportsDeleted, result.ObjectsDeleted);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Idempotent: a failed event stays Expired and is retried next poll.
                logger.LogError(ex, "Retention purge failed for event {EventId}; will retry next poll.", eventId);
            }
        }

        if (purged > 0)
        {
            logger.LogInformation("Retention purge: {Count} event(s) hard-deleted after grace.", purged);
        }
    }
}
