namespace WediFrame.Modules.Media.Domain;

/// <summary>
/// Validation rules for guest photo uploads (M1: single PUT; video multipart is
/// the next block). Per-file photo size is an ASSUMPTION (not in PROJECT.md):
/// 50 MB covers any phone photo incl. 48 MP HEIC with headroom — flagged in
/// Decision Log, owner can adjust. Package quotas (photo count, total bytes)
/// are M3 — limits here are per-file/per-request only.
/// </summary>
public static class PhotoRules
{
    public const long MaxBytes = 50 * 1024 * 1024; // 50 MB per photo

    /// <summary>Max files per presign request — keeps request/response sizes sane; the frontend batches.</summary>
    public const int MaxItemsPerRequest = 30;

    public const int MaxFileNameLength = 255;
    public const int MaxGuestNameLength = 100;

    /// <summary>Generated thumbnails are always JPEG (max webview compatibility).</summary>
    public const string ThumbnailContentType = "image/jpeg";

    /// <summary>
    /// Allowed content types mapped to the file extension used in the R2 key.
    /// HEIC/HEIF included because iPhones produce them — browsers mostly can't
    /// display them, which the thumbnail job handles (converts to a JPEG thumb).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> AllowedContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
            ["image/heic"] = ".heic",
            ["image/heif"] = ".heif",
            ["image/gif"] = ".gif",
        };

    /// <summary>R2 key prefix for an event's guest media.</summary>
    public static string KeyPrefix(Guid eventId) => $"events/{eventId:D}/media/";

    public static string NewKey(Guid eventId, Guid mediaId, string contentType)
        => KeyPrefix(eventId) + mediaId.ToString("N") + AllowedContentTypes[contentType];

    /// <summary>R2 key for an item's generated thumbnail: events/{eventId}/thumbs/{mediaId}.jpg.</summary>
    public static string ThumbnailKey(Guid eventId, Guid mediaId)
        => $"events/{eventId:D}/thumbs/{mediaId:N}.jpg";
}
