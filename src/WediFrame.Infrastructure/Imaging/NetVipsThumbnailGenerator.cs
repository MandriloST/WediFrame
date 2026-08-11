using NetVips;
using WediFrame.Shared.Imaging;

namespace WediFrame.Infrastructure.Imaging;

/// <summary>
/// libvips (via NetVips) thumbnail generator. libvips shrinks on load, so a
/// 48 MP HEIC never fully decodes into memory — important on a small Railway
/// instance. Native libvips ships with the NetVips.Native package and includes
/// the HEIF/HEIC loader, so iPhone photos become viewable JPEG thumbnails.
/// </summary>
public sealed class NetVipsThumbnailGenerator : IThumbnailGenerator
{
    public byte[] CreateJpegThumbnail(byte[] input, int maxEdge, int quality)
    {
        try
        {
            // Fits within maxEdge×maxEdge, only downscales (Size.Down), and
            // auto-rotates using the EXIF orientation tag (default behaviour).
            using var image = Image.ThumbnailBuffer(input, maxEdge, height: maxEdge, size: Enums.Size.Down);

            // JPEG has no alpha: flatten transparency onto white so PNG/WebP
            // with transparency don't come out black.
            if (image.HasAlpha())
            {
                using var flattened = image.Flatten(background: [255.0, 255.0, 255.0]);
                return flattened.JpegsaveBuffer(q: quality, keep: Enums.ForeignKeep.None);
            }

            return image.JpegsaveBuffer(q: quality, keep: Enums.ForeignKeep.None);
        }
        catch (VipsException ex)
        {
            throw new ThumbnailFormatException("libvips could not process the image.", ex);
        }
    }
}
