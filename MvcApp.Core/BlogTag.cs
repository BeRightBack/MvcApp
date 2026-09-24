using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core
{
    public class BlogTag
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Slug { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<BlogPostTag> PostTags { get; set; } = [];
    }
}
