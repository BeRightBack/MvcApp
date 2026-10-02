using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MvcApp.Core.Abstractions;

namespace MvcApp.Services;

/// <summary>
/// Purges Serilog rows older than Logging:RetentionDays at startup and then daily, so the log
/// table cannot grow without bound. RetentionDays = 0 disables it.
///
/// The value is read from the SystemSettings table on every cycle, falling back to configuration.
/// That makes retention editable at Admin -&gt; Settings -&gt; Logging without a redeploy, which is
/// the point: retention is a non-secret operational knob, not deployment configuration.
///
/// Two things this previously got wrong. It captured the configured value once in the constructor,
/// so editing appsettings.json had no effect until a restart. And when retention was 0 it
/// <c>return</c>ed out of ExecuteAsync, ending the background service permanently — turning
/// auto-purge off and then on again required an application restart. It now re-reads and
/// re-evaluates on each cycle instead of exiting.
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
        var firstCycle = true;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();

                // A fresh install has no log database yet; create it before the first
                // purge so the MySQL sink starts receiving rows.
                var initializer = scope.ServiceProvider.GetRequiredService<SystemLogDatabaseInitializer>();
                await initializer.EnsureExistsAsync(
                    scope.ServiceProvider.GetRequiredService<IConfiguration>()
                        .GetConnectionString("SerilogLogs"),
                    stoppingToken);

                var retentionDays = await ResolveRetentionDaysAsync(scope.ServiceProvider, stoppingToken);

                if (retentionDays <= 0)
                {
                    if (firstCycle)
                        logger.LogInformation("System log retention is disabled (Logging:RetentionDays = {Days})", retentionDays);
                }
                else
                {
                    if (firstCycle)
                        logger.LogInformation("System log retention active: purging rows older than {Days} day(s)", retentionDays);

                    var service = scope.ServiceProvider.GetRequiredService<ISystemLogService>();
                    var deleted = await service.DeleteOlderThanAsync(retentionDays, stoppingToken);
                    if (deleted > 0)
                    {
                        logger.LogInformation("System log retention removed {Deleted} rows older than {Days} days",
                            deleted, retentionDays);
                    }
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

            firstCycle = false;

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

    /// <summary>
    /// The SystemSettings row wins over configuration so an admin can change retention without
    /// editing appsettings.json. A row that exists but is blank or unparseable falls through to
    /// the configured value rather than silently disabling the purge.
    /// </summary>
    private async Task<int> ResolveRetentionDaysAsync(IServiceProvider scopedServices, CancellationToken ct)
    {
        try
        {
            var settings = scopedServices.GetRequiredService<ISettingsService>();
            var fromDb = await settings.GetAsync("Logging.RetentionDays");
            if (!string.IsNullOrWhiteSpace(fromDb) && int.TryParse(fromDb.Trim(), out var days))
                return days;
        }
        catch (Exception ex)
        {
            // The settings table is unavailable on a first run against a schema-less database.
            // Configuration is the fallback, so a transient failure must not disable the purge.
            logger.LogDebug(ex, "Could not read Logging.RetentionDays from settings; using configuration.");
        }

        return _options.RetentionDays;
    }
}
