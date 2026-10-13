using Microsoft.AspNetCore.Identity;

namespace TodoApi;

/// <summary>
/// Sends Identity's emails with links that open the SPA. When <c>Email:Queue</c> names a Dapr output binding, they go
/// on the queue behind it; otherwise, as in development, they are written to the log.
/// </summary>
public static class Emails
{
    public const string QueueKey = "Email:Queue";

    public static IServiceCollection AddEmails(this IServiceCollection services, IConfiguration configuration)
    {
        if (string.IsNullOrEmpty(configuration[QueueKey]))
        {
            services.AddKeyedTransient<IEmailSender<IdentityUser>, LoggingEmailSender>(SpaLinkEmailSender.TransportKey);
        }
        else
        {
            services.AddKeyedTransient<IEmailSender<IdentityUser>, DaprEmailSender>(SpaLinkEmailSender.TransportKey);
        }
        return services.AddTransient<IEmailSender<IdentityUser>, SpaLinkEmailSender>();
    }
}
