using MvcApp.Module.IPTV.Models.StoreViewModels;

namespace MvcApp.Module.IPTV.Services;

public interface ISubscriptionService
{
    Task CreateSubscriptionAsync(string userId, int subscriptionPlanId);
    Task CancelSubscriptionAsync(int subscriptionId);
    Task RenewSubscriptionAsync(int subscriptionId);
    Task<SubscriptionVm?> GetSubscriptionAsync(int subscriptionId);
}
