using Microsoft.AspNetCore.Identity;

namespace TodoApi;

/// <summary>
/// Emails the code that finishes a login, which <see cref="IEmailSender{TUser}"/> has no method for. A deployment that
/// sends real emails implements it next to its transport.
/// </summary>
public interface ITwoFactorCodeSender
{
    Task SendTwoFactorCodeAsync(IdentityUser user, string email, string code);
}
