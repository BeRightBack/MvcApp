using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        [Required, MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ProductImage { get; set; }

        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
    }
}
