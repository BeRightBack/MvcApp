using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Core.Models;
using MvcApp.Core.Pagination;
using MvcApp.Infrastructure.Helpers;

namespace MvcApp.Infrastructure;

public class LikesRepository(UserDbContext db) : ILikesRepository
{
    public async Task<UserLike?> GetUserLikeAsync(string sourceUserId, string likedUserId)
    {
        return await db.Likes!.FindAsync(sourceUserId, likedUserId);
    }

    public async Task<PaginationList<LikedMemberModel>> GetUserLikesAsync(LikesParameters likesParameters)
    {
        IQueryable<UserDetails> users;

        if (likesParameters.Predicate.Equals("liked", StringComparison.OrdinalIgnoreCase))
        {
            users = db.Likes!
                .Include(like => like.LikedUser)
                    .ThenInclude(user => user!.Photos)
                .Where(like => like.SourceUserId == likesParameters.UserId)
                .Select(like => like.LikedUser!);
        }
        else if (likesParameters.Predicate.Equals("likedby", StringComparison.OrdinalIgnoreCase))
        {
            users = db.Likes!
                .Include(like => like.SourceUser)
                    .ThenInclude(user => user!.Photos)
                .Where(like => like.LikedUserId == likesParameters.UserId)
                .Select(like => like.SourceUser!);
        }
        else
        {
            users = db.Users.Where(u => false);
        }

        var blockedIds = db.UserBlocks!.Where(b => b.SourceUserId == likesParameters.UserId).Select(b => b.BlockedUserId);
        var blockerIds = db.UserBlocks!.Where(b => b.BlockedUserId == likesParameters.UserId).Select(b => b.SourceUserId);

        users = users.Where(u => !blockedIds.Contains(u.Id) && !blockerIds.Contains(u.Id));

        if (!string.IsNullOrWhiteSpace(likesParameters.Gender) &&
            !likesParameters.Gender.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            users = users.Where(u => u.Gender == likesParameters.Gender);
        }

        users = (likesParameters.OrderBy ?? string.Empty).ToLowerInvariant() switch
        {
            "created" => users.OrderByDescending(u => u.Created),
            "age" => users.OrderBy(u => u.DateOfBirth),
            "name" => users.OrderBy(u => u.KnownAs).ThenBy(u => u.UserName),
            _ => users.OrderByDescending(u => u.LastActive)
        };

        var count = await users.CountAsync();

        var usersList = await users
            .Skip((likesParameters.PageNumber - 1) * likesParameters.PageSize)
            .Take(likesParameters.PageSize)
            .ToListAsync();

        var memberModels = usersList.Select(EntityMapper.MapToMemberModel).OfType<MemberModel>().ToList();

        var likedIds = await GetLikedUserIdsAsync(likesParameters.UserId);
        var likedByIds = await GetLikedByUserIdsAsync(likesParameters.UserId);
        var isLikedPredicate = likesParameters.Predicate.Equals("liked", StringComparison.OrdinalIgnoreCase);

        var likedMembers = memberModels.Select(m => EntityMapper.MapToLikedMemberModel(
            m,
            isLikedByYou: isLikedPredicate || likedIds.Contains(m.Id),
            isMutual: isLikedPredicate ? likedByIds.Contains(m.Id) : likedIds.Contains(m.Id))).ToList();

        return new PaginationList<LikedMemberModel>(
            likedMembers,
            count,
            likesParameters.PageNumber,
            likesParameters.PageSize);
    }

    public async Task<UserDetails?> GetUserWithLikesAsync(string userId)
    {
        return await db.Users
            .Include(x => x.LikedUsers)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == userId);
    }

    public async Task<(int Liked, int LikedBy)> GetLikeCountsAsync(string userId)
    {
        var blockedIds = db.UserBlocks!.Where(b => b.SourceUserId == userId).Select(b => b.BlockedUserId);
        var blockerIds = db.UserBlocks!.Where(b => b.BlockedUserId == userId).Select(b => b.SourceUserId);

        var liked = await db.Likes!
            .CountAsync(l => l.SourceUserId == userId
                && !blockedIds.Contains(l.LikedUserId)
                && !blockerIds.Contains(l.LikedUserId));
        var likedBy = await db.Likes!
            .CountAsync(l => l.LikedUserId == userId
                && !blockedIds.Contains(l.SourceUserId)
                && !blockerIds.Contains(l.SourceUserId));
        return (liked, likedBy);
    }

    public async Task<HashSet<string>> GetLikedUserIdsAsync(string userId)
    {
        return new HashSet<string>(await db.Likes!
            .Where(l => l.SourceUserId == userId)
            .Select(l => l.LikedUserId)
            .ToListAsync());
    }

    public async Task<HashSet<string>> GetLikedByUserIdsAsync(string userId)
    {
        return new HashSet<string>(await db.Likes!
            .Where(l => l.LikedUserId == userId)
            .Select(l => l.SourceUserId)
            .ToListAsync());
    }

    public async Task<List<LikedMemberModel>> GetMatchesAsync(string userId)
    {
        IQueryable<string> likedIds = db.Likes!
            .Where(l => l.SourceUserId == userId)
            .Select(l => l.LikedUserId);
        IQueryable<string> likedByIds = db.Likes!
            .Where(l => l.LikedUserId == userId)
            .Select(l => l.SourceUserId);
        IQueryable<string> blockedIds = db.UserBlocks!
            .Where(b => b.SourceUserId == userId)
            .Select(b => b.BlockedUserId);
        IQueryable<string> blockerIds = db.UserBlocks!
            .Where(b => b.BlockedUserId == userId)
            .Select(b => b.SourceUserId);

        var usersList = await db.Users
            .AsNoTracking()
            .Include(u => u.Photos)
            .Where(u => likedIds.Contains(u.Id)
                && likedByIds.Contains(u.Id)
                && !blockedIds.Contains(u.Id)
                && !blockerIds.Contains(u.Id))
            .OrderByDescending(u => u.LastActive)
            .ToListAsync();

        return usersList
            .Select(EntityMapper.MapToMemberModel)
            .OfType<MemberModel>()
            .Select(m => EntityMapper.MapToLikedMemberModel(m, true, true))
            .ToList();
    }
}
