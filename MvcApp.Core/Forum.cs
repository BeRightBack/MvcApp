using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core
{
    public class Forum
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public int CategoryId { get; set; }

        public ForumCategory? Category { get; set; }

        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int ThreadCount { get; set; }

        public int PostCount { get; set; }

        public DateTime? LastPostAt { get; set; }

        public string? LastPostByUserId { get; set; }

        public string? LastPostByUsername { get; set; }

        public ICollection<ForumThread> Threads { get; set; } = [];
    }
}
