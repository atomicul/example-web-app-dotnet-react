using System.Net;
using System.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApi.Data;

namespace TodoApi.Tests;

public class AuthTests(TodoApiFactory factory)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Anonymous_request_is_unauthorized()
    {
        var response = await factory.CreateClient().GetAsync("/api/todos", CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Confirmation_link_opens_the_spa_page()
    {
        var email = TestUsers.NewEmail();

        await factory.CreateClient().RegisterAsync(email);

        var link = factory.Emails.ConfirmationLinkFor(email);
        var query = HttpUtility.ParseQueryString(link.Query);
        Assert.Equal($"{TodoApiFactory.SpaUrl}/confirm-email", link.GetLeftPart(UriPartial.Path));
        Assert.NotNull(query["userId"]);
        Assert.NotNull(query["code"]);
    }

    [Fact]
    public async Task Login_requires_a_confirmed_email()
    {
        var client = factory.CreateClient();
        var email = TestUsers.NewEmail();
        await client.RegisterAsync(email);

        var response = await client.LoginAsync(email);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Bearer_token_authorizes_requests()
    {
        var client = await factory.CreateUserClientAsync();

        var response = await client.GetAsync("/api/todos", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Cookie_session_lasts_until_logout()
    {
        var client = factory.CreateClient();
        var email = await client.RegisterConfirmedAsync(factory);

        (await client.LoginAsync(email, useCookies: true)).EnsureSuccessStatusCode();
        var duringSession = await client.GetAsync("/api/todos", CancellationToken);
        (await client.PostAsync("/api/auth/logout", null, CancellationToken)).EnsureSuccessStatusCode();
        var afterLogout = await client.GetAsync("/api/todos", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, duringSession.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task Keys_protecting_sessions_are_kept_in_the_database()
    {
        await factory.CreateUserClientAsync();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.True(await db.DataProtectionKeys.AnyAsync(CancellationToken));
    }
}
