using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TodoApi.Data;

namespace TodoApi;

/// <summary>
/// Reports the API healthy once it can open its database and the database has every migration of this release. A
/// database it can't open throws, so the health check service logs why, while <c>/health</c> only answers
/// <c>503 Unhealthy</c>.
/// </summary>
public class DatabaseHealthCheck(AppDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        await db.Database.OpenConnectionAsync(ct);
        var pending = (await db.Database.GetPendingMigrationsAsync(ct)).ToList();
        return pending.Count == 0
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy($"The database lacks this release's migrations: {string.Join(", ", pending)}");
    }
}
