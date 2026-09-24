using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core;

public enum RSVPStatus
{
    Going = 0,
    Maybe = 1,
    Interested = 2
}

public class EventRSVP
{
    public int Id { get; set; }

    public int EventId { get; set; }
    public Event? Event { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public UserDetails? User { get; set; }

    public RSVPStatus Status { get; set; } = RSVPStatus.Going;

    public DateTime RSVPDate { get; set; } = DateTime.UtcNow;
}
