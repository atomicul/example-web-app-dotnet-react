using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TodoApi.Tests;

public class SpaTests(TodoApiFactory factory) : IDisposable
{
    private const string Page = "<!doctype html><title>Todo</title>";

    private readonly DirectoryInfo webRoot = Directory.CreateTempSubdirectory();

    [Theory]
    [InlineData("/")]
    [InlineData("/account")]
    public async Task Pages_get_the_spa_which_browsers_check_on_every_load(string path)
    {
        await File.WriteAllTextAsync(Path.Join(webRoot.FullName, "index.html"), Page, TestContext.Current.CancellationToken);

        var response = await ServingWebRoot().CreateClient().GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(Page, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.True(response.Headers.CacheControl?.NoCache);
    }

    public void Dispose()
    {
        webRoot.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    private WebApplicationFactory<Program> ServingWebRoot() =>
        factory.WithWebHostBuilder(builder => builder.UseWebRoot(webRoot.FullName));
}
