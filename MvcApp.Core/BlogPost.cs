using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core
{
    public class BlogPost
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(250)]
        public string Slug { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Excerpt { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? FeaturedImagePath { get; set; }

        public string? CreatedById { get; set; }

        public UserDetails? CreatedBy { get; set; }

        [MaxLength(50)]
        public string CreatedByUsername { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? PublishedAt { get; set; }

        public bool IsPublished { get; set; }

        public bool IsFeatured { get; set; }

        public int ViewCount { get; set; }

        public int? CategoryId { get; set; }

        public BlogCategory? Category { get; set; }

        public ICollection<BlogPostTag> PostTags { get; set; } = [];

        public ICollection<BlogComment> Comments { get; set; } = [];
    }
}
