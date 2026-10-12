using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Identity;

namespace TodoApi.Tests;

/// <summary>Keeps the confirmation links the API sends, decoded, so tests can follow them.</summary>
public class CapturingEmailSender : IEmailSender<IdentityUser>
{
    private readonly ConcurrentQueue<(string Email, Uri Link)> confirmationLinks = new();

    /// <summary>The latest confirmation link sent to the email.</summary>
    public Uri ConfirmationLinkFor(string email) => confirmationLinks.Last(sent => sent.Email == email).Link;

    public Task SendConfirmationLinkAsync(IdentityUser user, string email, string confirmationLink)
    {
        confirmationLinks.Enqueue((email, new Uri(WebUtility.HtmlDecode(confirmationLink))));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(IdentityUser user, string email, string resetLink) => Task.CompletedTask;

    public Task SendPasswordResetCodeAsync(IdentityUser user, string email, string resetCode) => Task.CompletedTask;
}
