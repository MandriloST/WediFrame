namespace WediFrame.Modules.Media.Services;

/// <summary>
/// Config for the thumbnail worker (section "Media:Thumbnails"). All values have
/// sane defaults, so the worker runs with no configuration. Override per env,
/// e.g. Media__Thumbnails__Enabled=false to turn it off on a given deploy.
/// </summary>
public sealed class ThumbnailOptions
{
    public const string SectionName = "Media:Thumbnails";

    /// <summary>Master switch. Disable to stop background processing on a deploy.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Seconds between polls when the queue is empty.</summary>
    public int PollSeconds { get; set; } = 15;

    /// <summary>Delay before the first poll, so startup isn't contended.</summary>
    public int InitialDelaySeconds { get; set; } = 10;

    /// <summary>Items per batch. Processed sequentially to bound memory (big photos).</summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>Longest edge of the generated thumbnail, in pixels.</summary>
    public int MaxEdge { get; set; } = 640;

    /// <summary>JPEG quality (1–100).</summary>
    public int JpegQuality { get; set; } = 80;

    public TimeSpan PollInterval => TimeSpan.FromSeconds(Math.Max(2, PollSeconds));
    public TimeSpan InitialDelay => TimeSpan.FromSeconds(Math.Max(0, InitialDelaySeconds));
}
