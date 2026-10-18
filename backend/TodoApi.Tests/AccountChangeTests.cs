using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

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

    /// <summary>A server that checks each cookie's security stamp on every request, instead of every 30 minutes.</summary>
    private WebApplicationFactory<Program> ValidatingSessionsOnEveryRequest() =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero)));
}
