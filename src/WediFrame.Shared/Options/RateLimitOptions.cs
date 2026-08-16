namespace WediFrame.Shared.Options;

/// <summary>
/// Bound from the "RateLimiting" configuration section. Every public surface is
/// limited per client IP (per device) — not per event token, since all guests of
/// one wedding share the token and would otherwise throttle each other. Behind
/// Railway's proxy the real IP comes from X-Forwarded-For (ForwardedHeaders).
///
/// Set <see cref="Enabled"/> = false to turn limiting off (local dev). Windows
/// are fixed; limits are per <c>WindowSeconds</c>.
/// </summary>
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Master switch. When false, all policies become no-ops.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Auth writes (register/login/refresh). Strict — this is the brute-force surface.</summary>
    public RateLimitRule Auth { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60 };

    /// <summary>Guest reads (event info, gallery, download). Generous for a single device.</summary>
    public RateLimitRule Guest { get; set; } = new() { PermitLimit = 120, WindowSeconds = 60 };

    /// <summary>Guest writes (upload init/confirm, video multipart). Tighter — they trigger work.</summary>
    public RateLimitRule Upload { get; set; } = new() { PermitLimit = 60, WindowSeconds = 60 };
}

/// <summary>One fixed-window rule: at most <see cref="PermitLimit"/> requests per <see cref="WindowSeconds"/>.</summary>
public sealed class RateLimitRule
{
    public int PermitLimit { get; set; } = 60;

    public int WindowSeconds { get; set; } = 60;
}
