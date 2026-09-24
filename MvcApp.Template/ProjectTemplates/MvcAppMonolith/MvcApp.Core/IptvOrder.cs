namespace MvcApp.Core
{
    public class IptvOrder
    {
        public string? Id { get; set; }
        public Guid UserId { get; set; }
        public DateTime OrderDate { get; set; }
        public string? Status { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
