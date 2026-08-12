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
            ServiceURL = r2.Endpoint, // EU-jurisdiction buckets use {accountId}.eu.r2.cloudflarestorage.com
            ForcePathStyle = true,
            // R2 requires the SigV4 credential scope region to be "auto" —
            // without this the SDK signs with us-east-1 and R2 answers AccessDenied.
            AuthenticationRegion = "auto",
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

    public async Task<Uri> PresignDownloadAsync(
        string key, string downloadFileName, TimeSpan expiry, CancellationToken ct = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiry),
        };

        // Force "save as" instead of inline render. The filename is signed into
        // the URL via the response-content-disposition override, so it survives
        // the direct browser → R2 request (the API never proxies the bytes).
        request.ResponseHeaderOverrides.ContentDisposition =
            $"attachment; filename=\"{SanitizeFileName(downloadFileName)}\"";

        var url = await _client.Value.GetPreSignedURLAsync(request);
        return new Uri(url);
    }

    /// <summary>Keep the filename header-safe (no quotes/control chars/path separators).</summary>
    private static string SanitizeFileName(string name)
    {
        var cleaned = new string(name
            .Where(c => !char.IsControl(c) && c != '"' && c != '\\' && c != '/')
            .ToArray())
            .Trim();
        return string.IsNullOrEmpty(cleaned) ? "wediframe" : cleaned;
    }

    public async Task<StoredObjectInfo?> HeadAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.Value.GetObjectMetadataAsync(
                new GetObjectMetadataRequest { BucketName = _bucket, Key = key }, ct);

            return new StoredObjectInfo(response.ContentLength, response.Headers.ContentType);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<ObjectDownload?> DownloadAsync(string key, CancellationToken ct = default)
    {
        try
        {
            using var response = await _client.Value.GetObjectAsync(
                new GetObjectRequest { BucketName = _bucket, Key = key }, ct);

            using var buffer = new MemoryStream();
            await response.ResponseStream.CopyToAsync(buffer, ct);
            return new ObjectDownload(buffer.ToArray(), response.Headers.ContentType);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Stream?> OpenReadAsync(string key, CancellationToken ct = default)
    {
        try
        {
            // Caller owns the returned stream; the underlying S3 response is
            // disposed when that stream is disposed.
            var response = await _client.Value.GetObjectAsync(
                new GetObjectRequest { BucketName = _bucket, Key = key }, ct);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public Task UploadAsync(string key, byte[] content, string contentType, CancellationToken ct = default)
    {
        // Seekable MemoryStream with a known length → a plain PUT with
        // Content-Length (no streaming/trailer checksum that R2 rejects).
        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            ContentType = contentType,
            InputStream = new MemoryStream(content),
            DisablePayloadSigning = true,
        };

        return _client.Value.PutObjectAsync(request, ct);
    }

    public Task UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        // The stream must be seekable so the SDK can set Content-Length and R2
        // gets a plain PUT (the ZIP worker passes a temp FileStream).
        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            ContentType = contentType,
            InputStream = content,
            AutoCloseStream = false,
            DisablePayloadSigning = true,
        };

        return _client.Value.PutObjectAsync(request, ct);
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
        => _client.Value.DeleteObjectAsync(new DeleteObjectRequest { BucketName = _bucket, Key = key }, ct);

    // --- Multipart ------------------------------------------------------------

    public async Task<string> CreateMultipartUploadAsync(string key, string contentType, CancellationToken ct = default)
    {
        var response = await _client.Value.InitiateMultipartUploadAsync(
            new InitiateMultipartUploadRequest
            {
                BucketName = _bucket,
                Key = key,
                ContentType = contentType,
            }, ct);

        return response.UploadId;
    }

    public async Task<Uri> PresignUploadPartAsync(string key, string uploadId, int partNumber, TimeSpan expiry, CancellationToken ct = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(expiry),
            UploadId = uploadId,
            PartNumber = partNumber,
        };

        var url = await _client.Value.GetPreSignedURLAsync(request);
        return new Uri(url);
    }

    public Task CompleteMultipartUploadAsync(string key, string uploadId, IReadOnlyList<MultipartPart> parts, CancellationToken ct = default)
        => _client.Value.CompleteMultipartUploadAsync(new CompleteMultipartUploadRequest
        {
            BucketName = _bucket,
            Key = key,
            UploadId = uploadId,
            PartETags = parts
                .OrderBy(p => p.PartNumber)
                .Select(p => new PartETag(p.PartNumber, p.ETag))
                .ToList(),
        }, ct);

    public Task AbortMultipartUploadAsync(string key, string uploadId, CancellationToken ct = default)
        => _client.Value.AbortMultipartUploadAsync(new AbortMultipartUploadRequest
        {
            BucketName = _bucket,
            Key = key,
            UploadId = uploadId,
        }, ct);

    public void Dispose()
    {
        if (_client.IsValueCreated)
        {
            _client.Value.Dispose();
        }
    }
}
