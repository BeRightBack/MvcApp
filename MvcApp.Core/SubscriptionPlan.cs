using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MvcApp.Core
{
    public class SubscriptionPlan
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? DescriptionShort { get; set; }
        public string? Description { get; set; }

        public ICollection<SubscriptionDetail> SubscriptionDetails { get; set; } = [];
    }

    public class SubscriptionDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public decimal Price { get; set; }
        public required string Description { get; set; }
        public int DurationInMonths { get; set; }
        public int? DurationInHours { get; set; }
        public int SubscriptionPlanId { get; set; }
        public SubscriptionPlan? SubscriptionPlan { get; set; }
    }
}
