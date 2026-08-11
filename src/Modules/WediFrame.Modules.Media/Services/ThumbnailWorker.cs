using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WediFrame.Modules.Media.Domain;
using WediFrame.Shared.Imaging;
using WediFrame.Shared.Storage;

namespace WediFrame.Modules.Media.Services;

/// <summary>
/// Background worker that turns confirmed guest photos into small JPEG
/// thumbnails. The "queue" is DB state (Confirmed photos with ThumbnailStatus
/// Pending) — no extra infrastructure. Runs in the API process (single Railway
/// service). Idempotent and self-healing: a crash mid-batch just leaves items
/// Pending for the next poll; a re-run overwrites the same thumbnail key.
///
/// Failure handling: a file that cannot be decoded (ThumbnailFormatException)
/// is marked Failed and skipped forever. Infrastructure errors (R2/DB) bubble
/// up, are logged, and retried on the next poll — items stay Pending.
/// </summary>
public sealed class ThumbnailWorker(
    IServiceScopeFactory scopeFactory,
    IThumbnailGenerator generator,
    IOptions<ThumbnailOptions> options,
    ILogger<ThumbnailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opt = options.Value;
        if (!opt.Enabled)
        {
            logger.LogInformation("Thumbnail worker is disabled.");
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
            var drainedFullBatch = false;
            try
            {
                var processed = await ProcessBatchAsync(stoppingToken);
                drainedFullBatch = processed >= opt.BatchSize;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Infrastructure hiccup (R2/DB/native lib). Log and retry next tick.
                logger.LogError(ex, "Thumbnail batch failed; retrying next poll.");
            }

            // A full batch likely means backlog — loop again without waiting.
            if (drainedFullBatch)
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

    private async Task<int> ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IObjectStorage>();
        var opt = options.Value;

        var batch = await db.Set<MediaItem>()
            .Where(m => m.Type == MediaType.Photo
                && m.UploadStatus == MediaUploadStatus.Confirmed
                && m.ThumbnailStatus == MediaThumbnailStatus.Pending
                && m.SoftDeletedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(opt.BatchSize)
            .ToListAsync(ct);

        if (batch.Count == 0)
        {
            return 0;
        }

        foreach (var item in batch)
        {
            ct.ThrowIfCancellationRequested();

            var original = await storage.DownloadAsync(item.ObjectKey, ct);
            if (original is null)
            {
                logger.LogWarning(
                    "Original missing for media {MediaId} ({Key}); marking thumbnail Failed.",
                    item.Id, item.ObjectKey);
                item.ThumbnailStatus = MediaThumbnailStatus.Failed;
                continue;
            }

            byte[] jpeg;
            try
            {
                jpeg = generator.CreateJpegThumbnail(original.Content, opt.MaxEdge, opt.JpegQuality);
            }
            catch (ThumbnailFormatException ex)
            {
                logger.LogWarning(
                    ex, "Could not thumbnail media {MediaId} ({ContentType}); marking Failed.",
                    item.Id, item.ContentType);
                item.ThumbnailStatus = MediaThumbnailStatus.Failed;
                continue;
            }

            var thumbnailKey = PhotoRules.ThumbnailKey(item.EventId, item.Id);
            await storage.UploadAsync(thumbnailKey, jpeg, PhotoRules.ThumbnailContentType, ct);

            item.ThumbnailKey = thumbnailKey;
            item.ThumbnailStatus = MediaThumbnailStatus.Ready;
        }

        await db.SaveChangesAsync(ct);
        return batch.Count;
    }
}
