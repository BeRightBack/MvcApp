namespace MvcApp.Core.Abstractions;

public interface IGamificationService
{
    Task AwardPointsAsync(string userId, int points, string reason, string? sourceType = null, int? sourceId = null);
    Task<List<PointTransaction>> GetRecentTransactionsAsync(string userId, int count = 20);
    Task<int> GetTotalPointsAsync(string userId);
    Task<List<Badge>> GetAllBadgesAsync();
    Task<List<UserBadge>> GetUserBadgesAsync(string userId);
    Task<bool> HasBadgeAsync(string userId, int badgeId);
    Task CheckAndAwardBadgesAsync(string userId);
    Task<List<(string UserId, string UserName, int TotalPoints, int BadgeCount)>> GetLeaderboardAsync(int top = 50);
    Task SeedBadgesAsync();
}
