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
    public async Task Health_is_unhealthy_until_the_database_is_migrated_without_saying_why()
    {
        var unmigrated = factory.WithSettings(new()
        {
            ["ConnectionStrings:Default"] = await factory.CreateEmptyDatabaseAsync("Unmigrated"),
        });

        var response = await unmigrated.CreateClient().GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Unknown_api_route_is_not_found()
    {
        var response = await client.GetAsync("/api/does-not-exist", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
