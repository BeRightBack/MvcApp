using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Abstractions;

namespace MvcApp.Infrastructure;

public class BanService(UserDbContext db) : IBanService
{
    public Task<UserBan?> GetActiveBanAsync(string userId)
    {
        var now = DateTime.UtcNow;
        return db.UserBans
            .Where(b => b.UserId == userId &&
                        b.RevokedAt == null &&
                        (b.BannedUntil == null || b.BannedUntil > now))
            .OrderByDescending(b => b.BannedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsBannedAsync(string userId) =>
        await GetActiveBanAsync(userId) != null;

    public async Task<IReadOnlyList<UserBan>> GetBansAsync()
    {
        return await db.UserBans
            .Include(b => b.User)
            .Include(b => b.BannedBy)
            .Include(b => b.RevokedBy)
            .OrderByDescending(b => b.BannedAt)
            .ToListAsync();
    }

    public async Task<UserBan?> BanUserAsync(string userId, string reason, string bannedById, TimeSpan? duration)
    {
        var ban = new UserBan
        {
            UserId = userId,
            Reason = reason,
            BannedById = bannedById,
            BannedUntil = duration.HasValue ? DateTime.UtcNow.Add(duration.Value) : null
        };

        db.UserBans.Add(ban);
        await db.SaveChangesAsync();
        return ban;
    }

    public async Task<bool> RevokeAsync(Guid banId, string revokedById)
    {
        var ban = await db.UserBans.FirstOrDefaultAsync(b => b.Id == banId && b.RevokedAt == null);
        if (ban == null) return false;

        ban.RevokedAt = DateTime.UtcNow;
        ban.RevokedById = revokedById;
        await db.SaveChangesAsync();
        return true;
    }
}
