namespace WediFrame.Shared.Storage;

/// <summary>
/// Abstraction over the media object store (Cloudflare R2 in production).
/// Guest files NEVER pass through the API on the request path: the browser
/// talks to storage directly via presigned URLs. The Download/Upload methods
/// exist only for server-side background work (the thumbnail worker), which
/// reads an original and writes a derived thumbnail — never on a user request.
/// Implementation lives in Infrastructure (R2ObjectStorage).
/// </summary>
public interface IObjectStorage
{
    /// <summary>
    /// Create a short-lived presigned PUT URL for a direct browser upload.
    /// The URL is bound to the exact key and content type — the client must
    /// send a matching Content-Type header or the upload is rejected by R2.
    /// </summary>
    Task<Uri> PresignPutAsync(string key, string contentType, TimeSpan expiry, CancellationToken ct = default);

    /// <summary>Create a short-lived presigned GET URL for displaying/downloading an object.</summary>
    Task<Uri> PresignGetAsync(string key, TimeSpan expiry, CancellationToken ct = default);

    /// <summary>
    /// Create a short-lived presigned GET URL that forces a browser download
    /// (Content-Disposition: attachment) with the given file name, instead of
    /// rendering inline. Used by the "save this photo/video" action.
    /// </summary>
    Task<Uri> PresignDownloadAsync(string key, string downloadFileName, TimeSpan expiry, CancellationToken ct = default);

    /// <summary>Fetch object metadata (HEAD), or null if the object does not exist.</summary>
    Task<StoredObjectInfo?> HeadAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Download an object's bytes (server-side, background jobs only), or null
    /// if the object does not exist.
    /// </summary>
    Task<ObjectDownload?> DownloadAsync(string key, CancellationToken ct = default);

    /// <summary>Upload bytes to a key (server-side, background jobs only). Overwrites.</summary>
    Task UploadAsync(string key, byte[] content, string contentType, CancellationToken ct = default);

    /// <summary>Delete an object. Deleting a non-existent key is a no-op.</summary>
    Task DeleteAsync(string key, CancellationToken ct = default);

    // --- Multipart (large video uploads, browser → R2 directly) --------------

    /// <summary>Start a multipart upload; returns the R2 upload id.</summary>
    Task<string> CreateMultipartUploadAsync(string key, string contentType, CancellationToken ct = default);

    /// <summary>
    /// Presigned PUT URL for one part (1-based part number) of a multipart upload.
    /// The browser PUTs the chunk to this URL and reads the ETag from the response.
    /// </summary>
    Task<Uri> PresignUploadPartAsync(string key, string uploadId, int partNumber, TimeSpan expiry, CancellationToken ct = default);

    /// <summary>Finish a multipart upload by assembling the uploaded parts (ordered by part number).</summary>
    Task CompleteMultipartUploadAsync(string key, string uploadId, IReadOnlyList<MultipartPart> parts, CancellationToken ct = default);

    /// <summary>Abort a multipart upload and discard any uploaded parts. Best-effort.</summary>
    Task AbortMultipartUploadAsync(string key, string uploadId, CancellationToken ct = default);
}

/// <summary>One completed part of a multipart upload: its number and the ETag R2 returned.</summary>
public sealed record MultipartPart(int PartNumber, string ETag);

/// <summary>Metadata returned by <see cref="IObjectStorage.HeadAsync"/>.</summary>
public sealed record StoredObjectInfo(long SizeBytes, string? ContentType);

/// <summary>Bytes + content type returned by <see cref="IObjectStorage.DownloadAsync"/>.</summary>
public sealed record ObjectDownload(byte[] Content, string? ContentType);
