namespace WediFrame.Modules.Retention.Services;

/// <summary>
/// Config for the retention worker (section "Retention"). All values have sane
/// defaults, so the worker runs with no configuration. Override per env, e.g.
/// Retention__Enabled=false to turn it off on a given deploy. Transitions are
/// date-based (day granularity), so an infrequent poll is plenty.
/// </summary>
public sealed class RetentionOptions
{
    public const string SectionName = "Retention";

    /// <summary>Master switch. Disable to stop automatic status transitions on a deploy.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Delay before the first sweep, so startup isn't contended.</summary>
    public int InitialDelaySeconds { get; set; } = 30;

    /// <summary>Seconds between sweeps. Day-granular flips → hourly (3600 s) is ample.</summary>
    public int PollSeconds { get; set; } = 3600;

    /// <summary>
    /// Days an Expired event's media is kept (recoverable) before physical
    /// deletion. Purge fires when today &gt; ExpiresAt + GraceDays. PROJECT.md: ~7.
    /// </summary>
    public int GraceDays { get; set; } = 7;

    public TimeSpan InitialDelay => TimeSpan.FromSeconds(Math.Max(0, InitialDelaySeconds));

    public TimeSpan PollInterval => TimeSpan.FromSeconds(Math.Max(5, PollSeconds));
}
