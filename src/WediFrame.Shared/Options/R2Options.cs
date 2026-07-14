namespace WediFrame.Shared.Options;

/// <summary>
/// Bound from the "R2" configuration section. Secrets come from user-secrets
/// locally and env vars on Railway (R2__AccessKeyId, R2__SecretAccessKey).
/// The bucket must be created with EU jurisdiction (GDPR requirement).
/// </summary>
public sealed class R2Options
{
    public const string SectionName = "R2";

    /// <summary>Cloudflare account id — part of the S3 endpoint URL.</summary>
    public string AccountId { get; set; } = "";

    /// <summary>R2 API token access key id (S3-compatible credentials).</summary>
    public string AccessKeyId { get; set; } = "";

    /// <summary>R2 API token secret. NEVER in appsettings committed to the repo.</summary>
    public string SecretAccessKey { get; set; } = "";

    /// <summary>Bucket name, e.g. "wediframe" (dev: "wediframe-dev").</summary>
    public string Bucket { get; set; } = "";

    /// <summary>
    /// R2 jurisdiction of the bucket. Jurisdictional buckets live on a separate
    /// endpoint: {accountId}.{jurisdiction}.r2.cloudflarestorage.com.
    /// Our buckets are always "eu" (GDPR); empty string = default namespace.
    /// </summary>
    public string Jurisdiction { get; set; } = "eu";

    /// <summary>S3-compatible endpoint for this account + jurisdiction.</summary>
    public string Endpoint =>
        string.IsNullOrWhiteSpace(Jurisdiction)
            ? $"https://{AccountId}.r2.cloudflarestorage.com"
            : $"https://{AccountId}.{Jurisdiction.Trim().ToLowerInvariant()}.r2.cloudflarestorage.com";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(AccountId)
        && !string.IsNullOrWhiteSpace(AccessKeyId)
        && !string.IsNullOrWhiteSpace(SecretAccessKey)
        && !string.IsNullOrWhiteSpace(Bucket);
}
