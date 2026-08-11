namespace WediFrame.Shared.Imaging;

/// <summary>
/// Turns an uploaded original into a small, web-friendly thumbnail. Abstracts
/// the imaging technology (libvips via NetVips in Infrastructure) so feature
/// modules depend only on this contract, not on native libraries.
/// </summary>
public interface IThumbnailGenerator
{
    /// <summary>
    /// Produce a JPEG thumbnail that fits within a <paramref name="maxEdge"/>×
    /// <paramref name="maxEdge"/> box (aspect preserved, never upscaled), with
    /// EXIF orientation applied and metadata stripped.
    /// Throws <see cref="ThumbnailFormatException"/> when the input cannot be
    /// decoded (corrupt or unsupported); other exceptions are infrastructure
    /// failures the caller should treat as transient.
    /// </summary>
    byte[] CreateJpegThumbnail(byte[] input, int maxEdge, int quality);
}

/// <summary>
/// The input could not be decoded/processed into a thumbnail (corrupt file or
/// a format the imaging library cannot read). Distinct from infrastructure
/// errors so the worker can mark that one item Failed and move on.
/// </summary>
public sealed class ThumbnailFormatException(string message, Exception? innerException = null)
    : Exception(message, innerException);
