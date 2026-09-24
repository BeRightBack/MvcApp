using MvcApp.Core;

namespace MvcApp.Module.IPTV.Models.StoreViewModels;

public class StoreViewModel
{
    public IEnumerable<SubscriptionPlan> SubscriptionPlans { get; set; } = new List<SubscriptionPlan>();
    public int CartItemCount { get; set; }
}
