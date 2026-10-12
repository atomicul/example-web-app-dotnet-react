using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TodoApi.Tests;

/// <summary>
/// Checks which IP the rate limit counts against, with an API limit of one request per client: a second request is
/// rejected only when it counts as coming from the same client.
/// </summary>
public class TrustedProxyTests(TodoApiFactory factory)
{
    private const string Proxy = "203.0.113.10";

    [Fact]
    public async Task Untrusted_proxy_cannot_choose_the_client_ip()
    {
        var api = LimitedToOneRequest(new());

        await SendAsync(api, Proxy, forwardedFor: "198.51.100.1");
        var second = await SendAsync(api, Proxy, forwardedFor: "198.51.100.2");

        Assert.Equal(StatusCodes.Status429TooManyRequests, second.Response.StatusCode);
    }

    [Theory]
    [InlineData("127.0.0.1", null, null)]
    [InlineData(Proxy, "TrustedProxies:Addresses:0", Proxy)]
    [InlineData(Proxy, "TrustedProxies:Networks:0", "203.0.113.0/24")]
    public async Task Trusted_proxy_forwards_the_client_ip(string proxy, string? setting, string? value)
    {
        var api = LimitedToOneRequest(setting is null ? new() : new() { [setting] = value });

        var first = await SendAsync(api, proxy, forwardedFor: "198.51.100.1");
        var otherClient = await SendAsync(api, proxy, forwardedFor: "198.51.100.2");
        var sameClient = await SendAsync(api, proxy, forwardedFor: "198.51.100.1");

        Assert.Equal(StatusCodes.Status404NotFound, first.Response.StatusCode);
        Assert.Equal(StatusCodes.Status404NotFound, otherClient.Response.StatusCode);
        Assert.Equal(StatusCodes.Status429TooManyRequests, sameClient.Response.StatusCode);
    }

    private WebApplicationFactory<Program> LimitedToOneRequest(Dictionary<string, string?> settings) =>
        factory.WithSettings(new(settings) { ["RateLimits:Api:PermitLimit"] = "1" });

    private static Task<HttpContext> SendAsync(WebApplicationFactory<Program> api, string remoteIp, string forwardedFor) =>
        api.Server.SendAsync(context =>
        {
            context.Request.Path = "/api/does-not-exist";
            context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
            context.Request.Headers["X-Forwarded-For"] = forwardedFor;
        }, TestContext.Current.CancellationToken);
}
