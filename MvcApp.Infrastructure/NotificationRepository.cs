using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Abstractions;

namespace MvcApp.Infrastructure;

public class NotificationRepository(UserDbContext db) : INotificationRepository
{
    public async Task AddAsync(Notification notification)
    {
        await db.Notifications!.AddAsync(notification);
    }

    public async Task<List<Notification>> GetForUserAsync(string userId, int count = 50)
    {
        return await db.Notifications!
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Take(count)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await db.Notifications!.CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task MarkAllReadAsync(string userId)
    {
        var unread = await db.Notifications!
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in unread)
        {
            notification.IsRead = true;
        }
    }
}
