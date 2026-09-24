using MvcApp.Core;

namespace MvcApp.Module.IPTV.Models.HomeViewModels;

public class PlansViewModel
{
    public IEnumerable<SubscriptionPlan> SubscriptionPlans { get; set; } = new List<SubscriptionPlan>();
}
