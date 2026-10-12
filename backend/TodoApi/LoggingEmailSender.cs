using System.Net;
using Microsoft.AspNetCore.Identity;

namespace TodoApi;

/// <summary>
/// Stand-in for a real email service that writes Identity emails to the application log.
/// Identity HTML-encodes links for embedding in markup, so they are decoded here to stay clickable.
/// Confirmation links open the SPA's confirmation page, which calls Identity's endpoint with the same query.
/// The page lives at the <c>SpaUrl</c> setting, or on the API's own origin when the API serves the SPA itself.
/// </summary>
public class LoggingEmailSender(ILogger<LoggingEmailSender> logger, IConfiguration configuration) : IEmailSender<IdentityUser>
{
    private const string ConfirmEmailPage = "/confirm-email";

    public Task SendConfirmationLinkAsync(IdentityUser user, string email, string confirmationLink) =>
        Log(email, "Confirm your email", ToConfirmEmailPage(WebUtility.HtmlDecode(confirmationLink)));

    public Task SendPasswordResetLinkAsync(IdentityUser user, string email, string resetLink) =>
        Log(email, "Reset your password", WebUtility.HtmlDecode(resetLink));

    public Task SendPasswordResetCodeAsync(IdentityUser user, string email, string resetCode) =>
        Log(email, "Reset your password", resetCode);

    private string ToConfirmEmailPage(string confirmationLink)
    {
        var link = new Uri(confirmationLink);
        var spaUrl = configuration["SpaUrl"] ?? link.GetLeftPart(UriPartial.Authority);
        return new UriBuilder(spaUrl) { Path = ConfirmEmailPage, Query = link.Query }.Uri.AbsoluteUri;
    }

    private Task Log(string email, string subject, string content)
    {
        logger.LogInformation("Email to {Email} | {Subject}: {Content}", email, subject, content);
        return Task.CompletedTask;
    }
}
