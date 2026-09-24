namespace MvcApp.Core
{
    public class Payment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SubscriptionPlanId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal PaymentAmount { get; set; }
    }
}
