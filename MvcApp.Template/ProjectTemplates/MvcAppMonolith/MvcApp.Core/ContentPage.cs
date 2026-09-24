using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core;

public class ContentPage
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    public string? CreatedById { get; set; }

    public UserDetails? CreatedBy { get; set; }

    [MaxLength(50)]
    public string CreatedByUsername { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? PublishedAt { get; set; }
}
