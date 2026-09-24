using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Infrastructure;
using MvcApp.Module.IPTV.Models.StoreViewModels;

namespace MvcApp.Module.IPTV.Services;

public class SubscriptionService(
    UserDbContext context,
    IRepository<Subscription> repository,
    IRepository<UserDetails> userRepository,
    IEmailSender emailSender,
    IConfiguration configuration) : ISubscriptionService
{
    public async Task CreateSubscriptionAsync(string userId, int subscriptionPlanId)
    {
        var subscriptionPlan = await context.SubscriptionPlans.Include(p => p.SubscriptionDetails).FirstOrDefaultAsync(p => p.Id == subscriptionPlanId);
        var user = await userRepository.GetFirstOrDefaultAsync(u => u.Id == userId);

        if (subscriptionPlan != null && user != null)
        {
            var subscriptionDetail = subscriptionPlan.SubscriptionDetails.FirstOrDefault();
            if (subscriptionDetail != null)
            {
                var endDate = DateTime.UtcNow;
                if (subscriptionDetail.DurationInHours.HasValue)
                {
                    endDate = endDate.AddHours(subscriptionDetail.DurationInHours.Value);
                }
                else
                {
                    endDate = endDate.AddMonths(subscriptionDetail.DurationInMonths);
                }

                var subscription = new Subscription
                {
                    UserId = userId,
                    SubscriptionPlanId = subscriptionPlanId,
                    StartDate = DateTime.UtcNow,
                    EndDate = endDate,
                    SubscriptionPlan = subscriptionPlan,
                    User = user
                };

                await repository.AddAsync(subscription);

                var adminEmail = configuration["AdminEmail"];
                var emailMessage = $"A new subscription has been added for user {user.UserName}.\n\nSubscription Plan: {subscriptionPlan.Name}\nStart Date: {subscription.StartDate}\nEnd Date: {subscription.EndDate}";
                if (!string.IsNullOrEmpty(adminEmail))
                {
                    await emailSender.SendEmailAsync(adminEmail, "New Subscription Added", emailMessage);
                }
            }
        }
    }

    public async Task CancelSubscriptionAsync(int subscriptionId)
    {
        var subscription = await context.Subscriptions.FindAsync(subscriptionId);

        if (subscription != null)
        {
            subscription.EndDate = DateTime.UtcNow;
            await repository.UpdateAsync(subscription);
        }
    }

    public async Task RenewSubscriptionAsync(int subscriptionId)
    {
        var subscription = await context.Subscriptions.FindAsync(subscriptionId);

        if (subscription != null)
        {
            var subscriptionPlan = await context.SubscriptionPlans.Include(p => p.SubscriptionDetails).FirstOrDefaultAsync(p => p.Id == subscription.SubscriptionPlanId);
            var subscriptionDetail = subscriptionPlan?.SubscriptionDetails.FirstOrDefault();
            if (subscriptionDetail != null)
            {
                subscription.EndDate = subscription.EndDate.AddMonths(subscriptionDetail.DurationInMonths);
                subscription.SubscriptionPlan = subscriptionPlan;
                await repository.UpdateAsync(subscription);
            }
        }
    }

    public async Task<SubscriptionVm?> GetSubscriptionAsync(int subscriptionId)
    {
        var subscription = await context.Subscriptions.Include(s => s.SubscriptionPlan).FirstOrDefaultAsync(s => s.Id == subscriptionId);

        if (subscription != null)
        {
            return new SubscriptionVm
            {
                Id = subscription.Id,
                UserId = subscription.UserId,
                SubscriptionPlanId = subscription.SubscriptionPlanId,
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate,
                SubscriptionPlan = subscription.SubscriptionPlan
            };
        }

        return null;
    }
}
