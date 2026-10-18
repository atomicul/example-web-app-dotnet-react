using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TodoApi.Auth;

namespace TodoApi.Tests;

public class AccountChangeTests(TodoApiFactory factory)
{
    private const string NewPassword = "N3w-passw0rd";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Email_of_another_user_is_refused()
    {
        var takenEmail = await factory.CreateClient().RegisterConfirmedAsync(factory);
        var client = await factory.CreateUserClientAsync();

        var response = await client.ChangeInfoAsync(new InfoRequest { NewEmail = takenEmail });

        var (_, issues) = await response.ReadValidationProblemAsync();
        Assert.Equal("DuplicateEmail", Assert.Single(issues).Code);
    }

    [Fact]
    public async Task No_email_change_is_pending_at_first()
    {
        var client = await factory.CreateUserClientAsync();

        Assert.Null(await GetPendingEmailAsync(client));
    }

    [Fact]
    public async Task Email_change_is_pending_until_confirmed()
    {
        var client = await factory.CreateUserClientAsync();
        var newEmail = TestUsers.NewEmail();

        (await client.ChangeInfoAsync(new InfoRequest { NewEmail = newEmail })).EnsureSuccessStatusCode();
        var beforeConfirming = await GetPendingEmailAsync(client);
        (await client.ConfirmEmailAsync(factory.Emails.ConfirmationLinkFor(newEmail))).EnsureSuccessStatusCode();
        var afterConfirming = await GetPendingEmailAsync(client);
        var login = await factory.CreateClient().LoginAsync(newEmail);

        Assert.Equal(newEmail, beforeConfirming);
        Assert.Null(afterConfirming);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Cancelled_email_change_cannot_be_confirmed()
    {
        var client = factory.CreateClient();
        var email = await client.RegisterConfirmedAsync(factory);
        (await client.LoginAsync(email, useCookies: true)).EnsureSuccessStatusCode();
        var newEmail = TestUsers.NewEmail();

        (await client.ChangeInfoAsync(new InfoRequest { NewEmail = newEmail })).EnsureSuccessStatusCode();
        (await client.DeleteAsync("/api/auth/manage/pendingEmail", CancellationToken)).EnsureSuccessStatusCode();
        var pending = await GetPendingEmailAsync(client);
        var confirmation = await client.ConfirmEmailAsync(factory.Emails.ConfirmationLinkFor(newEmail));
        var login = await factory.CreateClient().LoginAsync(email);

        Assert.Null(pending);
        Assert.Equal(HttpStatusCode.Unauthorized, confirmation.StatusCode);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Only_the_latest_email_change_can_be_confirmed()
    {
        var client = await factory.CreateUserClientAsync();
        var firstEmail = TestUsers.NewEmail();
        var latestEmail = TestUsers.NewEmail();

        (await client.ChangeInfoAsync(new InfoRequest { NewEmail = firstEmail })).EnsureSuccessStatusCode();
        (await client.ChangeInfoAsync(new InfoRequest { NewEmail = latestEmail })).EnsureSuccessStatusCode();
        var first = await client.ConfirmEmailAsync(factory.Emails.ConfirmationLinkFor(firstEmail));
        var latest = await client.ConfirmEmailAsync(factory.Emails.ConfirmationLinkFor(latestEmail));

        Assert.Equal(HttpStatusCode.Unauthorized, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, latest.StatusCode);
    }

    [Fact]
    public async Task Links_sent_earlier_for_the_same_email_keep_working()
    {
        var client = await factory.CreateUserClientAsync();
        var newEmail = TestUsers.NewEmail();

        (await client.ChangeInfoAsync(new InfoRequest { NewEmail = newEmail })).EnsureSuccessStatusCode();
        var firstLink = factory.Emails.ConfirmationLinkFor(newEmail);
        (await client.ChangeInfoAsync(new InfoRequest { NewEmail = newEmail })).EnsureSuccessStatusCode();
        var confirmation = await client.ConfirmEmailAsync(firstLink);

        Assert.Equal(HttpStatusCode.OK, confirmation.StatusCode);
    }

    [Fact]
    public async Task Password_change_drops_the_pending_email_change()
    {
        var client = await factory.CreateUserClientAsync();
        var newEmail = TestUsers.NewEmail();

        (await client.ChangeInfoAsync(new InfoRequest { NewEmail = newEmail })).EnsureSuccessStatusCode();
        (await client.ChangeInfoAsync(new InfoRequest { OldPassword = TestUsers.Password, NewPassword = NewPassword }))
            .EnsureSuccessStatusCode();
        var pending = await GetPendingEmailAsync(client);
        var confirmation = await client.ConfirmEmailAsync(factory.Emails.ConfirmationLinkFor(newEmail));

        Assert.Null(pending);
        Assert.Equal(HttpStatusCode.Unauthorized, confirmation.StatusCode);
    }

    [Fact]
    public async Task Password_change_keeps_the_session_that_made_it()
    {
        var client = ValidatingSessionsOnEveryRequest().CreateClient();
        var email = await client.RegisterConfirmedAsync(factory);
        (await client.LoginAsync(email, useCookies: true)).EnsureSuccessStatusCode();

        (await client.ChangeInfoAsync(new InfoRequest { OldPassword = TestUsers.Password, NewPassword = NewPassword }))
            .EnsureSuccessStatusCode();
        var response = await client.GetAsync("/api/todos", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Password_change_ends_the_other_sessions()
    {
        var server = ValidatingSessionsOnEveryRequest();
        var client = server.CreateClient();
        var otherSession = server.CreateClient();
        var email = await client.RegisterConfirmedAsync(factory);
        (await client.LoginAsync(email, useCookies: true)).EnsureSuccessStatusCode();
        (await otherSession.LoginAsync(email, useCookies: true)).EnsureSuccessStatusCode();

        (await client.ChangeInfoAsync(new InfoRequest { OldPassword = TestUsers.Password, NewPassword = NewPassword }))
            .EnsureSuccessStatusCode();
        var response = await otherSession.GetAsync("/api/todos", CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<string?> GetPendingEmailAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<PendingEmailResponse>("/api/auth/manage/pendingEmail", CancellationToken))!.Email;

    /// <summary>A server that checks each cookie's security stamp on every request, instead of every 30 minutes.</summary>
    private WebApplicationFactory<Program> ValidatingSessionsOnEveryRequest() =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero)));
}
