namespace WediFrame.Modules.Events.Contracts;

// Error strings are machine-readable codes ("events.title_required", ...);
// the frontend maps them to localized UI strings.

public sealed record CreateEventRequest(string Title, DateOnly UploadStartDate, string? Type);

public sealed record EventResponse(
    Guid Id,
    string Title,
    string Type,
    DateOnly UploadStartDate,
    string Status,
    string GuestToken,
    string GuestUrl,
    string? CoverPhotoKey,
    string? CoverPhotoUrl,
    DateTimeOffset CreatedAt);

/// <summary>Host asks for a presigned PUT URL to upload the cover photo directly to R2.</summary>
public sealed record CoverUploadRequest(string ContentType, long SizeBytes);

public sealed record CoverUploadResponse(
    string Key,
    string UploadUrl,
    string ContentType,
    DateTimeOffset ExpiresAt,
    long MaxBytes);

/// <summary>Host confirms the upload finished; backend verifies the object on R2.</summary>
public sealed record CoverConfirmRequest(string Key);

/// <summary>
/// Public event info for the guest page (/e/{token}). No ids, no owner data —
/// only what the guest page renders. UploadOpen is provisional until packages
/// exist (M3): true when the event is Active and today >= T0.
/// </summary>
public sealed record GuestEventInfoResponse(
    string Title,
    string Type,
    DateOnly UploadStartDate,
    string Status,
    string? CoverPhotoUrl,
    bool UploadOpen);
