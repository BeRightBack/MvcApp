using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core
{
    public class CartItem
    {
        public int Id { get; set; }

        [Required, MaxLength(450)]
        public string UserId { get; set; } = string.Empty;
        public UserDetails? User { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        [MaxLength(200)]
        public string? ProductName { get; set; }

        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
