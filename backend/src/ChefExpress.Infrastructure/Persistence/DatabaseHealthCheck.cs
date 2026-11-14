using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ChefExpress.Infrastructure.Persistence;

internal sealed class DatabaseHealthCheck(AppDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await db.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy("MariaDB reachable")
            : HealthCheckResult.Unhealthy("MariaDB unreachable");
}
