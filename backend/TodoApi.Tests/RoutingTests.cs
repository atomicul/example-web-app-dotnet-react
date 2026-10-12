using System.Net;

namespace TodoApi.Tests;

public class RoutingTests(TodoApiFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Health_answers_healthy()
    {
        var health = await client.GetStringAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal("Healthy", health);
    }

    [Fact]
    public async Task Unknown_api_route_is_not_found()
    {
        var response = await client.GetAsync("/api/does-not-exist", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
