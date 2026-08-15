namespace WediFrame.Modules.Events.Contracts;

// Error strings are machine-readable codes ("events.title_required", ...);
// the frontend maps them to localized UI strings.

public sealed record CreateEventRequest(string Title, DateOnly UploadStartDate, string? Type, string? PackageSlug);

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
    DateTimeOffset CreatedAt,
    string? PackageSlug,
    string? PackageName,
    DateOnly? UploadEndsAt,
    DateOnly? ExpiresAt);

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
/// only what the guest page renders. <see cref="UploadState"/> is one of
/// "NotStarted" | "Open" | "Closed" and drives the upload button; the gallery
/// shows in every state. <see cref="UploadOpen"/> is kept as the boolean shortcut
/// (== UploadState "Open") for convenience.
/// </summary>
public sealed record GuestEventInfoResponse(
    string Title,
    string Type,
    DateOnly UploadStartDate,
    string Status,
    string? CoverPhotoUrl,
    bool UploadOpen,
    string UploadState);

/// <summary>
/// Start a paid checkout for the event's (already chosen) package. R1 fields are
/// filled only when the host ticks "Trebam R1" in checkout.
/// </summary>
public sealed record CheckoutRequest(
    bool NeedsR1,
    string? CompanyName,
    string? CompanyOib,
    string? CompanyAddress);

/// <summary>Checkout response: the hosted payment URL to redirect the host to.</summary>
public sealed record CheckoutResponse(string Url);
