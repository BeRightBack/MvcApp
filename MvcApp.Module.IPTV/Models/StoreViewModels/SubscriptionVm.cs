using MvcApp.Core;

namespace MvcApp.Module.IPTV.Models.StoreViewModels;

public class SubscriptionVm
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public int SubscriptionPlanId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SubscriptionPlan? SubscriptionPlan { get; set; }
}
