namespace MvcApp.Core;

public class UserInterestTag
{
    public string UserId { get; set; } = string.Empty;
    public UserDetails? User { get; set; }

    public int TagId { get; set; }
    public InterestTag? Tag { get; set; }
}
