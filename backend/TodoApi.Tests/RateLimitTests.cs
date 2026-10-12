using System.Net;

namespace TodoApi.Tests;

public class RateLimitTests(TodoApiFactory factory)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Api_limit_rejects_requests_over_the_permit_limit()
    {
        var client = factory.WithSettings(new() { ["RateLimits:Api:PermitLimit"] = "2" }).CreateClient();

        await client.GetAsync("/api/does-not-exist", CancellationToken);
        await client.GetAsync("/api/does-not-exist", CancellationToken);
        var rejected = await client.GetAsync("/api/does-not-exist", CancellationToken);

        AssertRejected(rejected);
    }

    [Fact]
    public async Task Email_limit_rejects_a_second_email()
    {
        var client = factory.WithSettings(new() { ["RateLimits:Email:PermitLimit"] = "1" }).CreateClient();

        var first = await client.RegisterAsync(TestUsers.NewEmail());
        var second = await client.RegisterAsync(TestUsers.NewEmail());

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        AssertRejected(second);
    }

    [Fact]
    public async Task Health_is_not_rate_limited()
    {
        var client = factory.WithSettings(new() { ["RateLimits:Api:PermitLimit"] = "1" }).CreateClient();

        var responses = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => client.GetAsync("/health", CancellationToken)));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
    }

    private static void AssertRejected(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.NotNull(response.Headers.RetryAfter?.Delta);
    }
}
