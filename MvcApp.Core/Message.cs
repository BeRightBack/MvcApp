using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core
{
    public class Message
    {
        public int Id { get; set; }

        public required string SenderId { get; set; }

        [MaxLength(50)]
        public string? SenderUsername { get; set; }

        public UserDetails? Sender { get; set; }

        public required string RecipientId { get; set; }

        [MaxLength(50)]
        public string? RecipientUsername { get; set; }

        public UserDetails? Recipient { get; set; }

        [MaxLength(2000)]
        public string? Content { get; set; }

        public DateTime? DateRead { get; set; }

        public DateTime MessageSent { get; set; } = DateTime.UtcNow;

        public bool SenderDeleted { get; set; } = false;

        public bool RecipientDeleted { get; set; } = false;
    }
}
