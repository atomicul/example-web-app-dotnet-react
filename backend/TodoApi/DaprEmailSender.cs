using System.Net;
using System.Text.Json.Serialization;
using Dapr.Client;
using Microsoft.AspNetCore.Identity;

namespace TodoApi;

/// <summary>What an <see cref="EmailMessage"/>'s value is, which tells its consumer how to word the email.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<EmailKind>))]
public enum EmailKind
{
    ConfirmationLink,
    PasswordResetLink,
    PasswordResetCode,
}

/// <summary>An email for the queue's consumer to word and send to <paramref name="To"/>.</summary>
public record EmailMessage(EmailKind Kind, string To, string Value);

/// <summary>
/// Puts Identity's emails on the queue behind the Dapr output binding named by <c>Email:Queue</c>, for another service
/// to send. Links are decoded from Identity's HTML encoding, so the consumer gets them as plain URLs.
/// </summary>
public class DaprEmailSender(DaprClient dapr, IConfiguration configuration) : IEmailSender<IdentityUser>
{
    private const string CreateOperation = "create";

    public Task SendConfirmationLinkAsync(IdentityUser user, string email, string confirmationLink) =>
        Send(new EmailMessage(EmailKind.ConfirmationLink, email, WebUtility.HtmlDecode(confirmationLink)));

    public Task SendPasswordResetLinkAsync(IdentityUser user, string email, string resetLink) =>
        Send(new EmailMessage(EmailKind.PasswordResetLink, email, WebUtility.HtmlDecode(resetLink)));

    public Task SendPasswordResetCodeAsync(IdentityUser user, string email, string resetCode) =>
        Send(new EmailMessage(EmailKind.PasswordResetCode, email, resetCode));

    private Task Send(EmailMessage message) =>
        dapr.InvokeBindingAsync(configuration[Emails.QueueKey]!, CreateOperation, message);
}
