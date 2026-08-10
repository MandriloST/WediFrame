namespace WediFrame.Modules.Media.Contracts;

// Error strings are machine-readable codes ("media.type_unsupported", ...);
// the frontend maps them to localized UI strings.

/// <summary>
/// Guest asks for presigned PUT URLs for a batch of photos. Validation is
/// all-or-nothing: if ANY item is invalid the whole request returns 400 with
/// per-item error keys ("items[3].sizeBytes") — the frontend pre-filters and
/// retries valid items, so guests never lose a whole batch silently.
/// </summary>
public sealed record GuestUploadRequest(
    List<GuestUploadItemRequest> Items,
    string? GuestName);

public sealed record GuestUploadItemRequest(
    string ContentType,
    long SizeBytes,
    string? FileName);

public sealed record GuestUploadResponse(List<GuestUploadItemResponse> Items);

public sealed record GuestUploadItemResponse(
    Guid MediaId,
    string ObjectKey,
    string UploadUrl,
    string ContentType,
    DateTimeOffset ExpiresAt);

public sealed record GuestConfirmResponse(
    Guid MediaId,
    string UploadStatus,
    long SizeBytes);

// --- Gallery (M2) ------------------------------------------------------------

/// <summary>
/// One page of the guest gallery. Offset pagination with a deterministic order
/// (CreatedAt desc, ObjectKey desc): a batch of photos shares one CreatedAt, so
/// ObjectKey (unique) is the tie-break. The frontend also dedupes by MediaId,
/// which absorbs any boundary shift caused by concurrent uploads mid-scroll.
/// </summary>
public sealed record GuestGalleryResponse(
    List<GuestGalleryItem> Items,
    int? NextOffset);

/// <summary>
/// A confirmed, visible item for display. <see cref="Url"/> is a presigned GET
/// of the original; <see cref="ThumbnailUrl"/> is set once the thumbnail job
/// (next M2 block) runs — until then it is null and the grid lazy-loads the
/// original. HEIC/HEIF have no browser rendering and no thumbnail yet, so the
/// frontend shows a placeholder tile for them.
/// </summary>
public sealed record GuestGalleryItem(
    Guid MediaId,
    string Type,
    string Url,
    string? ThumbnailUrl,
    string ContentType,
    string? GuestName,
    DateTimeOffset CreatedAt);
