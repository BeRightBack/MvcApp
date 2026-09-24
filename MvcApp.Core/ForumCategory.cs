using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core
{
    public class ForumCategory
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Forum> Forums { get; set; } = [];
    }
}
