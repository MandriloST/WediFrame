namespace WediFrame.Modules.Media.Domain;

/// <summary>
/// A "download the whole gallery as one ZIP" job. Heavy work (fetch every
/// original from R2, stream it into an archive, upload the archive back) runs
/// in the <c>ExportWorker</c> background service; the host polls status. The
/// DB row IS the queue — same no-extra-infrastructure approach as thumbnails.
///
/// Lifecycle: Pending → Running → Ready (ObjectKey set, downloadable until
/// ExpiresAt) or Failed (Error set). A crash mid-run leaves the row Running;
/// the worker re-claims stale Running rows on a later poll.
/// </summary>
public sealed class MediaExport
{
    public Guid Id { get; set; }

    /// <summary>Owning event id (plain Guid — Events owns events).</summary>
    public Guid EventId { get; set; }

    /// <summary>Host who requested it (for audit / future rate limiting).</summary>
    public Guid RequestedByUserId { get; set; }

    public MediaExportStatus Status { get; set; } = MediaExportStatus.Pending;

    /// <summary>R2 key of the finished ZIP; null until Ready.</summary>
    public string? ObjectKey { get; set; }

    /// <summary>Number of media files included in the archive (set when Ready).</summary>
    public int ItemCount { get; set; }

    /// <summary>Size of the finished ZIP in bytes (set when Ready).</summary>
    public long SizeBytes { get; set; }

    /// <summary>Short machine-readable failure reason; null unless Failed.</summary>
    public string? Error { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>When the ZIP stops being downloadable (physical cleanup is a retention concern, M4).</summary>
    public DateTimeOffset? ExpiresAt { get; set; }
}

public enum MediaExportStatus
{
    Pending = 0,
    Running = 1,
    Ready = 2,
    Failed = 3,
}
