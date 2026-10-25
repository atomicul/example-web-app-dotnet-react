using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;

namespace TodoApi.Auth;

/// <summary>
/// Names the user's way of finishing the login when Identity's <c>POST /login</c> asks for a second factor, in the
/// problem's <see cref="TwoFactorMethods.ProblemMember"/>, and emails a code to a user who gets theirs by email when the
/// request carried no code, so logging in without one sends a new code and logging in again with it signs the user
/// in. Identity asks only once the password is right. The code counts towards its email limit as it is sent, rather
/// than before the endpoint runs, so the login that submits the code is never held back.
/// </summary>
public static class TwoFactorCodes
{
    private const string RequiresTwoFactor = nameof(SignInResult.RequiresTwoFactor);

    public static async ValueTask<object?> SendAsync(EndpointFilterInvocationContext invocation, EndpointFilterDelegate next)
    {
        var result = await next(invocation);
        if (AskedForCode(result) is not { } problem)
        {
            return result;
        }

        var context = invocation.HttpContext;
        var services = context.RequestServices;
        var signInManager = services.GetRequiredService<SignInManager<IdentityUser>>();
        var user = (await signInManager.GetTwoFactorAuthenticationUserAsync())!;
        var method = await signInManager.UserManager.GetTwoFactorMethodAsync(user);
        problem.ProblemDetails.Extensions[TwoFactorMethods.ProblemMember] = method;
        var request = invocation.Arguments.OfType<LoginRequest>().Single();
        if (method != TwoFactorMethod.Email || !string.IsNullOrEmpty(request.TwoFactorCode) || !string.IsNullOrEmpty(request.TwoFactorRecoveryCode))
        {
            return result;
        }

        using var lease = services.GetRequiredService<EmailLimits>().Take(context, EmailKind.TwoFactorCode);
        if (!lease.IsAcquired)
        {
            return RateLimits.Rejection(context, lease);
        }

        var code = await signInManager.UserManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
        await services.GetRequiredService<ITwoFactorCodeSender>().SendTwoFactorCodeAsync(user, user.Email!, code);
        return result;
    }

    private static ProblemHttpResult? AskedForCode(object? result) =>
        ((result as INestedHttpResult)?.Result ?? result) is ProblemHttpResult { ProblemDetails.Detail: RequiresTwoFactor } problem
            ? problem
            : null;
}
