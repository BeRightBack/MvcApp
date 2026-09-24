using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core
{
    public class ChatRoomMessage
    {
        public int Id { get; set; }

        public int ChatRoomId { get; set; }

        public ChatRoom? ChatRoom { get; set; }

        public string? SenderId { get; set; }

        public UserDetails? Sender { get; set; }

        [MaxLength(50)]
        public string? SenderUsername { get; set; }

        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        public DateTime MessageSent { get; set; } = DateTime.UtcNow;
    }
}
