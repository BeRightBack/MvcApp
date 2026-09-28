using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MvcApp.Core.Abstractions;

namespace MvcApp.Services;

/// <summary>
/// Purges Serilog rows older than Logging:RetentionDays once at startup and then
/// daily, so the log table cannot grow without bound. RetentionDays = 0 disables it.
/// </summary>
public sealed class SystemLogRetentionHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<SystemLogOptions> options,
    ILogger<SystemLogRetentionHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private readonly SystemLogOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var retentionDays = _options.RetentionDays;
        if (retentionDays <= 0)
        {
            logger.LogInformation("System log retention is disabled (Logging:RetentionDays = {Days})", retentionDays);
            return;
        }

        logger.LogInformation("System log retention active: purging rows older than {Days} day(s)", retentionDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ISystemLogService>();
                var deleted = await service.DeleteOlderThanAsync(retentionDays, stoppingToken);
                if (deleted > 0)
                {
                    logger.LogInformation("System log retention removed {Deleted} rows older than {Days} days",
                        deleted, retentionDays);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "System log retention purge failed");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
