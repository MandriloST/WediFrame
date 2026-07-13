namespace WediFrame.Shared.Storage;

/// <summary>
/// Abstraction over the media object store (Cloudflare R2 in production).
/// Files NEVER pass through the API: the browser talks to storage directly
/// via presigned URLs; the API only creates URLs and verifies objects.
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

    /// <summary>Delete an object. Deleting a non-existent key is a no-op.</summary>
    Task DeleteAsync(string key, CancellationToken ct = default);
}

/// <summary>Metadata returned by <see cref="IObjectStorage.HeadAsync"/>.</summary>
public sealed record StoredObjectInfo(long SizeBytes, string? ContentType);
