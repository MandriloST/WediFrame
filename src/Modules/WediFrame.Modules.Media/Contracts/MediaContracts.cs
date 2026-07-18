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
