using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WediFrame.Modules.Events.Services;

namespace WediFrame.Modules.Retention.Services;

/// <summary>
/// Background worker that drives time-based event status transitions (M4,
/// Phase 1). It owns only the *schedule* — the actual mutation + audit lives in
/// the Events module behind <see cref="IEventRetention"/> (Retention → Events,
/// one-way, no cycle). Same shape as Media's thumbnail/export workers: runs in
/// the API process (single Railway service), the DB is the source of truth, and
/// it is idempotent and self-healing (a crash mid-sweep just leaves work for the
/// next tick).
///
/// Phase 1 does NO physical deletion — only Active→UploadClosed and →Expired
/// flips. R2 media purge after a grace period is Phase 2.
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
}
