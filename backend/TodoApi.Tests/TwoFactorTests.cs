using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TodoApi.Tests;

public class TwoFactorTests(TodoApiFactory factory)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Login_asks_for_a_code_it_emails()
    {
        var client = WithTwoFactor().CreateClient();
        var email = await client.RegisterConfirmedAsync(factory);

        var response = await client.LoginAsync(email);

        await AssertRefusedAsync(response, "RequiresTwoFactor");
        Assert.Single(factory.Emails.TwoFactorCodesFor(email));
    }

    [Fact]
    public async Task Emailed_code_logs_in_with_a_bearer_token()
    {
        var client = WithTwoFactor().CreateClient();
        var email = await client.RegisterConfirmedAsync(factory);
        await client.LoginAsync(email);

        var response = await client.LoginAsync(email, twoFactorCode: factory.Emails.TwoFactorCodesFor(email).Last());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Emailed_code_logs_in_with_a_cookie()
    {
        var client = WithTwoFactor().CreateClient();
        var email = await client.RegisterConfirmedAsync(factory);
        await client.LoginAsync(email, useCookies: true);

        var login = await client.LoginAsync(email, useCookies: true, twoFactorCode: factory.Emails.TwoFactorCodesFor(email).Last());
        var todos = await client.GetAsync("/api/todos", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(HttpStatusCode.OK, todos.StatusCode);
    }

    [Fact]
    public async Task Every_login_asks_for_a_code()
    {
        var client = WithTwoFactor().CreateClient();
        var email = await client.RegisterConfirmedAsync(factory);
        await client.LoginAsync(email, useCookies: true);
        await client.LoginAsync(email, useCookies: true, twoFactorCode: factory.Emails.TwoFactorCodesFor(email).Last());
        (await client.PostAsync("/api/auth/logout", null, CancellationToken)).EnsureSuccessStatusCode();

        var response = await client.LoginAsync(email, useCookies: true);

        await AssertRefusedAsync(response, "RequiresTwoFactor");
    }

    [Fact]
    public async Task Wrong_code_is_refused()
    {
        var client = WithTwoFactor().CreateClient();
        var email = await client.RegisterConfirmedAsync(factory);
        await client.LoginAsync(email);

        var response = await client.LoginAsync(email, twoFactorCode: "000000x");

        await AssertRefusedAsync(response, "Failed");
    }

    [Fact]
    public async Task Wrong_password_emails_no_code()
    {
        var client = WithTwoFactor().CreateClient();
        var email = await client.RegisterConfirmedAsync(factory);

        var response = await client.LoginAsync(email, password: "Wr0ng-password");

        await AssertRefusedAsync(response, "Failed");
        Assert.Empty(factory.Emails.TwoFactorCodesFor(email));
    }

    [Fact]
    public async Task Code_limit_holds_back_a_new_code_but_not_the_login_that_submits_one()
    {
        var client = WithTwoFactor(new() { ["RateLimits:Email:PermitLimit"] = "1" }).CreateClient();
        var email = await client.RegisterConfirmedAsync(factory);
        await client.LoginAsync(email);

        var newCode = await client.LoginAsync(email);
        var submitted = await client.LoginAsync(email, twoFactorCode: factory.Emails.TwoFactorCodesFor(email).Last());

        Assert.Equal(HttpStatusCode.TooManyRequests, newCode.StatusCode);
        Assert.NotNull(newCode.Headers.RetryAfter?.Delta);
        Assert.Single(factory.Emails.TwoFactorCodesFor(email));
        Assert.Equal(HttpStatusCode.OK, submitted.StatusCode);
    }

    private WebApplicationFactory<Program> WithTwoFactor(Dictionary<string, string?>? settings = null) =>
        factory.WithSettings(new(settings ?? []) { ["TwoFactor:Enabled"] = "true" });

    private static async Task AssertRefusedAsync(HttpResponseMessage response, string reason)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(reason, problem?.Detail);
    }
}
