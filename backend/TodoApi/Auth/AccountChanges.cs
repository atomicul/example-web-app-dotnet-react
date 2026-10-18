using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using TodoApi.Validation;

namespace TodoApi.Auth;

/// <summary>
/// Endpoint filter for Identity's <c>POST /manage/info</c>, which changes the signed-in user's email or password. It
/// refuses an email another user has, which Identity would only notice once the change is confirmed, after changing
/// the email but not the user name. After a password change, it renews the session's cookie with the new security
/// stamp, so the session that made the change stays signed in while every other one ends.
/// </summary>
public static class AccountChanges
{
    public static async ValueTask<object?> GuardAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<InfoRequest>().Single();
        var services = context.HttpContext.RequestServices;
        var users = services.GetRequiredService<UserManager<IdentityUser>>();
        if (await users.GetUserAsync(context.HttpContext.User) is not { } user)
        {
            return await next(context);
        }

        if (await IsTakenByAnotherUserAsync(users, user, request.NewEmail))
        {
            var error = users.ErrorDescriber.DuplicateEmail(request.NewEmail!);
            return ValidationProblems.For([new ValidationError(null, error.Code, error.Description)]);
        }

        var result = await next(context);
        if (IsSuccess(result) && !string.IsNullOrEmpty(request.NewPassword))
        {
            await RenewCookieAsync(context.HttpContext, services.GetRequiredService<SignInManager<IdentityUser>>(), user);
        }
        return result;
    }

    private static async Task<bool> IsTakenByAnotherUserAsync(UserManager<IdentityUser> users, IdentityUser user, string? email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return false;
        }
        var owner = await users.FindByEmailAsync(email) ?? await users.FindByNameAsync(email);
        return owner is not null && owner.Id != user.Id;
    }

    private static bool IsSuccess(object? result) =>
        ((result as INestedHttpResult)?.Result ?? result) is Ok<InfoResponse>;

    /// <summary>Signs the user in again when the request came with Identity's cookie; bearer tokens are left as they are.</summary>
    private static async Task RenewCookieAsync(HttpContext context, SignInManager<IdentityUser> signInManager, IdentityUser user)
    {
        if ((await context.AuthenticateAsync(IdentityConstants.ApplicationScheme)).Succeeded)
        {
            await signInManager.RefreshSignInAsync(user);
        }
    }
}
