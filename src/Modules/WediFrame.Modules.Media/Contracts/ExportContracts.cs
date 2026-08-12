namespace WediFrame.Modules.Media.Contracts;

/// <summary>
/// State of a gallery ZIP export. The host starts one, then polls until
/// Status is "Ready" (DownloadUrl set — a short-lived presigned attachment)
/// or "Failed" (Error set). While Pending/Running the download fields are null.
/// </summary>
public sealed record ExportJobResponse(
    Guid JobId,
    string Status,
    int? ItemCount,
    long? SizeBytes,
    string? DownloadUrl,
    string? FileName,
    string? Error);
