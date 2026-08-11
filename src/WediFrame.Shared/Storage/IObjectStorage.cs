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
}

/// <summary>Metadata returned by <see cref="IObjectStorage.HeadAsync"/>.</summary>
public sealed record StoredObjectInfo(long SizeBytes, string? ContentType);

/// <summary>Bytes + content type returned by <see cref="IObjectStorage.DownloadAsync"/>.</summary>
public sealed record ObjectDownload(byte[] Content, string? ContentType);
