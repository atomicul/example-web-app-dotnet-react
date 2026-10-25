using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using TodoApi.Validation;

namespace TodoApi.Auth;

/// <summary>
/// The way each user finishes a login that asks for a second factor: a code emailed to them, or one their
/// authenticator app shows. Identity's <c>POST /manage/2fa</c> sets the app up, and turns on Identity's own two-factor
/// flag of the user once it checks a code from it, which is what marks the app as the user's way.
/// <see cref="GuardSetupAsync"/> filters that endpoint.
/// </summary>
public static class TwoFactorMethods
{
    /// <summary>The member of a login's <c>RequiresTwoFactor</c> problem that names the user's way.</summary>
    public const string ProblemMember = "twoFactorMethod";

    public static async Task<TwoFactorMethod> GetTwoFactorMethodAsync(this UserManager<IdentityUser> users, IdentityUser user) =>
        await users.GetTwoFactorEnabledAsync(user) ? TwoFactorMethod.Authenticator : TwoFactorMethod.Email;

    /// <summary>Gets how the signed-in user finishes a login, and whether logins ask for a second factor at all.</summary>
    public static async Task<Results<Ok<TwoFactorMethodResponse>, NotFound>> GetTwoFactorMethod(
        ClaimsPrincipal principal, SignInManager<IdentityUser> signInManager)
    {
        if (await signInManager.UserManager.GetUserAsync(principal) is not { } user)
        {
            return TypedResults.NotFound();
        }

        var isAvailable = await signInManager.IsTwoFactorEnabledAsync(user);
        return TypedResults.Ok(new TwoFactorMethodResponse(isAvailable, await signInManager.UserManager.GetTwoFactorMethodAsync(user)));
    }

    /// <summary>
    /// Refuses to turn the authenticator app on while logins ask for no second factor. After a change, it renews the
    /// session's cookie, as changing the key or turning the app on or off renews the security stamp, which would
    /// otherwise end the session that made the change.
    /// </summary>
    public static async ValueTask<object?> GuardSetupAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TwoFactorRequest>().Single();
        var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<IdentityUser>>();
        if (await signInManager.UserManager.GetUserAsync(context.HttpContext.User) is not { } user)
        {
            return await next(context);
        }

        if (request.Enable == true && !await signInManager.IsTwoFactorEnabledAsync(user))
        {
            return ValidationProblems.For([new ValidationError(null, "TwoFactorUnavailable", "Logins ask for no second factor.")]);
        }

        var result = await next(context);
        if (((result as INestedHttpResult)?.Result ?? result) is Ok<TwoFactorResponse>)
        {
            await AccountChanges.RenewCookieAsync(context.HttpContext, signInManager, user);
        }
        return result;
    }
}
