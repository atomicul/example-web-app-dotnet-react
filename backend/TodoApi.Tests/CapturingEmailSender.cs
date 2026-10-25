using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Identity;

namespace TodoApi.Tests;

/// <summary>
/// Keeps the confirmation and password reset links the API sends, decoded, and the codes that finish a login, so
/// tests can follow them.
/// </summary>
public class CapturingEmailSender : IEmailSender<IdentityUser>, ITwoFactorCodeSender
{
    private readonly ConcurrentQueue<(string Email, Uri Link)> confirmationLinks = new();
    private readonly ConcurrentQueue<(string Email, Uri Link)> resetLinks = new();
    private readonly ConcurrentQueue<(string Email, string Code)> twoFactorCodes = new();

    /// <summary>The latest confirmation link sent to the email.</summary>
    public Uri ConfirmationLinkFor(string email) => LatestFor(confirmationLinks, email);

    /// <summary>The latest password reset link sent to the email.</summary>
    public Uri ResetLinkFor(string email) => LatestFor(resetLinks, email);

    /// <summary>The codes sent to the email, oldest first.</summary>
    public IEnumerable<string> TwoFactorCodesFor(string email) =>
        twoFactorCodes.Where(sent => sent.Email == email).Select(sent => sent.Code);

    public Task SendConfirmationLinkAsync(IdentityUser user, string email, string confirmationLink) =>
        Capture(confirmationLinks, email, confirmationLink);

    public Task SendPasswordResetLinkAsync(IdentityUser user, string email, string resetLink) =>
        Capture(resetLinks, email, resetLink);

    public Task SendPasswordResetCodeAsync(IdentityUser user, string email, string resetCode) => Task.CompletedTask;

    public Task SendTwoFactorCodeAsync(IdentityUser user, string email, string code)
    {
        twoFactorCodes.Enqueue((email, code));
        return Task.CompletedTask;
    }

    private static Uri LatestFor(ConcurrentQueue<(string Email, Uri Link)> links, string email) =>
        links.Last(sent => sent.Email == email).Link;

    private static Task Capture(ConcurrentQueue<(string Email, Uri Link)> links, string email, string link)
    {
        links.Enqueue((email, new Uri(WebUtility.HtmlDecode(link))));
        return Task.CompletedTask;
    }
}
