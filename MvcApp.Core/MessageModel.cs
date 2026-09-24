using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core.Models;

public class MessageModel
{
    public int Id { get; set; }

    public string SenderId { get; set; } = string.Empty;

    [MaxLength(50)]
    public string SenderUsername { get; set; } = string.Empty;

    public string SenderPhotoUrl { get; set; } = string.Empty;

    public string RecipientId { get; set; } = string.Empty;

    [MaxLength(50)]
    public string RecipientUsername { get; set; } = string.Empty;

    public string RecipientPhotoUrl { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    public DateTime? DateRead { get; set; }

    public DateTime MessageSent { get; set; }

    public bool SenderDeleted { get; set; }

    public bool RecipientDeleted { get; set; }
}
