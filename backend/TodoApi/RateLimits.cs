using System.Globalization;
using System.Threading.RateLimiting;

namespace TodoApi;

/// <summary>
/// Per-client request limits, keyed by remote IP address, each a fixed window read from the
/// <c>RateLimits:{name}</c> configuration section: <see cref="Api"/> for every API request, and the
/// <see cref="EmailLimits"/> for requests that send an email.
/// A rejected request gets a 429 with a <c>Retry-After</c> header in seconds.
/// </summary>
public static class RateLimits
{
    private const string Section = "RateLimits";

    /// <summary>Every API request.</summary>
    public const string Api = "api";

    private const string Email = "Email";

    public static IServiceCollection AddRateLimits(this IServiceCollection services, IConfiguration configuration) =>
        services.AddSingleton(_ => new EmailLimits(ReadLimit(configuration, Email))).AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, _) =>
            {
                WriteRetryAfter(context.HttpContext, context.Lease);
                return ValueTask.CompletedTask;
            };
            options.AddPolicy(Api, PerClient(ReadLimit(configuration, Api)));
        });

    /// <summary>A limiter of its own, outside the rate limiting middleware, with the given limit.</summary>
    public static PartitionedRateLimiter<HttpContext> CreatePerClientLimiter(FixedWindowRateLimiterOptions limit) =>
        PartitionedRateLimiter.Create(PerClient(limit));

    /// <summary>The 429 a limiter of <see cref="CreatePerClientLimiter"/> answers with when it refuses a lease.</summary>
    public static IResult Rejection(HttpContext context, RateLimitLease lease)
    {
        WriteRetryAfter(context, lease);
        return TypedResults.StatusCode(StatusCodes.Status429TooManyRequests);
    }

    private static FixedWindowRateLimiterOptions ReadLimit(IConfiguration configuration, string name) =>
        configuration.GetRequiredSection($"{Section}:{name}").Get<FixedWindowRateLimiterOptions>()!;

    private static Func<HttpContext, RateLimitPartition<string>> PerClient(FixedWindowRateLimiterOptions limit) =>
        context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            _ => limit);

    private static void WriteRetryAfter(HttpContext context, RateLimitLease lease)
    {
        if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }
    }
}
