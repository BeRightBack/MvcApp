namespace MvcApp.Core;

public enum BadgeCategory
{
    Profile,
    Social,
    Dating,
    Achievement,
    Special
}

public class Badge
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public BadgeCategory Category { get; set; }
    public string? CriteriaType { get; set; }
    public int CriteriaValue { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
}

public class UserBadge
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int BadgeId { get; set; }
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

    public UserDetails? User { get; set; }
    public Badge? Badge { get; set; }
}

public class PointTransaction
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int Points { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? SourceType { get; set; }
    public int? SourceId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public UserDetails? User { get; set; }
}

public class LeaderboardEntry
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? KnownAs { get; set; }
    public int TotalPoints { get; set; }
    public bool IsVerified { get; set; }
    public int BadgeCount { get; set; }
}
