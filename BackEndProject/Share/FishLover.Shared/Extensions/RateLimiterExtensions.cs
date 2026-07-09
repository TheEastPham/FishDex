using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;

namespace FishLover.Shared.Extensions;

public static class RateLimiterExtensions
{
    /// <summary>
    /// IP-based rate limiting for unauthenticated requests.
    /// Authenticated users bypass all limits.
    /// Limits: 60 req/min (sliding) + 1000 req/day (fixed) per IP.
    /// Place UseRateLimiter() after UseAuthorization() in the pipeline.
    /// </summary>
    public static IServiceCollection AddFishLoverRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                {
                    if (ctx.User.Identity?.IsAuthenticated == true)
                        return RateLimitPartition.GetNoLimiter("auth");
                    var ip = GetClientIp(ctx);
                    return RateLimitPartition.GetSlidingWindowLimiter($"min:{ip}", _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit       = 60,
                        Window            = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 4,
                        QueueLimit        = 0,
                    });
                }),
                PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                {
                    if (ctx.User.Identity?.IsAuthenticated == true)
                        return RateLimitPartition.GetNoLimiter("auth");
                    var ip = GetClientIp(ctx);
                    return RateLimitPartition.GetFixedWindowLimiter($"day:{ip}", _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 1000,
                        Window      = TimeSpan.FromHours(24),
                        QueueLimit  = 0,
                    });
                })
            );
            options.OnRejected = async (ctx, ct) =>
            {
                ctx.HttpContext.Response.Headers.RetryAfter = "60";
                await ctx.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", ct);
            };
        });

        return services;
    }

    private static string GetClientIp(HttpContext ctx)
    {
        var ip = (ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? "")
                     .Split(',', StringSplitOptions.TrimEntries)[0];
        return string.IsNullOrEmpty(ip)
            ? ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown"
            : ip;
    }
}
