using System.Net;
using Microsoft.AspNetCore.Identity;

namespace TodoApi;

/// <summary>
/// Stand-in for a real email service that writes Identity emails to the application log.
/// Identity HTML-encodes links for embedding in markup, so they are decoded here to stay clickable.
/// </summary>
public class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender<IdentityUser>
{
    public Task SendConfirmationLinkAsync(IdentityUser user, string email, string confirmationLink) =>
        Log(email, "Confirm your email", WebUtility.HtmlDecode(confirmationLink));

    public Task SendPasswordResetLinkAsync(IdentityUser user, string email, string resetLink) =>
        Log(email, "Reset your password", WebUtility.HtmlDecode(resetLink));

    public Task SendPasswordResetCodeAsync(IdentityUser user, string email, string resetCode) =>
        Log(email, "Reset your password", resetCode);

    private Task Log(string email, string subject, string content)
    {
        logger.LogInformation("Email to {Email} | {Subject}: {Content}", email, subject, content);
        return Task.CompletedTask;
    }
}
