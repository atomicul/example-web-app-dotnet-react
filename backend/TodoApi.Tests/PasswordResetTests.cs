using System.Net;
using System.Web;

namespace TodoApi.Tests;

public class PasswordResetTests(TodoApiFactory factory)
{
    private const string NewPassword = "N3w-passw0rd";

    [Fact]
    public async Task Reset_link_opens_the_spa_page()
    {
        var client = factory.CreateClient();
        var email = await client.RegisterConfirmedAsync(factory);

        (await client.ForgotPasswordAsync(email)).EnsureSuccessStatusCode();

        var link = factory.Emails.ResetLinkFor(email);
        var query = HttpUtility.ParseQueryString(link.Query);
        Assert.Equal($"{TodoApiFactory.SpaUrl}/reset-password", link.GetLeftPart(UriPartial.Path));
        Assert.Equal(email, query["email"]);
        Assert.NotNull(query["code"]);
    }

    [Fact]
    public async Task Reset_link_replaces_the_password()
    {
        var client = factory.CreateClient();
        var email = await client.RegisterConfirmedAsync(factory);
        (await client.ForgotPasswordAsync(email)).EnsureSuccessStatusCode();

        (await client.ResetPasswordAsync(factory.Emails.ResetLinkFor(email), NewPassword)).EnsureSuccessStatusCode();
        var withOldPassword = await client.LoginAsync(email);
        var withNewPassword = await client.LoginAsync(email, password: NewPassword);

        Assert.Equal(HttpStatusCode.Unauthorized, withOldPassword.StatusCode);
        Assert.Equal(HttpStatusCode.OK, withNewPassword.StatusCode);
    }

    [Fact]
    public async Task Reset_link_works_once()
    {
        var client = factory.CreateClient();
        var email = await client.RegisterConfirmedAsync(factory);
        (await client.ForgotPasswordAsync(email)).EnsureSuccessStatusCode();
        var link = factory.Emails.ResetLinkFor(email);
        (await client.ResetPasswordAsync(link, NewPassword)).EnsureSuccessStatusCode();

        var response = await client.ResetPasswordAsync(link, "An0ther-passw0rd");

        var (_, issues) = await response.ReadValidationProblemAsync();
        Assert.Equal("InvalidToken", Assert.Single(issues).Code);
    }
}
