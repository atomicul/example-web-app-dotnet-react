using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TodoApi.Tests;

/// <summary>
/// The test server ignores Kestrel's endpoints, so <c>HTTPS_PORT</c> tells the redirect which port HTTPS is on. The
/// host isn't localhost, which never gets HSTS.
/// </summary>
public class HttpsTests(TodoApiFactory factory)
{
    private const string Host = "todo.test";

    [Fact]
    public async Task Http_redirects_to_https_when_kestrel_serves_https()
    {
        var response = await SendAsync(ServingHttps(), $"http://{Host}/health");

        Assert.Equal(HttpStatusCode.TemporaryRedirect, response.StatusCode);
        Assert.Equal(new Uri($"https://{Host}/health"), response.Headers.Location);
    }

    [Fact]
    public async Task Https_asks_browsers_to_keep_using_https()
    {
        var response = await SendAsync(ServingHttps(), $"https://{Host}/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public async Task Http_is_served_without_an_https_endpoint()
    {
        var response = await SendAsync(factory, $"http://{Host}/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Strict-Transport-Security"));
    }

    private WebApplicationFactory<Program> ServingHttps() =>
        factory.WithSettings(new()
        {
            ["Kestrel:Endpoints:Https:Url"] = "https://*:443",
            ["HTTPS_PORT"] = "443",
        });

    private static Task<HttpResponseMessage> SendAsync(WebApplicationFactory<Program> api, string url) =>
        api.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false })
            .GetAsync(url, TestContext.Current.CancellationToken);
}
