using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core
{
    public class ForumPost
    {
        public int Id { get; set; }

        public int ThreadId { get; set; }

        public ForumThread? Thread { get; set; }

        public string? CreatedById { get; set; }

        public UserDetails? CreatedBy { get; set; }

        [MaxLength(50)]
        public string CreatedByUsername { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public string? UpdatedById { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? DeletedAt { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;
    }
}
