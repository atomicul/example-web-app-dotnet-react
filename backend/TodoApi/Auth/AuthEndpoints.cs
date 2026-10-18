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

        return app;
    }

    /// <summary>
    /// Puts the Identity endpoints that send an email under the stricter email limit.
    /// <c>POST /manage/info</c> sends one when the user changes their email.
    /// </summary>
    private static void LimitEmailSending(EndpointBuilder endpoint)
    {
        if (EmailSendingRoutes.Any(route => IsPost(endpoint, route)))
        {
            endpoint.Metadata.Add(new EnableRateLimitingAttribute(RateLimits.Email));
        }
    }

    /// <summary>Runs <see cref="AccountChanges.GuardAsync"/> around <c>POST /manage/info</c>.</summary>
    private static void GuardAccountChanges(EndpointBuilder endpoint)
    {
        if (IsPost(endpoint, "/manage/info"))
        {
            endpoint.FilterFactories.Add((_, next) => invocation => AccountChanges.GuardAsync(invocation, next));
        }
    }

    private static bool IsPost(EndpointBuilder endpoint, string route) =>
        endpoint is RouteEndpointBuilder { RoutePattern.RawText: { } pattern }
        && pattern.EndsWith(route)
        && endpoint.Metadata.OfType<HttpMethodMetadata>().Any(metadata => metadata.HttpMethods.Contains(HttpMethods.Post));

    /// <summary>Signs the current user out by clearing the Identity cookie.</summary>
    public static async Task<Ok> Logout(SignInManager<IdentityUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return TypedResults.Ok();
    }
}
