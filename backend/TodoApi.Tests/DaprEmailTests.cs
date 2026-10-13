using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace TodoApi.Tests;

/// <summary>
/// Emails sent with <c>Email:Queue</c> set, through the real Dapr sidecar. The server's own email setup is applied
/// again over the factory's, which captures emails instead.
/// </summary>
public class DaprEmailTests(TodoApiFactory factory, DaprSidecarContainer dapr)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Registration_queues_a_link_to_the_spa_confirmation_page()
    {
        var email = TestUsers.NewEmail();

        await QueueingEmails().CreateClient().RegisterAsync(email);

        var message = Assert.Single(MessagesTo(email));
        Assert.Equal(EmailKind.ConfirmationLink, message.Kind);
        Assert.StartsWith($"{TodoApiFactory.SpaUrl}/confirm-email?", message.Value);
    }

    [Fact]
    public async Task Forgotten_password_queues_a_reset_code()
    {
        var client = QueueingEmails().CreateClient();
        var email = TestUsers.NewEmail();
        await client.RegisterAsync(email);
        await client.ConfirmEmailAsync(new Uri(Assert.Single(MessagesTo(email)).Value));

        await client.PostAsJsonAsync("/api/auth/forgotPassword", new ForgotPasswordRequest { Email = email },
            CancellationToken);

        var message = Assert.Single(MessagesTo(email), sent => sent.Kind == EmailKind.PasswordResetCode);
        Assert.NotEmpty(message.Value);
    }

    private IEnumerable<EmailMessage> MessagesTo(string email) =>
        dapr.MessagesSentTo<EmailMessage>(DaprSidecarContainer.EmailsBinding).Where(message => message.To == email);

    private WebApplicationFactory<Program> QueueingEmails()
    {
        var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [Emails.QueueKey] = DaprSidecarContainer.EmailsBinding,
            [DaprSidecar.GrpcEndpointKey] = dapr.GrpcEndpoint,
        }).Build();
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddConfiguration(settings));
            builder.ConfigureTestServices(services => services.AddEmails(settings));
        });
    }
}
