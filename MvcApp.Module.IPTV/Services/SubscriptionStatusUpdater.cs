using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MvcApp.Infrastructure;

namespace MvcApp.Module.IPTV.Services;

public class SubscriptionStatusUpdater : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriptionStatusUpdater> _logger;
    private Timer? _timer;

    public SubscriptionStatusUpdater(IServiceProvider serviceProvider, ILogger<SubscriptionStatusUpdater> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Subscription Status Updater Service is starting.");
        _timer = new Timer(UpdateSubscriptionStatuses, null, TimeSpan.FromMinutes(1), TimeSpan.FromHours(1));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Subscription Status Updater Service is stopping.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }

    public void UpdateSubscriptionStatuses(object? state = null)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<UserDbContext>();
            var subscriptions = context.Subscriptions.Where(s => s.Status != "expired" && s.EndDate <= DateTime.UtcNow).ToList();

            foreach (var subscription in subscriptions)
            {
                subscription.Status = "expired";
            }

            context.SaveChanges();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update subscription statuses.");
        }
    }
}
