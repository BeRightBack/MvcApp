using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core
{
    public class ProductCategory
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(150)]
        public string Slug { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public int? ParentCategoryId { get; set; }
        public ProductCategory? ParentCategory { get; set; }
        public ICollection<ProductCategory>? SubCategories { get; set; }

        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<Product>? Products { get; set; }
    }
}
