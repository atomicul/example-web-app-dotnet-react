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
    public async Task Each_kind_of_email_has_its_own_limit()
    {
        var client = factory.WithSettings(new() { ["RateLimits:Email:PermitLimit"] = "1" }).CreateClient();
        var email = TestUsers.NewEmail();

        var registered = await client.RegisterAsync(email);
        var resetAsked = await client.ForgotPasswordAsync(email);

        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
        Assert.Equal(HttpStatusCode.OK, resetAsked.StatusCode);
    }

    [Fact]
    public async Task Refused_request_does_not_count_towards_the_email_limit()
    {
        var client = factory.WithSettings(new() { ["RateLimits:Email:PermitLimit"] = "1" }).CreateClient();

        var refused = await client.RegisterAsync(TestUsers.NewEmail(), password: "short");
        var registered = await client.RegisterAsync(TestUsers.NewEmail());

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
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
