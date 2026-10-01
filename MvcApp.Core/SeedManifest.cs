using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core;

public class SeedManifest
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string PackName { get; set; } = string.Empty;

    [Required]
    [MaxLength(400)]
    public string EntityType { get; set; } = string.Empty;

    [Required]
    [MaxLength(512)]
    public string EntityKey { get; set; } = string.Empty;

    public DateTime AppliedAt { get; set; }

    [MaxLength(255)]
    public string? AppliedBy { get; set; }
}
