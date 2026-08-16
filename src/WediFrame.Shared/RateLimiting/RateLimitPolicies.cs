namespace WediFrame.Shared.RateLimiting;

/// <summary>
/// Names of the rate-limit policies, referenced by endpoints via
/// <c>.RequireRateLimiting(...)</c>. Constants (not magic strings) so a rename is
/// a compile error, and modules can opt in without depending on the Api project.
/// The policies themselves are configured in the Api host.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>Auth writes (register/login/refresh) — brute-force protection, strict.</summary>
    public const string Auth = "auth";

    /// <summary>Public guest reads (event info, gallery, download) — moderate.</summary>
    public const string Guest = "guest";

    /// <summary>Public guest writes (upload init/confirm, video multipart) — tighter than reads.</summary>
    public const string Upload = "upload";
}
