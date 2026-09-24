using System.ComponentModel.DataAnnotations;

namespace MvcApp.Core;

public class VideoUpload
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string Filename { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? ThumbnailFilename { get; set; }

    public int DurationSeconds { get; set; }

    public VideoCategory Category { get; set; } = VideoCategory.General;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public int ViewCount { get; set; } = 0;

    public bool IsApproved { get; set; } = false;

    public bool IsDeleted { get; set; } = false;

    public required string UserId { get; set; }

    public UserDetails? User { get; set; }
}
