using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core;

public class EventCategory
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Icon { get; set; } = "bx-calendar-event";
}
