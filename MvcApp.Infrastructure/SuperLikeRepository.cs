using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Abstractions;

namespace MvcApp.Infrastructure
{
    public class SuperLikeRepository(UserDbContext db) : ISuperLikeRepository
    {
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
