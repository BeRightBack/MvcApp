using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core;

public class Event
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public DateTime EventDate { get; set; }

    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Country { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public int? CategoryId { get; set; }
    public EventCategory? Category { get; set; }

    [Required]
    public string CreatorId { get; set; } = string.Empty;
    public UserDetails? Creator { get; set; }

    public int MaxAttendees { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<EventRSVP> RSVPs { get; set; } = [];
}
