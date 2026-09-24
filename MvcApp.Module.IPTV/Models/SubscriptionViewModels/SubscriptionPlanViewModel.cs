namespace MvcApp.Module.IPTV.Models.SubscriptionViewModels;

public class SubscriptionPlanViewModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? DescriptionShort { get; set; }
    public required string Description { get; set; }
    public IList<SubscriptionDetailViewModel> SubscriptionDetails { get; set; } = new List<SubscriptionDetailViewModel>();
}

public class SubscriptionDetailViewModel
{
    public int Id { get; set; }
    public required string Description { get; set; }
    public decimal Price { get; set; }
    public int DurationInMonths { get; set; }
    public int? DurationInHours { get; set; } = 0;

    public string? PriceFormatted { get; set; }

    public int SubscriptionPlanId { get; set; }
    public SubscriptionPlanViewModel? SubscriptionPlan { get; set; }
}
