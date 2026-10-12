using System.Net;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;

namespace TodoApi;

/// <summary>
/// Points Identity's email links at the SPA, then hands the email to the transport it wraps.
/// Confirmation links open the SPA's confirmation page, which calls Identity's endpoint with the same query.
/// The page lives at the <c>SpaUrl</c> setting, or on the API's own origin when the API serves the SPA itself.
/// Links reach the transport HTML-encoded, just as Identity hands them over.
/// </summary>
public class SpaLinkEmailSender(
    [FromKeyedServices(SpaLinkEmailSender.TransportKey)] IEmailSender<IdentityUser> transport,
    IConfiguration configuration) : IEmailSender<IdentityUser>
{
    /// <summary>The service key of the <see cref="IEmailSender{TUser}"/> that delivers the emails.</summary>
    public const string TransportKey = "transport";

    private const string ConfirmEmailPage = "/confirm-email";

    public Task SendConfirmationLinkAsync(IdentityUser user, string email, string confirmationLink) =>
        transport.SendConfirmationLinkAsync(user, email, ToConfirmEmailPage(confirmationLink));

    public Task SendPasswordResetLinkAsync(IdentityUser user, string email, string resetLink) =>
        transport.SendPasswordResetLinkAsync(user, email, resetLink);

    public Task SendPasswordResetCodeAsync(IdentityUser user, string email, string resetCode) =>
        transport.SendPasswordResetCodeAsync(user, email, resetCode);

    private string ToConfirmEmailPage(string confirmationLink)
    {
        var link = new Uri(WebUtility.HtmlDecode(confirmationLink));
        var spaUrl = configuration["SpaUrl"] ?? link.GetLeftPart(UriPartial.Authority);
        var page = new UriBuilder(spaUrl) { Path = ConfirmEmailPage, Query = link.Query }.Uri.AbsoluteUri;
        return HtmlEncoder.Default.Encode(page);
    }
}
