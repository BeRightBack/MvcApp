namespace MvcApp.Core;

public class ProfileAppreciation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public required string SourceUserId { get; set; }
    public UserDetails? SourceUser { get; set; }
    public required string TargetUserId { get; set; }
    public UserDetails? TargetUser { get; set; }
    public int Value { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
