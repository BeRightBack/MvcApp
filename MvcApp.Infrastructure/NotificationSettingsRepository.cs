using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Abstractions;

namespace MvcApp.Infrastructure;

public class NotificationSettingsRepository(UserDbContext db) : INotificationSettingsRepository
{
    public async Task<UserNotificationSettings?> GetAsync(string userId)
    {
        return await db.UserNotificationSettings!
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId);
    }

    public async Task<UserNotificationSettings> GetOrCreateAsync(string userId)
    {
        var existing = await db.UserNotificationSettings!
            .FirstOrDefaultAsync(s => s.UserId == userId);
        if (existing != null)
        {
            return existing;
        }

        var settings = new UserNotificationSettings { UserId = userId };
        db.UserNotificationSettings!.Add(settings);
        return settings;
    }

    public async Task UpdateAsync(string userId, bool emailOnLike, bool emailOnMatch)
    {
        var settings = await db.UserNotificationSettings!
            .FirstOrDefaultAsync(s => s.UserId == userId);
        if (settings == null)
        {
            settings = new UserNotificationSettings { UserId = userId };
            db.UserNotificationSettings!.Add(settings);
        }

        settings.EmailOnLike = emailOnLike;
        settings.EmailOnMatch = emailOnMatch;
        settings.UpdatedAt = DateTime.UtcNow;
    }
}
