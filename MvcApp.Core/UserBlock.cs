namespace MvcApp.Core;

public class UserBlock
{
    public UserDetails? SourceUser { get; set; }
    public required string SourceUserId { get; set; }
    public UserDetails? BlockedUser { get; set; }
    public required string BlockedUserId { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
}
