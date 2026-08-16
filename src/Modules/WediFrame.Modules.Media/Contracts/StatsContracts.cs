namespace WediFrame.Modules.Media.Contracts;

/// <summary>
/// Host stats for an event: confirmed (stored) usage vs the package limits
/// (GET /events/{id}/stats). Max* are null when the event has no package
/// (legacy) — the frontend then shows usage without a cap.
/// </summary>
public sealed record EventStatsResponse(
    int PhotoCount,
    int? MaxPhotoCount,
    long VideoBytes,
    long? MaxVideoTotalBytes,
    long TotalBytes,
    long? MaxTotalBytes,
    string? PackageSlug,
    string? PackageName,
    DateOnly UploadStartDate,
    DateOnly? UploadEndsAt,
    DateOnly? ExpiresAt);
