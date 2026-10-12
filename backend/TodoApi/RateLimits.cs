using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace TodoApi;

/// <summary>
/// Per-client request limits, keyed by remote IP address, each a fixed window read from the
/// <c>RateLimits:{policy}</c> configuration section.
/// A rejected request gets a 429 with a <c>Retry-After</c> header in seconds.
/// </summary>
public static class RateLimits
{
    private const string Section = "RateLimits";

    /// <summary>Every API request.</summary>
    public const string Api = "api";

    /// <summary>Requests that send an email.</summary>
    public const string Email = "email";

    public static IServiceCollection AddRateLimits(this IServiceCollection services, IConfiguration configuration) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRetryAfter;
            options.AddPolicy(Api, PerClient(ReadLimit(configuration, Api)));
            options.AddPolicy(Email, PerClient(ReadLimit(configuration, Email)));
        });

    private static FixedWindowRateLimiterOptions ReadLimit(IConfiguration configuration, string policy) =>
        configuration.GetRequiredSection($"{Section}:{policy}").Get<FixedWindowRateLimiterOptions>()!;

    private static Func<HttpContext, RateLimitPartition<string>> PerClient(FixedWindowRateLimiterOptions limit) =>
        context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            _ => limit);

    private static ValueTask WriteRetryAfter(OnRejectedContext context, CancellationToken _)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }
        return ValueTask.CompletedTask;
    }
}
