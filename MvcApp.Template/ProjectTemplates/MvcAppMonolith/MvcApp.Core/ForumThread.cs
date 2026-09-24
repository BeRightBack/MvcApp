using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core
{
    public class ForumThread
    {
        public int Id { get; set; }

        public int ForumId { get; set; }

        public Forum? Forum { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? CreatedById { get; set; }

        public UserDetails? CreatedBy { get; set; }

        [MaxLength(50)]
        public string CreatedByUsername { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsPinned { get; set; }

        public bool IsLocked { get; set; }

        public bool IsDeleted { get; set; }

        public int ViewCount { get; set; }

        public int ReplyCount { get; set; }

        public DateTime? LastPostAt { get; set; }

        public string? LastPostByUserId { get; set; }

        [MaxLength(50)]
        public string? LastPostByUsername { get; set; }

        public ICollection<ForumPost> Posts { get; set; } = [];
    }
}
