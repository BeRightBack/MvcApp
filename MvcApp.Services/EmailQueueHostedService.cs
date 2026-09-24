using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MvcApp.Services;

public sealed class EmailQueueHostedService(
    IEmailQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<EmailQueueHostedService> logger) : BackgroundService
{
    private const int MaxAttempts = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            EmailQueueItem item;
            try
            {
                item = await queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await ProcessAsync(item, stoppingToken);
        }
    }

    private async Task ProcessAsync(EmailQueueItem item, CancellationToken cancellationToken)
    {
        Exception? lastError = null;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<SmtpEmailSender>();
                await sender.SendAsync(item, cancellationToken);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                lastError = ex;
                if (attempt < MaxAttempts)
                {
                    var delay = TimeSpan.FromSeconds(2 * attempt);
                    logger.LogWarning(ex, "SMTP send failed (attempt {Attempt}/{MaxAttempts}) to {To}; retrying in {DelaySeconds}s",
                        attempt, MaxAttempts, item.To, delay.TotalSeconds);
                    try
                    {
                        await Task.Delay(delay, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }
        }

        logger.LogError(lastError, "SMTP send failed permanently to {To}: {Subject}", item.To, item.Subject);
    }
}
