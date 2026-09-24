using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Abstractions;

namespace MvcApp.Infrastructure;

public class GamificationService(UserDbContext db) : IGamificationService
{
    public async Task AwardPointsAsync(string userId, int points, string reason, string? sourceType = null, int? sourceId = null)
    {
        if (points <= 0) return;

        var user = await db.Users.FindAsync(userId);
        if (user == null) return;

        var tx = new PointTransaction
        {
            UserId = userId,
            Points = points,
            Reason = reason,
            SourceType = sourceType,
            SourceId = sourceId,
            CreatedAt = DateTime.UtcNow
        };
        db.PointTransactions.Add(tx);

        user.TotalPoints += points;
        await db.SaveChangesAsync();

        await CheckAndAwardBadgesAsync(userId);
    }

    public async Task<List<PointTransaction>> GetRecentTransactionsAsync(string userId, int count = 20)
    {
        return await db.PointTransactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<int> GetTotalPointsAsync(string userId)
    {
        var user = await db.Users.FindAsync(userId);
        return user?.TotalPoints ?? 0;
    }

    public async Task<List<Badge>> GetAllBadgesAsync()
    {
        return await db.Badges
            .Where(b => b.IsEnabled)
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Name)
            .ToListAsync();
    }

    public async Task<List<UserBadge>> GetUserBadgesAsync(string userId)
    {
        return await db.UserBadges
            .Where(ub => ub.UserId == userId)
            .Include(ub => ub.Badge)
            .OrderByDescending(ub => ub.EarnedAt)
            .ToListAsync();
    }

    public async Task<bool> HasBadgeAsync(string userId, int badgeId)
    {
        return await db.UserBadges.AnyAsync(ub => ub.UserId == userId && ub.BadgeId == badgeId);
    }

    public async Task CheckAndAwardBadgesAsync(string userId)
    {
        var badges = await db.Badges.Where(b => b.IsEnabled).ToListAsync();
        var userBadges = await db.UserBadges.Where(ub => ub.UserId == userId).Select(ub => ub.BadgeId).ToListAsync();
        var user = await db.Users.FindAsync(userId);
        if (user == null) return;

        var toAward = new List<UserBadge>();

        foreach (var badge in badges)
        {
            if (userBadges.Contains(badge.Id)) continue;

            var earned = badge.CriteriaType switch
            {
                "TotalPoints" => user.TotalPoints >= badge.CriteriaValue,
                "PhotosUploaded" => await db.Photos!.CountAsync(p => p.UserDetailsId == userId) >= badge.CriteriaValue,
                "MessagesSent" => await db.Messages!.CountAsync(m => m.SenderId == userId) >= badge.CriteriaValue,
                "MatchesReceived" => await db.Likes!.CountAsync(l => l.LikedUserId == userId) >= badge.CriteriaValue,
                "SuperLikesSent" => await db.SuperLikes.CountAsync(s => s.SourceUserId == userId) >= badge.CriteriaValue,
                "SuperLikesReceived" => await db.SuperLikes.CountAsync(s => s.TargetUserId == userId) >= badge.CriteriaValue,
                "BoostsUsed" => await db.UserBoosts.CountAsync(b => b.UserId == userId) >= badge.CriteriaValue,
                "ProfileComplete" => user.IsProfileComplete,
                "Verified" => user.IsVerified,
                "DaysActive" => (DateTime.UtcNow - (user.Created)).TotalDays >= badge.CriteriaValue,
                _ => false
            };

            if (earned)
            {
                toAward.Add(new UserBadge
                {
                    UserId = userId,
                    BadgeId = badge.Id,
                    EarnedAt = DateTime.UtcNow
                });
            }
        }

        if (toAward.Count > 0)
        {
            db.UserBadges.AddRange(toAward);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<(string UserId, string UserName, int TotalPoints, int BadgeCount)>> GetLeaderboardAsync(int top = 50)
    {
        return await db.Users
            .Where(u => u.TotalPoints > 0)
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.TotalPoints,
                BadgeCount = db.UserBadges.Count(ub => ub.UserId == u.Id)
            })
            .OrderByDescending(x => x.TotalPoints)
            .Take(top)
            .ToListAsync()
            .ContinueWith(t => t.Result.Select(x => (x.Id, x.UserName ?? "Unknown", x.TotalPoints, x.BadgeCount)).ToList());
    }

