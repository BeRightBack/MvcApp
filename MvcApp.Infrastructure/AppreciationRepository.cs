using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Abstractions;

namespace MvcApp.Infrastructure;

public class AppreciationRepository(UserDbContext db) : IAppreciationRepository
{
    public async Task<ProfileAppreciation?> GetAppreciationAsync(string sourceUserId, string targetUserId)
    {
        return await db.ProfileAppreciations
            .FirstOrDefaultAsync(a => a.SourceUserId == sourceUserId && a.TargetUserId == targetUserId);
    }

    public async Task<double> GetAverageScoreAsync(string targetUserId)
    {
        var values = await db.ProfileAppreciations
            .Where(a => a.TargetUserId == targetUserId)
            .Select(a => (double)a.Value)
            .ToListAsync();

        return values.Count > 0 ? values.Average() : 0;
    }

    public async Task<int> GetTotalAppreciationsAsync(string targetUserId)
    {
        return await db.ProfileAppreciations
            .CountAsync(a => a.TargetUserId == targetUserId);
    }

    public async Task<bool> AddAppreciationAsync(ProfileAppreciation appreciation)
    {
        db.ProfileAppreciations.Add(appreciation);
        return await db.SaveChangesAsync() > 0;
    }
}
