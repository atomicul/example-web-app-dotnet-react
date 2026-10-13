using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TodoApi.Tests;

public class AcmeChallengeTests(TodoApiFactory factory) : IDisposable
{
    private const string Token = "Zm9v-token_1";

    private readonly DirectoryInfo webRoot = Directory.CreateTempSubdirectory();

    [Fact]
    public async Task Serves_the_challenge_files_under_the_web_root()
    {
        var challenges = Directory.CreateDirectory(Path.Join(webRoot.FullName, ".well-known", "acme-challenge"));
        await File.WriteAllTextAsync(Path.Join(challenges.FullName, Token), $"{Token}.key",
            TestContext.Current.CancellationToken);

        var challenge = await ServingWebRoot().CreateClient().GetStringAsync($"/.well-known/acme-challenge/{Token}",
            TestContext.Current.CancellationToken);

        Assert.Equal($"{Token}.key", challenge);
    }

    [Fact]
    public async Task Unknown_challenge_is_not_found()
    {
        var response = await ServingWebRoot().CreateClient().GetAsync($"/.well-known/acme-challenge/{Token}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public void Dispose()
    {
        webRoot.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    private WebApplicationFactory<Program> ServingWebRoot() =>
        factory.WithSettings(new() { ["AcmeChallenge:WebRoot"] = webRoot.FullName });
}
