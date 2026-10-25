using System.Threading.RateLimiting;

namespace TodoApi;

/// <summary>The kinds of email the API sends, each limited on its own.</summary>
public enum EmailKind
{
    /// <summary>A link that confirms an email, sent on registration, on request and on an email change.</summary>
    Confirmation,

    /// <summary>A link that resets a forgotten password.</summary>
    PasswordReset,

    /// <summary>A code that finishes a login.</summary>
    TwoFactorCode,
}

/// <summary>
/// Per-client limits on each kind of email, all read from the <c>RateLimits:Email</c> configuration section, so
/// sending one kind never holds back another. Unlike <see cref="RateLimits.Api"/>, which the rate limiting middleware
/// counts before the endpoint runs, a request counts only once it succeeds: a refused registration counts towards the
/// API limit alone. A request that succeeds counts even when Identity sends nothing, as for an unknown email, so the
/// limit doesn't tell which emails have an account. A login counts only as it sends a two-factor code, through
/// <see cref="Take"/>, so a wrong password or the login that submits the code is never held back.
/// </summary>
public sealed class EmailLimits(IConfiguration configuration) : IDisposable
{
    private const string Section = "Email";

    private readonly Dictionary<EmailKind, PartitionedRateLimiter<HttpContext>> limiters =
        Enum.GetValues<EmailKind>().ToDictionary(kind => kind, _ => RateLimits.CreatePerClientLimiter(configuration, Section));

    /// <summary>
    /// Endpoint filter that answers 429 while the client has no email of the kind left in the window, and otherwise
    /// counts the request once <paramref name="sendsEmail"/> says the endpoint's result is one that sends an email.
    /// </summary>
    public static async ValueTask<object?> LimitAsync(
        EndpointFilterInvocationContext invocation,
        EndpointFilterDelegate next,
        EmailKind kind,
        Func<EndpointFilterInvocationContext, object?, bool> sendsEmail)
    {
        var context = invocation.HttpContext;
        var limiter = context.RequestServices.GetRequiredService<EmailLimits>().limiters[kind];
        using (var check = limiter.AttemptAcquire(context, permitCount: 0))
        {
            if (!check.IsAcquired)
            {
                return RateLimits.Rejection(context, check);
            }
        }

        var result = await next(invocation);
        if (sendsEmail(invocation, result))
        {
            limiter.AttemptAcquire(context).Dispose();
        }
        return result;
    }

    /// <summary>
    /// Counts an email of the kind that is about to be sent; the lease says whether the client had one left. For an
    /// endpoint that only sometimes sends one, as login does a two-factor code, so its other requests aren't held back.
    /// </summary>
    public RateLimitLease Take(HttpContext context, EmailKind kind) => limiters[kind].AttemptAcquire(context);

    /// <summary>Whether an endpoint's result, or the one it wraps, has a success status code.</summary>
    public static bool IsSuccess(object? result) =>
        ((result as INestedHttpResult)?.Result ?? result) is IStatusCodeHttpResult { StatusCode: >= 200 and < 300 };

    public void Dispose()
    {
        foreach (var limiter in limiters.Values)
        {
            limiter.Dispose();
        }
    }
}
