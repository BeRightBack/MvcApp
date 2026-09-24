using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Core.Models;
using MvcApp.Infrastructure.Helpers;

namespace MvcApp.Infrastructure;

public class UserBlockRepository(UserDbContext db) : IUserBlockRepository
{
    public async Task<UserBlock?> GetBlockAsync(string sourceUserId, string blockedUserId)
    {
        return await db.UserBlocks!.FindAsync(sourceUserId, blockedUserId);
    }

    public async Task<HashSet<string>> GetBlockedUserIdsAsync(string userId)
    {
        return new HashSet<string>(await db.UserBlocks!
            .Where(b => b.SourceUserId == userId)
            .Select(b => b.BlockedUserId)
            .ToListAsync());
    }

    public async Task<HashSet<string>> GetBlockersOfAsync(string userId)
    {
        return new HashSet<string>(await db.UserBlocks!
            .Where(b => b.BlockedUserId == userId)
            .Select(b => b.SourceUserId)
            .ToListAsync());
    }

    public async Task AddAsync(UserBlock block)
    {
        var existing = await db.UserBlocks!.FindAsync(block.SourceUserId, block.BlockedUserId);
        if (existing == null)
        {
            await db.UserBlocks.AddAsync(block);
        }
    }

    public async Task RemoveAsync(string sourceUserId, string blockedUserId)
    {
        var existing = await db.UserBlocks!.FindAsync(sourceUserId, blockedUserId);
        if (existing != null)
        {
            db.UserBlocks.Remove(existing);
        }
    }

    public async Task<bool> IsBlockedAsync(string userIdA, string userIdB)
    {
        return await db.UserBlocks!.AnyAsync(b =>
            (b.SourceUserId == userIdA && b.BlockedUserId == userIdB) ||
            (b.SourceUserId == userIdB && b.BlockedUserId == userIdA));
    }

    public async Task<List<LikedMemberModel>> GetBlockedUsersAsync(string userId)
    {
        var users = await db.UserBlocks!
            .AsNoTracking()
            .Include(b => b.BlockedUser)
            .ThenInclude(u => u!.Photos)
            .Where(b => b.SourceUserId == userId)
            .Select(b => b.BlockedUser!)
            .OrderByDescending(u => u.LastActive)
            .ToListAsync();

        return users
            .Select(EntityMapper.MapToMemberModel)
            .OfType<MemberModel>()
            .Select(m => EntityMapper.MapToLikedMemberModel(m, false, false))
            .ToList();
    }
}
