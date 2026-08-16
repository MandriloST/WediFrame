using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using WediFrame.Shared.Options;
using WediFrame.Shared.RateLimiting;

namespace WediFrame.Api.RateLimiting;

public static class RateLimitingExtensions
{
    /// <summary>
    /// Registers the auth/guest/upload rate-limit policies from the "RateLimiting"
    /// config section. All partition per client IP (see <see cref="RateLimitOptions"/>).
    /// When disabled, every policy is a no-op so <c>.RequireRateLimiting(...)</c>
    /// still resolves but never throttles. Rejections return 429 + Retry-After.
    /// Requires <c>UseForwardedHeaders</c> + <c>UseRateLimiter</c> in the pipeline.
    /// </summary>
    public static IServiceCollection AddWediFrameRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RateLimitOptions>()
            .Bind(configuration.GetSection(RateLimitOptions.SectionName));

        var options = configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>()
            ?? new RateLimitOptions();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
                }

                return ValueTask.CompletedTask;
            };

            AddIpFixedWindow(limiter, RateLimitPolicies.Auth, options.Enabled, options.Auth);
            AddIpFixedWindow(limiter, RateLimitPolicies.Guest, options.Enabled, options.Guest);
            AddIpFixedWindow(limiter, RateLimitPolicies.Upload, options.Enabled, options.Upload);
        });

        return services;
    }

    private static void AddIpFixedWindow(RateLimiterOptions limiter, string name, bool enabled, RateLimitRule rule)
    {
        limiter.AddPolicy(name, context =>
        {
            if (!enabled)
            {
                return RateLimitPartition.GetNoLimiter("disabled");
            }

            var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rule.PermitLimit,
                Window = TimeSpan.FromSeconds(rule.WindowSeconds),
                QueueLimit = 0, // no queueing — reject immediately with 429
            });
        });
    }
}
