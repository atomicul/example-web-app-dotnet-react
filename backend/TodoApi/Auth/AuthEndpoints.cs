using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using TodoApi.Validation;

namespace TodoApi.Auth;

public static class AuthEndpoints
{
    private static readonly string[] EmailSendingRoutes =
        ["/register", "/resendConfirmationEmail", "/forgotPassword", "/manage/info"];

    public static IEndpointRouteBuilder MapAuth(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/auth").WithTags("Auth");

        var identity = auth.MapIdentityApi<IdentityUser>();
        identity.Add(LimitEmailSending);
        identity.Add(GuardAccountChanges);
        identity.AddEndpointFilter(ValidationProblems.CodeIdentityErrors);
        auth.MapPost("/logout", Logout).RequireAuthorization();
        auth.MapGet("/manage/pendingEmail", AccountChanges.GetPendingEmail).RequireAuthorization().WithName("GetPendingEmail");
        auth.MapDelete("/manage/pendingEmail", AccountChanges.CancelPendingEmail).RequireAuthorization().WithName("CancelPendingEmail");

        return app;
    }

    /// <summary>
    /// Puts the Identity endpoints that send an email under the stricter email limit.
    /// <c>POST /manage/info</c> sends one when the user changes their email.
    /// </summary>
    private static void LimitEmailSending(EndpointBuilder endpoint)
    {
        if (EmailSendingRoutes.Any(route => Is(endpoint, HttpMethods.Post, route)))
        {
            endpoint.Metadata.Add(new EnableRateLimitingAttribute(RateLimits.Email));
        }
    }

    /// <summary>
    /// Runs <see cref="AccountChanges.GuardInfoAsync"/> around <c>POST /manage/info</c> and
    /// <see cref="AccountChanges.GuardEmailConfirmationAsync"/> around <c>GET /confirmEmail</c>.
    /// </summary>
    private static void GuardAccountChanges(EndpointBuilder endpoint)
    {
        if (Is(endpoint, HttpMethods.Post, "/manage/info"))
        {
            endpoint.FilterFactories.Add((_, next) => invocation => AccountChanges.GuardInfoAsync(invocation, next));
        }
        else if (Is(endpoint, HttpMethods.Get, "/confirmEmail"))
        {
            endpoint.FilterFactories.Add((_, next) => invocation => AccountChanges.GuardEmailConfirmationAsync(invocation, next));
        }
    }

    private static bool Is(EndpointBuilder endpoint, string method, string route) =>
        endpoint is RouteEndpointBuilder { RoutePattern.RawText: { } pattern }
        && pattern.EndsWith(route)
        && endpoint.Metadata.OfType<HttpMethodMetadata>().Any(metadata => metadata.HttpMethods.Contains(method));

    /// <summary>Signs the current user out by clearing the Identity cookie.</summary>
    public static async Task<Ok> Logout(SignInManager<IdentityUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return TypedResults.Ok();
    }
}
