using System.ComponentModel.DataAnnotations;
using MvcApp.Core;

namespace MvcApp.Module.Video;

public class VideoRoomMessage
{
    public int Id { get; set; }

    public int VideoRoomId { get; set; }

    public VideoRoom? VideoRoom { get; set; }

    public string? SenderId { get; set; }

    public UserDetails? Sender { get; set; }

    [MaxLength(50)]
    public string? SenderUsername { get; set; }

    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    public DateTime MessageSent { get; set; } = DateTime.UtcNow;
}
