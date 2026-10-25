using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;

namespace TodoApi.Auth;

/// <summary>
/// Emails a code to the user when Identity's <c>POST /login</c> asks for a second factor and the request carried no
/// code, so logging in without one sends a new code and logging in again with it signs the user in. Identity asks only
/// once the password is right. The code counts towards its email limit as it is sent, rather than before the endpoint
/// runs, so the login that submits the code is never held back.
/// </summary>
public static class TwoFactorCodes
{
    private const string RequiresTwoFactor = nameof(SignInResult.RequiresTwoFactor);

    public static async ValueTask<object?> SendAsync(EndpointFilterInvocationContext invocation, EndpointFilterDelegate next)
    {
        var result = await next(invocation);
        var request = invocation.Arguments.OfType<LoginRequest>().Single();
        if (!AsksForCode(result) || !string.IsNullOrEmpty(request.TwoFactorCode) || !string.IsNullOrEmpty(request.TwoFactorRecoveryCode))
        {
            return result;
        }

        var context = invocation.HttpContext;
        var services = context.RequestServices;
        using var lease = services.GetRequiredService<EmailLimits>().Take(context, EmailKind.TwoFactorCode);
        if (!lease.IsAcquired)
        {
            return RateLimits.Rejection(context, lease);
        }

        var signInManager = services.GetRequiredService<SignInManager<IdentityUser>>();
        var user = (await signInManager.GetTwoFactorAuthenticationUserAsync())!;
        var code = await signInManager.UserManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
        await services.GetRequiredService<ITwoFactorCodeSender>().SendTwoFactorCodeAsync(user, user.Email!, code);
        return result;
    }

    private static bool AsksForCode(object? result) =>
        ((result as INestedHttpResult)?.Result ?? result) is ProblemHttpResult { ProblemDetails.Detail: RequiresTwoFactor };
}
