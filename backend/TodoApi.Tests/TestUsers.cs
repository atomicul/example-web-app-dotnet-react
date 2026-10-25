using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Web;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity.Data;

namespace TodoApi.Tests;

/// <summary>Registers, confirms and logs in users through the API, as a client would.</summary>
public static class TestUsers
{
    public const string Password = "Passw0rd!";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public static string NewEmail() => $"{Guid.NewGuid()}@test.local";

    /// <summary>A client authorized with the bearer token of a new, confirmed user.</summary>
    public static async Task<HttpClient> CreateUserClientAsync(this TodoApiFactory factory)
    {
        var client = factory.CreateClient();
        var email = await client.RegisterConfirmedAsync(factory);
        var login = await client.LoginAsync(email);
        var token = await login.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<AccessTokenResponse>(CancellationToken);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        return client;
    }

    /// <summary>Registers a new user and confirms their email with the link the API sent, returning the email.</summary>
    public static async Task<string> RegisterConfirmedAsync(this HttpClient client, TodoApiFactory factory)
    {
        var email = NewEmail();
        (await client.RegisterAsync(email)).EnsureSuccessStatusCode();
        (await client.ConfirmEmailAsync(factory.Emails.ConfirmationLinkFor(email))).EnsureSuccessStatusCode();
        return email;
    }

    public static Task<HttpResponseMessage> RegisterAsync(this HttpClient client, string email, string password = Password) =>
        client.PostAsJsonAsync("/api/auth/register", new RegisterRequest { Email = email, Password = password }, CancellationToken);

    /// <summary>Calls the API's confirmation endpoint with the query of a link to the SPA's confirmation page.</summary>
    public static Task<HttpResponseMessage> ConfirmEmailAsync(this HttpClient client, Uri confirmationLink) =>
        client.GetAsync($"/api/auth/confirmEmail{confirmationLink.Query}", CancellationToken);

    /// <summary>Changes the signed-in user's email or password through Identity's <c>POST /manage/info</c>.</summary>
    public static Task<HttpResponseMessage> ChangeInfoAsync(this HttpClient client, InfoRequest request) =>
        client.PostAsJsonAsync("/api/auth/manage/info", request, CancellationToken);

    public static Task<HttpResponseMessage> ForgotPasswordAsync(this HttpClient client, string email) =>
        client.PostAsJsonAsync("/api/auth/forgotPassword", new ForgotPasswordRequest { Email = email }, CancellationToken);

    /// <summary>Sets a new password with the email and code of a link to the SPA's password reset page.</summary>
    public static Task<HttpResponseMessage> ResetPasswordAsync(this HttpClient client, Uri resetLink, string newPassword)
    {
        var query = HttpUtility.ParseQueryString(resetLink.Query);
        var request = new ResetPasswordRequest { Email = query["email"]!, ResetCode = query["code"]!, NewPassword = newPassword };
        return client.PostAsJsonAsync("/api/auth/resetPassword", request, CancellationToken);
    }

    public static Task<HttpResponseMessage> LoginAsync(
        this HttpClient client,
        string email,
        bool useCookies = false,
        string password = Password,
        string? twoFactorCode = null,
        string? twoFactorRecoveryCode = null) =>
        client.PostAsJsonAsync(
            $"/api/auth/login?useCookies={useCookies}",
            new LoginRequest
            {
                Email = email,
                Password = password,
                TwoFactorCode = twoFactorCode,
                TwoFactorRecoveryCode = twoFactorRecoveryCode,
            },
            CancellationToken);

    /// <summary>Sets up or turns off the signed-in user's authenticator app through Identity's <c>POST /manage/2fa</c>.</summary>
    public static Task<HttpResponseMessage> SetUpTwoFactorAsync(this HttpClient client, TwoFactorRequest request) =>
        client.PostAsJsonAsync("/api/auth/manage/2fa", request, CancellationToken);
}
