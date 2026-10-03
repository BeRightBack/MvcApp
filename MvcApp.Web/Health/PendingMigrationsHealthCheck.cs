using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MvcApp.Infrastructure;
using MvcApp.Localization;

namespace MvcApp.Web.Health;

/// <summary>
/// Reports a pending-migration state as Unhealthy. A reachable database is not a migrated
/// database: the app returns 500 for every request when the schema is behind, while a
/// connectivity-only check still reports Healthy. This is what makes readiness meaningful.
/// </summary>
public sealed class PendingMigrationsHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();

        var contexts = new DbContext[]
        {
            scope.ServiceProvider.GetRequiredService<UserDbContext>(),
            scope.ServiceProvider.GetRequiredService<LocalizationDbContext>()
        };

        var pending = new List<string>();

        try
        {
            foreach (var dbContext in contexts)
            {
                var migrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
                pending.AddRange(migrations.Select(migration => $"{dbContext.GetType().Name}:{migration}"));
            }
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Could not determine migration state: {ex.Message}", ex);
        }

        if (pending.Count > 0)
        {
            return HealthCheckResult.Unhealthy($"Pending migrations: {string.Join(", ", pending)}");
        }

        return HealthCheckResult.Healthy("No pending migrations.");
    }
}
