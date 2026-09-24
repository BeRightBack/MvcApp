using MvcApp.Module.IPTV.Models.StoreViewModels;

namespace MvcApp.Module.IPTV.Models.AdminViewModels;

public class IptvDashboardViewModel
{
    public int TotalSubscriptions { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int PendingSubscriptions { get; set; }
    public int ProcessingSubscriptions { get; set; }
    public int ExpiredSubscriptions { get; set; }
    public int TotalPlans { get; set; }
    public List<SubscriptionVm> RecentSubscriptions { get; set; } = new();
}
