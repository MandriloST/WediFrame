namespace WediFrame.Modules.Media.Domain;

/// <summary>
/// Validation + chunking rules for guest video uploads (multipart to R2).
/// Videos have no duration limit (Decision Log 2026-07-06); the guardrail is a
/// 2 GB per-file cap. No transcoding in the MVP — the original is stored and
/// streamed back via R2 range requests. Package quotas (total GB, video GB)
/// arrive with Billing (M3).
/// </summary>
public static class VideoRules
{
    /// <summary>Max size of a single video file (PROJECT.md, confirmed).</summary>
    public const long MaxBytes = 2L * 1024 * 1024 * 1024; // 2 GB

    /// <summary>
    /// Multipart part size. 16 MB keeps retries cheap on poor venue wifi while
    /// staying well under R2's 10 000-part ceiling (2 GB / 16 MB ≈ 128 parts).
    /// S3/R2 require every part except the last to be ≥ 5 MB.
    /// </summary>
    public const long PartSizeBytes = 16 * 1024 * 1024; // 16 MB

    /// <summary>Hard S3/R2 limit on the number of parts per multipart upload.</summary>
    public const int MaxParts = 10_000;

    /// <summary>
    /// Allowed video content types mapped to the extension used in the R2 key.
    /// iPhones record .mov (quicktime); Android records .mp4; webm covers the rest.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> AllowedContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["video/mp4"] = ".mp4",
            ["video/quicktime"] = ".mov",
            ["video/webm"] = ".webm",
        };

    public static string NewKey(Guid eventId, Guid mediaId, string contentType)
        => PhotoRules.KeyPrefix(eventId) + mediaId.ToString("N") + AllowedContentTypes[contentType];

    /// <summary>Number of parts a file of the given size splits into.</summary>
    public static int PartCount(long sizeBytes)
        => (int)((sizeBytes + PartSizeBytes - 1) / PartSizeBytes);
}
