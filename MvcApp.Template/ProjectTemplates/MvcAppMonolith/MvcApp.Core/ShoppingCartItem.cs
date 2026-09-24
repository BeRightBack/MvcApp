namespace MvcApp.Core
{
    public class ShoppingCartItem
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public int SubscriptionPlanId { get; set; }
        public int SubscriptionDetailId { get; set; }
        public int Quantity { get; set; }
        public SubscriptionPlan? SubscriptionPlan { get; set; }
        public SubscriptionDetail? SubscriptionDetail { get; set; }
    }
}
