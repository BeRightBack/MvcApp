using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core
{
    public enum OrderStatus
    {
        Pending = 0,
        Processing = 1,
        Shipped = 2,
        Delivered = 3,
        Cancelled = 4
    }

    public enum PaymentStatus
    {
        Pending = 0,
        Paid = 1,
        Failed = 2,
        Refunded = 3
    }

    public class Order
    {
        public int Id { get; set; }

        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;
        public UserDetails? User { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public decimal Subtotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Tax { get; set; }
        public decimal TotalAmount { get; set; }

        [MaxLength(200)]
        public string? ShippingName { get; set; }

        [MaxLength(500)]
        public string? ShippingAddress { get; set; }

        [MaxLength(100)]
        public string? ShippingCity { get; set; }

        [MaxLength(100)]
        public string? ShippingState { get; set; }

        [MaxLength(20)]
        public string? ShippingPostalCode { get; set; }

        [MaxLength(100)]
        public string? ShippingCountry { get; set; }

        [MaxLength(50)]
        public string? PaymentMethod { get; set; }

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public ICollection<OrderItem> Items { get; set; } = [];
    }
}