    public async Task SeedBadgesAsync()
    {
        if (await db.Badges.AnyAsync()) return;

        var badges = new List<Badge>
        {
            // Profile
            new() { Name = "Profile Pro", Description = "Complete your profile", Icon = "bx bx-user", Category = BadgeCategory.Profile, CriteriaType = "ProfileComplete", CriteriaValue = 1, SortOrder = 1 },
            new() { Name = "Photo Star", Description = "Upload 5 photos", Icon = "bx bx-camera", Category = BadgeCategory.Profile, CriteriaType = "PhotosUploaded", CriteriaValue = 5, SortOrder = 2 },
            new() { Name = "Verified", Description = "Get verified", Icon = "bx bx-check-shield", Category = BadgeCategory.Profile, CriteriaType = "Verified", CriteriaValue = 1, SortOrder = 3 },

            // Social
            new() { Name = "First Message", Description = "Send your first message", Icon = "bx bx-envelope", Category = BadgeCategory.Social, CriteriaType = "MessagesSent", CriteriaValue = 1, SortOrder = 10 },
            new() { Name = "Social Butterfly", Description = "Send 10 messages", Icon = "bx bx-chat", Category = BadgeCategory.Social, CriteriaType = "MessagesSent", CriteriaValue = 10, SortOrder = 11 },
            new() { Name = "Conversationalist", Description = "Send 50 messages", Icon = "bx bx-conversation", Category = BadgeCategory.Social, CriteriaType = "MessagesSent", CriteriaValue = 50, SortOrder = 12 },

            // Dating
            new() { Name = "Heart Seeker", Description = "Receive your first like", Icon = "bx bx-heart", Category = BadgeCategory.Dating, CriteriaType = "MatchesReceived", CriteriaValue = 1, SortOrder = 20 },
            new() { Name = "Popular", Description = "Receive 10 likes", Icon = "bx bx-heart-circle", Category = BadgeCategory.Dating, CriteriaType = "MatchesReceived", CriteriaValue = 10, SortOrder = 21 },
            new() { Name = "Heartthrob", Description = "Receive 50 likes", Icon = "bx bx-heart", Category = BadgeCategory.Dating, CriteriaType = "MatchesReceived", CriteriaValue = 50, SortOrder = 22 },
            new() { Name = "Super Connector", Description = "Send 10 Super Likes", Icon = "bx bx-star", Category = BadgeCategory.Dating, CriteriaType = "SuperLikesSent", CriteriaValue = 10, SortOrder = 23 },
            new() { Name = "Superstar", Description = "Receive 10 Super Likes", Icon = "bx bxs-star", Category = BadgeCategory.Dating, CriteriaType = "SuperLikesReceived", CriteriaValue = 10, SortOrder = 24 },
            new() { Name = "Boosted", Description = "Use your first Boost", Icon = "bx bx-bolt-circle", Category = BadgeCategory.Dating, CriteriaType = "BoostsUsed", CriteriaValue = 1, SortOrder = 25 },
            new() { Name = "On Fire", Description = "Use 5 Boosts", Icon = "bx bx-flame", Category = BadgeCategory.Dating, CriteriaType = "BoostsUsed", CriteriaValue = 5, SortOrder = 26 },

            // Achievement
            new() { Name = "Getting Started", Description = "Earn 100 points", Icon = "bx bx-star", Category = BadgeCategory.Achievement, CriteriaType = "TotalPoints", CriteriaValue = 100, SortOrder = 30 },
            new() { Name = "Rising Star", Description = "Earn 500 points", Icon = "bx bx-star-half", Category = BadgeCategory.Achievement, CriteriaType = "TotalPoints", CriteriaValue = 500, SortOrder = 31 },
            new() { Name = "Champion", Description = "Earn 1000 points", Icon = "bx bx-trophy", Category = BadgeCategory.Achievement, CriteriaType = "TotalPoints", CriteriaValue = 1000, SortOrder = 32 },
            new() { Name = "Veteran", Description = "Active for 30 days", Icon = "bx bx-calendar", Category = BadgeCategory.Achievement, CriteriaType = "DaysActive", CriteriaValue = 30, SortOrder = 33 },
        };

        db.Badges.AddRange(badges);
        await db.SaveChangesAsync();
    }
}
