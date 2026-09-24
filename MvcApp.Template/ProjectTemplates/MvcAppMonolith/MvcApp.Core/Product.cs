using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core
{
    public class Product
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(250)]
        public string Slug { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ShortDescription { get; set; }

        public string? Description { get; set; }

        [Required]
        public decimal Price { get; set; }

        public decimal? ComparePrice { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        [MaxLength(2000)]
        public string? AdditionalImages { get; set; }

        public int? CategoryId { get; set; }
        public ProductCategory? Category { get; set; }

        public int StockQuantity { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; }
        public int SortOrder { get; set; }

        public string? CreatedById { get; set; }
        public UserDetails? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
