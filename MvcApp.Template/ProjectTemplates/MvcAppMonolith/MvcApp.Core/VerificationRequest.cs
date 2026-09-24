using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core;

public class VerificationRequest
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public UserDetails? User { get; set; }

    [Required]
    public string Filename { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? DisplayNumber { get; set; }

    public VerificationStatus Status { get; set; } = VerificationStatus.Pending;

    [MaxLength(500)]
    public string? AdminNotes { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }

    [MaxLength(100)]
    public string? ReviewedBy { get; set; }
}
