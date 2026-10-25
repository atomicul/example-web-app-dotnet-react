using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace TodoApi.Auth;

/// <summary>
/// Asks every user for a code emailed to them before signing them in while the <c>TwoFactor:Enabled</c> setting is on,
/// rather than only the users who turned two-factor authentication on themselves. Identity's <c>POST /login</c> checks
/// its <c>twoFactorCode</c> with the authenticator app's provider only, so the code is checked with the email provider
/// instead. The browser is never remembered, so every login asks for a code.
/// </summary>
public class TwoFactorSignInManager(
    UserManager<IdentityUser> userManager,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<IdentityUser> claimsFactory,
    IOptions<IdentityOptions> optionsAccessor,
    ILogger<SignInManager<IdentityUser>> logger,
    IAuthenticationSchemeProvider schemes,
    IUserConfirmation<IdentityUser> confirmation,
    IConfiguration configuration)
    : SignInManager<IdentityUser>(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
{
    public const string EnabledSetting = "TwoFactor:Enabled";

    public override Task<bool> IsTwoFactorEnabledAsync(IdentityUser user) =>
        Task.FromResult(configuration.GetValue<bool>(EnabledSetting));

    public override Task<SignInResult> TwoFactorAuthenticatorSignInAsync(string code, bool isPersistent, bool rememberClient) =>
        TwoFactorSignInAsync(TokenOptions.DefaultEmailProvider, code, isPersistent, rememberClient: false);
}
