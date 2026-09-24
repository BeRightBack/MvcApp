using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core;

public class Notification
{
    public int Id { get; set; }

    public required string UserId { get; set; }
    public UserDetails? User { get; set; }

    [MaxLength(20)]
    public string Type { get; set; } = string.Empty;

    [MaxLength(255)]
    public required string ActorId { get; set; }

    [MaxLength(50)]
    public string ActorUsername { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
