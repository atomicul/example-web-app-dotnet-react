using Microsoft.AspNetCore.Identity;

namespace TodoApi;

/// <summary>
/// Sends Identity's emails with links that open the SPA at the <c>SpaUrl</c> setting, and the codes that finish a login,
/// writing them to the log.
/// A deployment that sends real emails registers its own transport under <see cref="SpaLinkEmailSender.TransportKey"/>
/// and its own <see cref="ITwoFactorCodeSender"/> instead.
/// </summary>
public static class Emails
{
    public static IServiceCollection AddEmails(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddKeyedTransient<IEmailSender<IdentityUser>, LoggingEmailSender>(SpaLinkEmailSender.TransportKey);
        services.AddTransient<ITwoFactorCodeSender, LoggingEmailSender>();
        return services.AddTransient<IEmailSender<IdentityUser>>(provider => new SpaLinkEmailSender(
            provider.GetRequiredKeyedService<IEmailSender<IdentityUser>>(SpaLinkEmailSender.TransportKey),
            configuration["SpaUrl"],
            provider.GetRequiredService<IHttpContextAccessor>()));
    }
}
