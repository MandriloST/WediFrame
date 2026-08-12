using System.Text;

namespace WediFrame.Modules.Media.Domain;

/// <summary>Naming helpers for gallery ZIP exports (R2 key, entry names, download name).</summary>
public static class ExportRules
{
    public const string ZipContentType = "application/zip";

    /// <summary>R2 key of a finished export ZIP.</summary>
    public static string ObjectKey(Guid eventId, Guid exportId)
        => $"events/{eventId:D}/exports/{exportId:N}.zip";

    /// <summary>Filename the host's browser saves the ZIP as.</summary>
    public static string DownloadFileName(Guid eventId)
        => $"wediframe-{eventId.ToString("N")[..8]}.zip";

    /// <summary>
    /// A unique, filesystem-safe entry name for one item inside the archive.
    /// Prefers the guest's original filename; falls back to a generated name.
    /// A numeric prefix guarantees uniqueness even when many guests upload
    /// "IMG_1234.jpg".
    /// </summary>
    public static string EntryName(int sequence, MediaType type, string? contentType, string? originalName, Guid mediaId)
    {
        var baseName = !string.IsNullOrWhiteSpace(originalName)
            ? Sanitize(originalName!)
            : MediaDownloadName.For(type, contentType, mediaId);

        return $"{sequence:D4}-{baseName}";
    }

    private static string Sanitize(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            sb.Append(char.IsControl(c) || c is '/' or '\\' or ':' or '*' or '?' or '"' or '<' or '>' or '|'
                ? '_'
                : c);
        }

        var cleaned = sb.ToString().Trim().Trim('.');
        return string.IsNullOrEmpty(cleaned) ? "file" : cleaned;
    }
}
