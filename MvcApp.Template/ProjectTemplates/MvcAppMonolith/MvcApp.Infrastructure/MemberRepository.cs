using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Core.Models;
using MvcApp.Core.Pagination;
using MvcApp.Infrastructure.Helpers;

namespace MvcApp.Infrastructure;

public class MemberRepository(UserDbContext db) : IMemberRepository
{
    public async Task<MemberModel?> GetMemberAsync(string username)
    {
        var user = await db.Users
            .AsNoTracking()
            .Include(p => p.Photos)
            .Where(x => x.UserName == username)
            .SingleOrDefaultAsync();

        return EntityMapper.MapToMemberModel(user);
    }

    public async Task<PaginationList<MemberModel>> GetMembersAsync(MemberParameters userParameters)
    {
        IQueryable<UserDetails> users = db.Users
            .Include(p => p.Photos)
            .Include(u => u.UserInterestTags).ThenInclude(ut => ut.Tag)
            .AsQueryable();

        users = users.Where(u => u.UserName != userParameters.CurrentUsername);

        if (!string.IsNullOrWhiteSpace(userParameters.CurrentUsername))
        {
            var currentUserId = await db.Users
                .Where(u => u.UserName == userParameters.CurrentUsername)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(currentUserId))
            {
                IQueryable<string> blockedIds = db.UserBlocks!
                    .Where(b => b.SourceUserId == currentUserId)
                    .Select(b => b.BlockedUserId);
                IQueryable<string> blockerIds = db.UserBlocks!
                    .Where(b => b.BlockedUserId == currentUserId)
                    .Select(b => b.SourceUserId);
                users = users.Where(u => !blockedIds.Contains(u.Id) && !blockerIds.Contains(u.Id));
            }
        }

        users = users.Where(u => u.EmailConfirmed && u.IsProfileComplete);
        users = users.Where(u => !u.IsIncognito);

        users = users.Where(u => u.DateOfBirth > DateTime.MinValue);

        if (userParameters.HasPhoto == true)
            users = users.Where(u => u.Photos.Any(p => p.IsApproved));

        if (userParameters.IsOnline == true)
            users = users.Where(u => u.LastActive > DateTime.UtcNow.AddMinutes(-5));

        if (userParameters.AvailableNow == true)
            users = users.Where(u => u.IsAvailable);

        if (!string.IsNullOrWhiteSpace(userParameters.City))
            users = users.Where(u => u.City != null && u.City.ToLower().Contains(userParameters.City.ToLower()));

        if (userParameters.InterestTagIds != null && userParameters.InterestTagIds.Count > 0)
            users = users.Where(u => u.UserInterestTags.Any(ut => userParameters.InterestTagIds.Contains(ut.TagId)));

        if (!string.IsNullOrEmpty(userParameters.Gender))
        {
            users = users.Where(u => u.Gender == userParameters.Gender);
        }

        DateTime minDob = DateTime.Today.AddYears(-userParameters.MaxAge - 1);
        DateTime maxDob = DateTime.Today.AddYears(-userParameters.MinAge);

        users = users.Where(u => u.DateOfBirth >= minDob && u.DateOfBirth <= maxDob);

        var orderBy = (userParameters.OrderBy ?? "").ToLowerInvariant();
        users = orderBy switch
        {
            "created" => users.OrderByDescending(u => u.Created),
            "lastactive" => users.OrderByDescending(u => u.LastActive),
            "age" => users.OrderBy(u => u.DateOfBirth),
            "score" => users.OrderByDescending(u => u.Score),
            _ => users.OrderByDescending(u => u.LastActive)
        };

        var usersList = await users
            .Skip((userParameters.PageNumber - 1) * userParameters.PageSize)
            .Take(userParameters.PageSize)
            .ToListAsync();

        var memberModels = usersList.Select(EntityMapper.MapToMemberModel).OfType<MemberModel>().ToList();

        var count = await users.CountAsync();

        return new PaginationList<MemberModel>(
            memberModels,
            count,
            userParameters.PageNumber,
            userParameters.PageSize);
    }

    public async Task<UserDetails?> GetMemberByIdAsync(string id)
    {
        return await db.Users.FindAsync(id);
    }

    public async Task<UserDetails?> GetMemberByUsernameAsync(string username)
    {
        return await db.Users
            .Include(p => p.Photos)
            .SingleOrDefaultAsync(x => x.UserName == username);
    }

    public async Task<IEnumerable<UserDetails>> GetMembersAsync()
    {
        return await db.Users
            .AsNoTracking()
            .Include(p => p.Photos)
            .ToListAsync();
    }

    public void UpdateMember(UserDetails user)
    {
        db.Entry(user).State = EntityState.Modified;
    }
}
