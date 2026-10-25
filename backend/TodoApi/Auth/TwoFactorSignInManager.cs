using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace TodoApi.Auth;

/// <summary>
/// Asks every user for a second factor before signing them in while <c>enabled</c>, rather than only the users who set
/// up an authenticator app. Identity's <c>POST /login</c> checks its <c>twoFactorCode</c> with the authenticator app's
/// provider only, so the code of a user who gets theirs by email is checked with the email provider instead (see
/// <see cref="TwoFactorMethods"/>). The browser is never remembered, so every login asks for a code.
/// </summary>
public class TwoFactorSignInManager(
    UserManager<IdentityUser> userManager,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<IdentityUser> claimsFactory,
    IOptions<IdentityOptions> optionsAccessor,
    ILogger<SignInManager<IdentityUser>> logger,
    IAuthenticationSchemeProvider schemes,
    IUserConfirmation<IdentityUser> confirmation,
    bool enabled)
    : SignInManager<IdentityUser>(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
{
    public override Task<bool> IsTwoFactorEnabledAsync(IdentityUser user) => Task.FromResult(enabled);

    public override async Task<SignInResult> TwoFactorAuthenticatorSignInAsync(string code, bool isPersistent, bool rememberClient) =>
        await GetTwoFactorAuthenticationUserAsync() is { } user && await UserManager.GetTwoFactorMethodAsync(user) == TwoFactorMethod.Authenticator
            ? await base.TwoFactorAuthenticatorSignInAsync(code, isPersistent, rememberClient: false)
            : await TwoFactorSignInAsync(TokenOptions.DefaultEmailProvider, code, isPersistent, rememberClient: false);
}
