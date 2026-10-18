using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Extensions.Options;
using TodoApi.Validation;

namespace TodoApi.Auth;

/// <summary>
/// Changes of the signed-in user's email and password, on top of Identity's <c>POST /manage/info</c>.
/// <see cref="GuardInfoAsync"/> filters that endpoint: it refuses an email another user has, which Identity would only
/// notice once the change is confirmed, after changing the email but not the user name. After a password change, it
/// renews the session's cookie with the new security stamp, so the session that made the change stays signed in
/// while every other one ends. Identity's email change links carry everything they need, so nothing records that a
/// change is waiting: the filter keeps the new email with the user's authentication tokens, for
/// <see cref="GetPendingEmail"/> to show and <see cref="CancelPendingEmail"/> to drop.
/// <see cref="GuardEmailConfirmationAsync"/> then lets only that email's links through Identity's <c>GET /confirmEmail</c>.
/// </summary>
public static class AccountChanges
{
    private const string LoginProvider = "EmailChange";
    private const string PendingTokenName = "Pending";

    public static async ValueTask<object?> GuardInfoAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
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
        if (!IsSuccess(result))
        {
            return result;
        }
        if (!string.IsNullOrEmpty(request.NewPassword))
        {
            await RenewCookieAsync(context.HttpContext, services.GetRequiredService<SignInManager<IdentityUser>>(), user);
        }
        if (!string.IsNullOrEmpty(request.NewEmail) && request.NewEmail != user.Email)
        {
            await RememberPendingEmailAsync(users, user, request.NewEmail);
        }
        return result;
    }

    /// <summary>
    /// Refuses a link that changes the email unless it is for the change the user asked for last and hasn't
    /// cancelled, so cancelling a change, or asking for another, makes the links sent for it stop working.
    /// </summary>
    public static async ValueTask<object?> GuardEmailConfirmationAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var query = context.HttpContext.Request.Query;
        var changedEmail = query["changedEmail"].ToString();
        if (string.IsNullOrEmpty(changedEmail))
        {
            return await next(context);
        }

        var users = context.HttpContext.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
        var user = await users.FindByIdAsync(query["userId"].ToString());
        var pending = user is null ? null : await ReadPendingEmailAsync(users, user);
        return pending?.Email == changedEmail ? await next(context) : TypedResults.Unauthorized();
    }

    /// <summary>Gets the email the signed-in user asked to change to, while the link that confirms it still works.</summary>
    public static async Task<Results<Ok<PendingEmailResponse>, NotFound>> GetPendingEmail(
        ClaimsPrincipal principal, UserManager<IdentityUser> users, IOptions<DataProtectionTokenProviderOptions> tokenOptions)
    {
        if (await users.GetUserAsync(principal) is not { } user)
        {
            return TypedResults.NotFound();
        }

        var pending = await ReadPendingEmailAsync(users, user);
        var isConfirmable = pending is not null
            && pending.SecurityStamp == user.SecurityStamp
            && pending.RequestedAt + tokenOptions.Value.TokenLifespan > DateTimeOffset.UtcNow;
        return TypedResults.Ok(new PendingEmailResponse(isConfirmable ? pending!.Email : null));
    }

    /// <summary>Cancels the signed-in user's email change, so the links sent for it stop working.</summary>
    public static async Task<Results<NoContent, NotFound>> CancelPendingEmail(
        ClaimsPrincipal principal, UserManager<IdentityUser> users)
    {
        if (await users.GetUserAsync(principal) is not { } user)
        {
            return TypedResults.NotFound();
        }

        await users.RemoveAuthenticationTokenAsync(user, LoginProvider, PendingTokenName);
        return TypedResults.NoContent();
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

    /// <summary>
    /// Keeps the new email with the security stamp its link was made with: confirming the change, changing the
    /// password or resetting it renews the stamp, which also makes the link stop working.
    /// </summary>
    private static async Task RememberPendingEmailAsync(UserManager<IdentityUser> users, IdentityUser user, string email)
    {
        var pending = new PendingEmail(email, user.SecurityStamp!, DateTimeOffset.UtcNow);
        await users.SetAuthenticationTokenAsync(user, LoginProvider, PendingTokenName, JsonSerializer.Serialize(pending));
    }

    private static async Task<PendingEmail?> ReadPendingEmailAsync(UserManager<IdentityUser> users, IdentityUser user)
    {
        var stored = await users.GetAuthenticationTokenAsync(user, LoginProvider, PendingTokenName);
        return stored is null ? null : JsonSerializer.Deserialize<PendingEmail>(stored);
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

    private sealed record PendingEmail(string Email, string SecurityStamp, DateTimeOffset RequestedAt);
}
