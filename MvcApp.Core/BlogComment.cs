using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core
{
    public class BlogComment
    {
        public int Id { get; set; }

        public int PostId { get; set; }

        public BlogPost? Post { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        public string? CreatedById { get; set; }

        public UserDetails? CreatedBy { get; set; }

        [MaxLength(50)]
        public string? CreatedByUsername { get; set; }

        [MaxLength(100)]
        public string? GuestName { get; set; }

        [MaxLength(200)]
        public string? GuestEmail { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsApproved { get; set; }

        public bool IsDeleted { get; set; }
    }
}
