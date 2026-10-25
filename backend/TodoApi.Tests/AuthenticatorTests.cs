using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TodoApi.Auth;

namespace TodoApi.Tests;

public class AuthenticatorTests(TodoApiFactory factory)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Method_is_the_email_at_first()
    {
        var client = WithTwoFactor().CreateClient();
        await SignInWithEmailedCodeAsync(client, await client.RegisterConfirmedAsync(factory));

        var method = await GetMethodAsync(client);

        Assert.Equal(new TwoFactorMethodResponse(IsAvailable: true, TwoFactorMethod.Email), method);
    }

    [Fact]
    public async Task Method_is_unavailable_while_two_factor_is_off()
    {
        var client = await factory.CreateUserClientAsync();

        var method = await GetMethodAsync(client);

        Assert.Equal(new TwoFactorMethodResponse(IsAvailable: false, TwoFactorMethod.Email), method);
    }

    [Fact]
    public async Task App_cannot_be_turned_on_while_two_factor_is_off()
    {
        var client = await factory.CreateUserClientAsync();
        var key = await ResetKeyAsync(client);

        var response = await client.SetUpTwoFactorAsync(new TwoFactorRequest { Enable = true, TwoFactorCode = Totp.CodeFor(key) });

        var (_, issues) = await response.ReadValidationProblemAsync();
        Assert.Equal("TwoFactorUnavailable", Assert.Single(issues).Code);
        Assert.Equal(TwoFactorMethod.Email, (await GetMethodAsync(client)).Method);
    }

    [Fact]
    public async Task Setting_up_the_app_keeps_the_session_that_did_it()
    {
        var client = WithTwoFactor().CreateClient();
        await SignInWithEmailedCodeAsync(client, await client.RegisterConfirmedAsync(factory));
        var key = await ResetKeyAsync(client);

        var setup = await ReadAsync<TwoFactorResponse>(
            await client.SetUpTwoFactorAsync(new TwoFactorRequest { Enable = true, TwoFactorCode = Totp.CodeFor(key) }));
        var method = await GetMethodAsync(client);

        Assert.Equal(10, setup.RecoveryCodes?.Length);
        Assert.Equal(TwoFactorMethod.Authenticator, method.Method);
    }

    [Fact]
    public async Task Wrong_app_code_does_not_set_the_app_up()
    {
        var client = WithTwoFactor().CreateClient();
        await SignInWithEmailedCodeAsync(client, await client.RegisterConfirmedAsync(factory));
        await ResetKeyAsync(client);

        var response = await client.SetUpTwoFactorAsync(new TwoFactorRequest { Enable = true, TwoFactorCode = "000000x" });

        var (_, issues) = await response.ReadValidationProblemAsync();
        Assert.Equal("InvalidTwoFactorCode", Assert.Single(issues).Code);
        Assert.Equal(TwoFactorMethod.Email, (await GetMethodAsync(client)).Method);
    }

    [Fact]
    public async Task Login_names_the_email_as_the_way_to_finish_it_by_default()
    {
        var client = WithTwoFactor().CreateClient();
        var email = await client.RegisterConfirmedAsync(factory);

        var response = await client.LoginAsync(email);

        Assert.Equal("Email", await ReadMethodNamedAsync(response));
    }

    [Fact]
    public async Task App_code_logs_in_without_emailing_one()
    {
        var client = WithTwoFactor().CreateClient();
        var (email, key, _) = await RegisterWithAppAsync(client);
        var emailedCodes = factory.Emails.TwoFactorCodesFor(email).Count();

        var asked = await client.LoginAsync(email, useCookies: true);
        var login = await client.LoginAsync(email, useCookies: true, twoFactorCode: Totp.CodeFor(key));

        Assert.Equal("Authenticator", await ReadMethodNamedAsync(asked));
        Assert.Equal(emailedCodes, factory.Emails.TwoFactorCodesFor(email).Count());
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task App_code_is_refused_until_the_app_is_set_up()
    {
        var client = WithTwoFactor().CreateClient();
        var email = await client.RegisterConfirmedAsync(factory);
        await SignInWithEmailedCodeAsync(client, email);
        var key = await ResetKeyAsync(client);
        await LogoutAsync(client);

        await client.LoginAsync(email, useCookies: true);
        var login = await client.LoginAsync(email, useCookies: true, twoFactorCode: Totp.CodeFor(key));

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Recovery_code_logs_in_once()
    {
        var client = WithTwoFactor().CreateClient();
        var (email, _, recoveryCodes) = await RegisterWithAppAsync(client);

        var first = await client.LoginAsync(email, useCookies: true, twoFactorRecoveryCode: recoveryCodes[0]);
        await LogoutAsync(client);
        var second = await client.LoginAsync(email, useCookies: true, twoFactorRecoveryCode: recoveryCodes[0]);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
    }

    [Fact]
    public async Task Turning_the_app_off_goes_back_to_emailed_codes()
    {
        var client = WithTwoFactor().CreateClient();
        var (email, key, _) = await RegisterWithAppAsync(client);
        await client.LoginAsync(email, useCookies: true);
        await client.LoginAsync(email, useCookies: true, twoFactorCode: Totp.CodeFor(key));

        (await client.SetUpTwoFactorAsync(new TwoFactorRequest { Enable = false, ResetSharedKey = true })).EnsureSuccessStatusCode();
        var method = await GetMethodAsync(client);
        await LogoutAsync(client);
        var login = await client.LoginAsync(email, useCookies: true);

        Assert.Equal(TwoFactorMethod.Email, method.Method);
        Assert.Equal("Email", await ReadMethodNamedAsync(login));
        Assert.Equal(HttpStatusCode.OK, (await client.LoginAsync(
            email, useCookies: true, twoFactorCode: factory.Emails.TwoFactorCodesFor(email).Last())).StatusCode);
    }

    /// <summary>
    /// A server whose logins ask for a second factor, and which checks each cookie's security stamp on every request
    /// instead of every 30 minutes, so a session that a change of the app would end fails at once.
    /// </summary>
    private WebApplicationFactory<Program> WithTwoFactor() =>
        factory.WithSettings(new() { ["TwoFactor:Enabled"] = "true" })
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero)));

    /// <summary>Registers a user and sets up an authenticator app for them, leaving the client signed out.</summary>
    private async Task<(string Email, string SharedKey, string[] RecoveryCodes)> RegisterWithAppAsync(HttpClient client)
    {
        var email = await client.RegisterConfirmedAsync(factory);
        await SignInWithEmailedCodeAsync(client, email);
        var key = await ResetKeyAsync(client);
        var setup = await ReadAsync<TwoFactorResponse>(
            await client.SetUpTwoFactorAsync(new TwoFactorRequest { Enable = true, TwoFactorCode = Totp.CodeFor(key) }));
        await LogoutAsync(client);
        return (email, key, setup.RecoveryCodes!);
    }

    private async Task SignInWithEmailedCodeAsync(HttpClient client, string email)
    {
        await client.LoginAsync(email, useCookies: true);
        (await client.LoginAsync(email, useCookies: true, twoFactorCode: factory.Emails.TwoFactorCodesFor(email).Last()))
            .EnsureSuccessStatusCode();
    }

    /// <summary>Gives the signed-in user a new authenticator key, as the SPA does before showing it, and returns it.</summary>
    private static async Task<string> ResetKeyAsync(HttpClient client) =>
        (await ReadAsync<TwoFactorResponse>(await client.SetUpTwoFactorAsync(new TwoFactorRequest { ResetSharedKey = true })))
            .SharedKey;

    private static async Task LogoutAsync(HttpClient client) =>
        (await client.PostAsync("/api/auth/logout", null, CancellationToken)).EnsureSuccessStatusCode();

    private static async Task<TwoFactorMethodResponse> GetMethodAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<TwoFactorMethodResponse>("/api/auth/manage/twoFactorMethod", CancellationToken))!;

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        (await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<T>(CancellationToken))!;

    /// <summary>The way to finish the login that a <c>RequiresTwoFactor</c> refusal names.</summary>
    private static async Task<string?> ReadMethodNamedAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("RequiresTwoFactor", problem?.Detail);
        return ((JsonElement)problem!.Extensions[TwoFactorMethods.ProblemMember]!).GetString();
    }
}
