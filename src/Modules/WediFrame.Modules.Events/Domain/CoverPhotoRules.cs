namespace WediFrame.Modules.Events.Domain;

/// <summary>
/// Validation rules for the event cover photo (host-uploaded, shown as the
/// guest page hero). Assumption (not in PROJECT.md): max 20 MB, JPEG/PNG/WebP —
/// enough for any phone photo; flag in Decision Log, owner can adjust.
/// </summary>
public static class CoverPhotoRules
{
    public const long MaxBytes = 20 * 1024 * 1024; // 20 MB

    /// <summary>Allowed content types mapped to the file extension used in the R2 key.</summary>
    public static readonly IReadOnlyDictionary<string, string> AllowedContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
        };

    /// <summary>R2 key prefix for an event's cover photos.</summary>
    public static string KeyPrefix(Guid eventId) => $"events/{eventId:D}/cover/";

    public static string NewKey(Guid eventId, string contentType)
        => KeyPrefix(eventId) + Guid.NewGuid().ToString("N") + AllowedContentTypes[contentType];
}
