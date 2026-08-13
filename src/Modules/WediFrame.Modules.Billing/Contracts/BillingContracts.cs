namespace WediFrame.Modules.Billing.Contracts;

/// <summary>
/// Public package info for the pricing page (GET /packages). Byte limits are sent
/// raw (bytes) so the frontend formats them per locale; the frontend localizes the
/// display name by <see cref="Slug"/> (Name is only a canonical fallback).
/// </summary>
public sealed record PackageResponse(
    string Slug,
    string Name,
    int PriceCents,
    string Currency,
    int MaxPhotoCount,
    long MaxVideoTotalBytes,
    long MaxTotalBytes,
    long MaxFileBytes,
    int UploadPeriodDays,
    int RetentionDays,
    int SortOrder);
