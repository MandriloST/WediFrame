namespace WediFrame.Modules.Media.Contracts;

/// <summary>
/// Response for a "download this item" request (host or guest). The URL is a
/// short-lived presigned GET with Content-Disposition: attachment, so the
/// browser saves the file directly from R2 — the API never proxies the bytes.
/// </summary>
public sealed record MediaDownloadResponse(string Url, string FileName);
