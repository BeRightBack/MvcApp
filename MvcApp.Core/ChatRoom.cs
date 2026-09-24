using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core
{
    public class ChatRoom
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? CreatedById { get; set; }

        public UserDetails? CreatedBy { get; set; }

        public ICollection<ChatRoomMessage> Messages { get; set; } = [];
    }
}
