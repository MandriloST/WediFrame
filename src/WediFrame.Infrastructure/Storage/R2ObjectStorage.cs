using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using WediFrame.Shared.Options;
using WediFrame.Shared.Storage;

namespace WediFrame.Infrastructure.Storage;

/// <summary>
/// Cloudflare R2 implementation of <see cref="IObjectStorage"/> via the
/// S3-compatible API (AWS SDK v4).
///
/// R2 specifics:
///  - endpoint is https://{accountId}.r2.cloudflarestorage.com (path-style),
///  - AWS SDK default request/response checksums (CRC32) are not supported by R2,
///    so both are set to WHEN_REQUIRED (per Cloudflare docs for aws-sdk-net).
///
/// The S3 client is created lazily so the API boots (health, auth, event CRUD)
/// even before R2 is configured — the first actual storage call throws a clear
/// error instead. Registered as a singleton: AmazonS3Client is thread-safe, and
/// presigning is a local computation (no network round-trip).
/// </summary>
public sealed class R2ObjectStorage(IOptions<R2Options> options) : IObjectStorage, IDisposable
{
    private readonly Lazy<AmazonS3Client> _client = new(() => CreateClient(options.Value));
    private readonly string _bucket = options.Value.Bucket;

    private static AmazonS3Client CreateClient(R2Options r2)
    {
        if (!r2.IsConfigured)
        {
            throw new InvalidOperationException(
                "R2 storage is not configured. Set the \"R2\" section (AccountId, AccessKeyId, SecretAccessKey, Bucket) " +
                "via user-secrets locally or R2__* environment variables on Railway.");
        }

        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{r2.AccountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true,
            // R2 does not support the SDK's default CRC32 checksums:
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        };

        return new AmazonS3Client(r2.AccessKeyId, r2.SecretAccessKey, config);
    }

    public async Task<Uri> PresignPutAsync(string key, string contentType, TimeSpan expiry, CancellationToken ct = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(expiry),
            ContentType = contentType,
        };

        var url = await _client.Value.GetPreSignedURLAsync(request);
        return new Uri(url);
    }

    public async Task<Uri> PresignGetAsync(string key, TimeSpan expiry, CancellationToken ct = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiry),
        };

        var url = await _client.Value.GetPreSignedURLAsync(request);
        return new Uri(url);
    }

    public async Task<StoredObjectInfo?> HeadAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.Value.GetObjectMetadataAsync(
                new GetObjectMetadataRequest { BucketName = _bucket, Key = key }, ct);

            // SDK v4: ContentLength is nullable (long?).
            return new StoredObjectInfo(response.ContentLength ?? 0, response.Headers.ContentType);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
        => _client.Value.DeleteObjectAsync(new DeleteObjectRequest { BucketName = _bucket, Key = key }, ct);

    public void Dispose()
    {
        if (_client.IsValueCreated)
        {
            _client.Value.Dispose();
        }
    }
}
