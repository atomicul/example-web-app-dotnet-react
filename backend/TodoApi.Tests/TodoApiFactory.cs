using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using TodoApi.Data;

[assembly: AssemblyFixture(typeof(TodoApi.Tests.TodoApiFactory))]

namespace TodoApi.Tests;

/// <summary>
/// Runs the API against a migrated SQL Server in Docker, shared by every test in the run. Tests stay isolated by
/// registering their own users, since all data belongs to a user. Emails are captured instead of logged.
/// </summary>
public class TodoApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string SpaUrl = "https://spa.test";

    private readonly MsSqlContainer database = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest").Build();

    public CapturingEmailSender Emails { get; } = new();

    public async ValueTask InitializeAsync()
    {
        await database.StartAsync();
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", database.GetConnectionString());
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SpaUrl"] = SpaUrl,
            ["RateLimits:Api:PermitLimit"] = "100000",
            ["RateLimits:Email:PermitLimit"] = "100000",
        }));
        builder.ConfigureTestServices(services =>
            services.AddKeyedSingleton<IEmailSender<IdentityUser>>(SpaLinkEmailSender.TransportKey, Emails));
    }

    /// <summary>
    /// A separate server over the same database with the given settings on top. Each has its own rate limiter, so its
    /// requests don't count against other tests.
    /// </summary>
    public WebApplicationFactory<Program> WithSettings(Dictionary<string, string?> settings) =>
        WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(settings)));

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await database.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
