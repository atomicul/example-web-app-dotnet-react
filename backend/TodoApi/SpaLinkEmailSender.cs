using System.Net;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;

namespace TodoApi;

/// <summary>
/// Points Identity's emails at the SPA, then hands the email to the transport it wraps.
/// Confirmation links open the SPA's confirmation page, which calls Identity's endpoint with the same query.
/// Password reset codes become links to the SPA's reset page, which sends the code with the new password.
/// The pages live at the <c>SpaUrl</c> setting, or on the API's own origin when the API serves the SPA itself.
/// Links reach the transport HTML-encoded, just as Identity hands them over.
/// </summary>
public class SpaLinkEmailSender(
    [FromKeyedServices(SpaLinkEmailSender.TransportKey)] IEmailSender<IdentityUser> transport,
    IConfiguration configuration,
    IHttpContextAccessor httpContextAccessor) : IEmailSender<IdentityUser>
{
    /// <summary>The service key of the <see cref="IEmailSender{TUser}"/> that delivers the emails.</summary>
    public const string TransportKey = "transport";

    private const string ConfirmEmailPage = "/confirm-email";
    private const string ResetPasswordPage = "/reset-password";

    public Task SendConfirmationLinkAsync(IdentityUser user, string email, string confirmationLink) =>
        transport.SendConfirmationLinkAsync(user, email, ToConfirmEmailPage(confirmationLink));

    public Task SendPasswordResetLinkAsync(IdentityUser user, string email, string resetLink) =>
        transport.SendPasswordResetLinkAsync(user, email, resetLink);

    public Task SendPasswordResetCodeAsync(IdentityUser user, string email, string resetCode) =>
        transport.SendPasswordResetLinkAsync(user, email, ToResetPasswordPage(email, resetCode));

    private string ToConfirmEmailPage(string confirmationLink)
    {
        var link = new Uri(WebUtility.HtmlDecode(confirmationLink));
        return ToSpaPage(link.GetLeftPart(UriPartial.Authority), ConfirmEmailPage, link.Query);
    }

    private string ToResetPasswordPage(string email, string resetCode)
    {
        var request = httpContextAccessor.HttpContext!.Request;
        var query = QueryString.Create("email", email).Add("code", WebUtility.HtmlDecode(resetCode));
        return ToSpaPage($"{request.Scheme}://{request.Host}", ResetPasswordPage, query.ToUriComponent());
    }

    private string ToSpaPage(string apiOrigin, string path, string query)
    {
        var spaUrl = configuration["SpaUrl"] ?? apiOrigin;
        var page = new UriBuilder(spaUrl) { Path = path, Query = query }.Uri.AbsoluteUri;
        return HtmlEncoder.Default.Encode(page);
    }
}
