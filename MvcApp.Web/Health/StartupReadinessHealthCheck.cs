using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MvcApp.Web.Health;

/// <summary>
/// Readiness gate for startup seeding. Unhealthy until seeding has finished, and unhealthy with
/// the reason if it failed — so a deploy gate or load balancer does not send traffic to an
/// instance whose database is unusable.
/// </summary>
public sealed class StartupReadinessHealthCheck(StartupState state) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (state.SeedingError is { } error)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy($"Startup seeding failed: {error}"));
        }

        if (!state.SeedingCompleted)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("Startup seeding has not completed yet."));
        }

        return Task.FromResult(HealthCheckResult.Healthy("Startup seeding completed."));
    }
}
