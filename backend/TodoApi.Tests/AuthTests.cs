using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
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
    public async Task Registration_errors_are_coded_as_identity_keys_them()
    {
        var response = await factory.CreateClient().RegisterAsync(TestUsers.NewEmail(), password: "Password!");

        var (problem, issues) = await response.ReadValidationProblemAsync();

        Assert.Equal(problem.Errors["PasswordRequiresDigit"], issues.Where(issue => issue.Code == "PasswordRequiresDigit").Select(issue => issue.Detail));
        Assert.All(issues, issue => Assert.Null(issue.Pointer));
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
    public async Task Login_is_locked_out_after_repeated_failures()
    {
        var client = factory.CreateClient();
        var email = await client.RegisterConfirmedAsync(factory);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await client.LoginAsync(email, password: "Wr0ng-password");
        }

        var response = await client.LoginAsync(email);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("LockedOut", problem?.Detail);
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

    [Fact]
    public async Task Keys_are_encrypted_with_the_configured_certificate()
    {
        const string password = "Certificate-passw0rd";
        var certificatePath = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(certificatePath, CreateCertificatePfx(password), CancellationToken);
            await using var server = factory.WithWebHostBuilder(builder => builder
                .UseSetting("DataProtection:Certificate:Path", certificatePath)
                .UseSetting("DataProtection:Certificate:Password", password));
            var keyManager = server.Services.GetRequiredService<IKeyManager>();
            var activation = DateTimeOffset.UtcNow.AddYears(1);

            var key = keyManager.CreateNewKey(activation, activation.AddDays(90));

            using var scope = server.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stored = await db.DataProtectionKeys.SingleAsync(row => row.FriendlyName == $"key-{key.KeyId}", CancellationToken);
            Assert.Contains("EncryptedData", stored.Xml);
            Assert.DoesNotContain("<value>", stored.Xml);
            Assert.NotNull(keyManager.GetAllKeys().Single(read => read.KeyId == key.KeyId).Descriptor);
        }
        finally
        {
            File.Delete(certificatePath);
        }
    }

    private static byte[] CreateCertificatePfx(string password)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=TodoApi.Tests", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1));
        return certificate.Export(X509ContentType.Pfx, password);
    }
}
