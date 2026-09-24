namespace MvcApp.Core;

public class UserBan
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string UserId { get; set; }
    public UserDetails? User { get; set; }

    public required string Reason { get; set; }

    public required string BannedById { get; set; }
    public UserDetails? BannedBy { get; set; }

    public DateTime BannedAt { get; set; } = DateTime.UtcNow;

    // null = indefinite ban
    public DateTime? BannedUntil { get; set; }

    public DateTime? RevokedAt { get; set; }
    public string? RevokedById { get; set; }
    public UserDetails? RevokedBy { get; set; }
}
