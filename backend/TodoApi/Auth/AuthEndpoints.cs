using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using TodoApi.Validation;

namespace TodoApi.Auth;

public static class AuthEndpoints
{
    /// <summary>The Identity endpoints that send an email, the kind each sends, and which requests send one.</summary>
    private static readonly (string Route, EmailKind Kind, Func<EndpointFilterInvocationContext, object?, bool> SendsEmail)[] EmailSendingRoutes =
    [
        ("/register", EmailKind.Confirmation, Succeeded),
        ("/resendConfirmationEmail", EmailKind.Confirmation, Succeeded),
        ("/manage/info", EmailKind.Confirmation, ChangedEmail),
        ("/forgotPassword", EmailKind.PasswordReset, Succeeded),
    ];

    public static IEndpointRouteBuilder MapAuth(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/auth").WithTags("Auth");

        var identity = auth.MapIdentityApi<IdentityUser>();
        identity.Add(LimitEmailSending);
        identity.Add(GuardAccountChanges);
        identity.Add(GuardTwoFactorSetup);
        identity.Add(SendTwoFactorCodes);
        identity.AddEndpointFilter(ValidationProblems.CodeIdentityErrors);
        auth.MapPost("/logout", Logout).RequireAuthorization();
        auth.MapGet("/manage/pendingEmail", AccountChanges.GetPendingEmail).RequireAuthorization().WithName("GetPendingEmail");
        auth.MapDelete("/manage/pendingEmail", AccountChanges.CancelPendingEmail).RequireAuthorization().WithName("CancelPendingEmail");
        auth.MapGet("/manage/twoFactorMethod", TwoFactorMethods.GetTwoFactorMethod).RequireAuthorization().WithName("GetTwoFactorMethod");

        return app;
    }

    /// <summary>Puts the Identity endpoints that send an email under the stricter limit of the kind they send.</summary>
    private static void LimitEmailSending(EndpointBuilder endpoint)
    {
        foreach (var (route, kind, sendsEmail) in EmailSendingRoutes.Where(sending => Is(endpoint, HttpMethods.Post, sending.Route)))
        {
            endpoint.FilterFactories.Add((_, next) => invocation => EmailLimits.LimitAsync(invocation, next, kind, sendsEmail));
        }
    }

    private static bool Succeeded(EndpointFilterInvocationContext _, object? result) => EmailLimits.IsSuccess(result);

    /// <summary><c>POST /manage/info</c> sends an email only when it changes the email, not the password alone.</summary>
    private static bool ChangedEmail(EndpointFilterInvocationContext invocation, object? result) =>
        EmailLimits.IsSuccess(result) && !string.IsNullOrEmpty(invocation.Arguments.OfType<InfoRequest>().Single().NewEmail);

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

    /// <summary>Runs <see cref="TwoFactorMethods.GuardSetupAsync"/> around <c>POST /manage/2fa</c>.</summary>
    private static void GuardTwoFactorSetup(EndpointBuilder endpoint)
    {
        if (Is(endpoint, HttpMethods.Post, "/manage/2fa"))
        {
            endpoint.FilterFactories.Add((_, next) => invocation => TwoFactorMethods.GuardSetupAsync(invocation, next));
        }
    }

    /// <summary>Runs <see cref="TwoFactorCodes.SendAsync"/> around <c>POST /login</c>.</summary>
    private static void SendTwoFactorCodes(EndpointBuilder endpoint)
    {
        if (Is(endpoint, HttpMethods.Post, "/login"))
        {
            endpoint.FilterFactories.Add((_, next) => invocation => TwoFactorCodes.SendAsync(invocation, next));
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
