using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Abstractions;

namespace MvcApp.Infrastructure
{
    public class SuperLikeService(UserDbContext db) : ISuperLikeService
    {
        public async Task<bool> CanSuperLikeTodayAsync(string userId)
        {
            var used = await GetSuperLikesUsedTodayAsync(userId);
            var limit = await GetSuperLikeLimitAsync(userId);
            return used < limit;
        }

        public async Task<int> GetSuperLikesUsedTodayAsync(string userId)
        {
            var today = DateTime.UtcNow.Date;
            return await db.SuperLikes
                .CountAsync(s => s.SourceUserId == userId && s.CreatedAt >= today);
        }

        public async Task<int> GetSuperLikeLimitAsync(string userId)
        {
            var user = await db.Users.FindAsync(userId);
            return user?.IsVip == true ? 5 : 1;
        }

        public async Task<bool> HasSuperLikedAsync(string sourceUserId, string targetUserId)
        {
            return await db.SuperLikes
                .AnyAsync(s => s.SourceUserId == sourceUserId && s.TargetUserId == targetUserId);
        }

        public async Task<bool> SendSuperLikeAsync(string sourceUserId, string targetUserId)
        {
            if (sourceUserId == targetUserId) return false;
            if (!await CanSuperLikeTodayAsync(sourceUserId)) return false;
            if (await HasSuperLikedAsync(sourceUserId, targetUserId)) return false;

            var blocked = await db.UserBlocks!
                .AnyAsync(b => (b.SourceUserId == sourceUserId && b.BlockedUserId == targetUserId)
                            || (b.SourceUserId == targetUserId && b.BlockedUserId == sourceUserId));
            if (blocked) return false;

            db.SuperLikes.Add(new SuperLike
            {
                SourceUserId = sourceUserId,
                TargetUserId = targetUserId,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsBoostActiveAsync(string userId)
        {
            var now = DateTime.UtcNow;
            return await db.UserBoosts
                .AnyAsync(b => b.UserId == userId && b.IsActive && b.StartDate <= now && b.EndDate > now);
        }

        public async Task<int> GetBoostsUsedTodayAsync(string userId)
        {
            var cutoff = DateTime.UtcNow.AddHours(-24);
            return await db.UserBoosts
                .CountAsync(b => b.UserId == userId && b.StartDate >= cutoff);
        }

        public async Task<int> GetBoostLimitAsync(string userId)
        {
            var user = await db.Users.FindAsync(userId);
            return user?.IsVip == true ? 2 : 1;
        }

        public async Task<bool> ActivateBoostAsync(string userId)
        {
            if (await IsBoostActiveAsync(userId)) return false;

            var used = await GetBoostsUsedTodayAsync(userId);
            var limit = await GetBoostLimitAsync(userId);
            if (used >= limit) return false;

            var now = DateTime.UtcNow;
            db.UserBoosts.Add(new UserBoost
            {
                UserId = userId,
                StartDate = now,
                EndDate = now.AddMinutes(30),
                IsActive = true
            });
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<List<string>> GetBoostedUserIdsAsync()
        {
            var now = DateTime.UtcNow;
            return await db.UserBoosts
                .Where(b => b.IsActive && b.StartDate <= now && b.EndDate > now)
                .Select(b => b.UserId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<HashSet<string>> GetSuperLikedUserIdsAsync(string sourceUserId)
        {
            return await db.SuperLikes
                .Where(s => s.SourceUserId == sourceUserId)
                .Select(s => s.TargetUserId)
                .ToHashSetAsync();
        }

        public async Task<HashSet<string>> GetSuperLikedByUserIdsAsync(string targetUserId)
        {
            return await db.SuperLikes
                .Where(s => s.TargetUserId == targetUserId)
                .Select(s => s.SourceUserId)
                .ToHashSetAsync();
        }
    }
}
