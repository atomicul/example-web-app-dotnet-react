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
    public async Task Health_is_unhealthy_without_the_database()
    {
        var unreachable = factory.WithSettings(new()
        {
            ["ConnectionStrings:Default"] = "Server=127.0.0.1,1;Database=TodoApp;Connect Timeout=1;TrustServerCertificate=True",
        });

        var response = await unreachable.CreateClient().GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_api_route_is_not_found()
    {
        var response = await client.GetAsync("/api/does-not-exist", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
