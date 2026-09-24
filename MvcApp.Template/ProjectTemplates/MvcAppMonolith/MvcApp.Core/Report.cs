using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core;

public class Report
{
    public int Id { get; set; }

    [Required]
    public string ReporterId { get; set; } = string.Empty;

    public UserDetails? Reporter { get; set; }

    [Required]
    public string ReportedUserId { get; set; } = string.Empty;

    public UserDetails? ReportedUser { get; set; }

    public ReportReason Reason { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    [MaxLength(500)]
    public string? AdminNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }

    [MaxLength(100)]
    public string? ReviewedBy { get; set; }
}
