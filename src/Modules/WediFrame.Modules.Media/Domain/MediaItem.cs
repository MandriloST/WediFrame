namespace WediFrame.Modules.Media.Domain;

/// <summary>
/// A single guest-uploaded file (photo now; video arrives with multipart, next block).
/// Files live on R2 under <see cref="ObjectKey"/> — the database holds metadata only.
/// </summary>
public sealed class MediaItem
{
    public Guid Id { get; set; }

    /// <summary>Owning event id. Plain Guid — no cross-module FK (Events owns events).</summary>
    public Guid EventId { get; set; }

    public MediaType Type { get; set; }

    /// <summary>R2 object key: events/{eventId}/media/{mediaId}{ext}.</summary>
    public required string ObjectKey { get; set; }

    public required string ContentType { get; set; }

    /// <summary>
    /// Declared by the client at presign; overwritten with the ACTUAL object size
    /// (verified via HEAD) at confirm. Package quota math (M3) must only ever
    /// count Confirmed items, so this is trustworthy once status is Confirmed.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>Original file name as picked by the guest (for ZIP export, M2). Optional.</summary>
    public string? FileName { get; set; }

    /// <summary>Self-reported guest name (optional, no accounts by design).</summary>
    public string? GuestName { get; set; }

    public MediaUploadStatus UploadStatus { get; set; } = MediaUploadStatus.Pending;

    public MediaVisibility Visibility { get; set; } = MediaVisibility.Visible;

    /// <summary>Set by thumbnail background job (M2).</summary>
    public string? ThumbnailKey { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ConfirmedAt { get; set; }

    /// <summary>Soft delete (host action or retention job); physical cleanup follows the grace period.</summary>
    public DateTimeOffset? SoftDeletedAt { get; set; }
}

public enum MediaType
{
    Photo = 0,
    Video = 1,
}

public enum MediaUploadStatus
{
    /// <summary>Presigned URL handed out; object not verified yet. Stale pendings are cleaned up by a job.</summary>
    Pending = 0,
    Confirmed = 1,
    Failed = 2,
}

public enum MediaVisibility
{
    Visible = 0,
    Hidden = 1,
}
