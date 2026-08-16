using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WediFrame.Modules.Media.Domain;
using WediFrame.Shared.Storage;

namespace WediFrame.Modules.Media.Services;

/// <summary>
/// Background worker that packages a whole event's gallery into one ZIP. The
/// queue is DB state (<see cref="MediaExport"/> rows with status Pending, or a
/// stale Running row from a crashed run) — no extra infrastructure, same shape
/// as the thumbnail worker. One job at a time (each is heavy).
///
/// Memory stays bounded: every original is streamed R2 → ZIP entry, the archive
/// is written to a temp file, then the temp file is streamed back up to R2. The
/// API never holds a whole file (or the whole archive) in memory. This is the
/// sanctioned server-side use of the storage Download/Upload path — it runs off
/// the request path, never on a guest/host request.
/// </summary>
public sealed class ExportWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ExportOptions> options,
    TimeProvider timeProvider,
    ILogger<ExportWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opt = options.Value;
        if (!opt.Enabled)
        {
            logger.LogInformation("Export worker is disabled.");
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
            var didWork = false;
            try
            {
                didWork = await ProcessNextAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Export poll failed; retrying next tick.");
            }

            // If we processed a job there may be more queued — loop immediately.
            if (didWork)
            {
                continue;
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

    /// <summary>Claim and process the next job. Returns false when the queue is empty.</summary>
    private async Task<bool> ProcessNextAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IObjectStorage>();
        var opt = options.Value;
        var now = timeProvider.GetUtcNow();
        var staleBefore = now - opt.StaleRunningAfter;

        // Pending, or a Running row a crashed worker abandoned (oldest first).
        var job = await db.Set<MediaExport>()
            .Where(e => e.Status == MediaExportStatus.Pending
                || (e.Status == MediaExportStatus.Running && e.StartedAt != null && e.StartedAt < staleBefore))
            .OrderBy(e => e.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (job is null)
        {
            return false;
        }

        job.Status = MediaExportStatus.Running;
        job.StartedAt = now;
        job.Error = null;
        await db.SaveChangesAsync(ct);

        var tempPath = Path.Combine(Path.GetTempPath(), $"wediframe-export-{job.Id:N}.zip");
        try
        {
            var (itemCount, zipSize) = await BuildZipAsync(db, storage, job.EventId, tempPath, ct);

            var key = ExportRules.ObjectKey(job.EventId, job.Id);
            await using (var upload = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                await storage.UploadAsync(key, upload, ExportRules.ZipContentType, ct);
            }

            job.Status = MediaExportStatus.Ready;
            job.ObjectKey = key;
            job.ItemCount = itemCount;
            job.SizeBytes = zipSize;
            job.CompletedAt = timeProvider.GetUtcNow();
            job.ExpiresAt = job.CompletedAt + opt.ZipTtl;
            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "Export {ExportId} ready: {ItemCount} items, {Bytes} bytes.",
                job.Id, itemCount, zipSize);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Shutting down: leave the row Running so a later run re-claims it.
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Export {ExportId} failed.", job.Id);
            job.Status = MediaExportStatus.Failed;
            job.Error = "export.failed";
            job.CompletedAt = timeProvider.GetUtcNow();
            await db.SaveChangesAsync(CancellationToken.None);
        }
        finally
        {
            TryDelete(tempPath);
        }

        return true;
    }

    /// <summary>
    /// Stream every confirmed, non-deleted item (visible AND hidden — this is the
    /// host's full archive) into a ZIP on disk. Returns (item count, zip size).
    /// A missing original is skipped, not fatal.
    /// </summary>
    private async Task<(int ItemCount, long ZipSize)> BuildZipAsync(
        DbContext db, IObjectStorage storage, Guid eventId, string tempPath, CancellationToken ct)
    {
        var items = await db.Set<MediaItem>()
            .Where(m => m.EventId == eventId
                && m.UploadStatus == MediaUploadStatus.Confirmed
                && m.SoftDeletedAt == null)
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.ObjectKey)
            .Select(m => new { m.Id, m.Type, m.ObjectKey, m.ContentType, m.FileName })
            .ToListAsync(ct);

        var included = 0;
        await using (var file = new FileStream(tempPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
        using (var archive = new ZipArchive(file, ZipArchiveMode.Create, leaveOpen: false))
        {
            var seq = 1;
            foreach (var item in items)
            {
                ct.ThrowIfCancellationRequested();

                await using var source = await storage.OpenReadAsync(item.ObjectKey, ct);
                if (source is null)
                {
                    logger.LogWarning("Export skipping missing object {Key}.", item.ObjectKey);
                    continue;
                }

                var entryName = ExportRules.EntryName(seq, item.Type, item.ContentType, item.FileName, item.Id);
                // Media is already compressed — store, don't waste CPU re-compressing.
                var entry = archive.CreateEntry(entryName, CompressionLevel.NoCompression);
                await using var entryStream = entry.Open();
                await source.CopyToAsync(entryStream, ct);

                seq++;
                included++;
            }
        }

        var zipSize = new FileInfo(tempPath).Length;
        return (included, zipSize);
    }

    private void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not delete temp export file {Path}.", path);
        }
    }
}
