namespace WediFrame.Modules.Media.Domain;

/// <summary>
/// Builds a friendly, safe download filename for a media item. Guests upload
/// with opaque object keys, so "save" would otherwise hand the visitor a
/// meaningless name — this produces e.g. <c>wediframe-3f9a2b1c.jpg</c>.
/// </summary>
public static class MediaDownloadName
{
    public static string For(MediaType type, string? contentType, Guid mediaId)
    {
        var shortId = mediaId.ToString("N")[..8];
        return $"wediframe-{shortId}.{Extension(type, contentType)}";
    }

    private static string Extension(MediaType type, string? contentType) =>
        (contentType?.ToLowerInvariant()) switch
        {
            "image/jpeg" => "jpg",
            "image/png" => "png",
            "image/webp" => "webp",
            "image/heic" => "heic",
            "image/heif" => "heif",
            "video/mp4" => "mp4",
            "video/quicktime" => "mov",
            "video/webm" => "webm",
            _ => type == MediaType.Video ? "mp4" : "jpg",
        };
}
