using System.ComponentModel.DataAnnotations;
using MvcApp.Core;

namespace MvcApp.Module.Video;

public class VideoRoom
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>How many simultaneous broadcast spots the room supports (max 10).</summary>
    [Range(1, 10)]
    public int MaxSpots { get; set; } = 10;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? CreatedById { get; set; }

    public UserDetails? CreatedBy { get; set; }

    public ICollection<VideoRoomMessage> Messages { get; set; } = [];
}
