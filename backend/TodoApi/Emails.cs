using Microsoft.AspNetCore.Identity;

namespace TodoApi;

/// <summary>
/// Sends Identity's emails with links that open the SPA, writing them to the log. A deployment that sends real emails
/// registers its own transport under <see cref="SpaLinkEmailSender.TransportKey"/> instead.
/// </summary>
public static class Emails
{
    public static IServiceCollection AddEmails(this IServiceCollection services)
    {
        services.AddKeyedTransient<IEmailSender<IdentityUser>, LoggingEmailSender>(SpaLinkEmailSender.TransportKey);
        return services.AddTransient<IEmailSender<IdentityUser>, SpaLinkEmailSender>();
    }
}
