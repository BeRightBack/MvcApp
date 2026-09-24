namespace MvcApp.Core;

public class UserNotificationSettings
{
    public required string UserId { get; set; }
    public UserDetails? User { get; set; }

    public bool EmailOnLike { get; set; } = true;
    public bool EmailOnMatch { get; set; } = true;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
