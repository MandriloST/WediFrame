using WediFrame.Modules.Events.Services;

namespace WediFrame.Modules.Media.Domain;

/// <summary>Current usage for an event: photo count, total bytes, video bytes.</summary>
public readonly record struct MediaUsage(int PhotoCount, long TotalBytes, long VideoBytes);

/// <summary>
/// Package quota checks for guest uploads (M3). Limits come from the event's
/// package (via IGuestEventAccess); per-file caps stay in PhotoRules/VideoRules.
/// Returns a machine error code (mapped to i18n on the frontend) or null when the
/// addition fits. Enforced twice: at presign/init against pending+confirmed usage
/// (fast feedback), and authoritatively at confirm/complete against confirmed usage
/// with the ACTUAL verified size.
/// </summary>
public static class GuestQuota
{
    public static string? CheckAddition(
        GuestUploadLimits limits,
        MediaUsage current,
        int addPhotos,
        long addPhotoBytes,
        long addVideoBytes)
    {
        if (current.PhotoCount + addPhotos > limits.MaxPhotoCount)
        {
            return "media.quota_photo_count";
        }

        if (current.VideoBytes + addVideoBytes > limits.MaxVideoTotalBytes)
        {
            return "media.quota_video_bytes";
        }

        if (current.TotalBytes + addPhotoBytes + addVideoBytes > limits.MaxTotalBytes)
        {
            return "media.quota_total_bytes";
        }

        return null;
    }
}
