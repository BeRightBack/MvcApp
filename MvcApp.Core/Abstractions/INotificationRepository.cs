namespace MvcApp.Core.Abstractions;

public interface INotificationRepository
{
    Task AddAsync(Notification notification);
    Task<List<Notification>> GetForUserAsync(string userId, int count = 50);
    Task<int> GetUnreadCountAsync(string userId);
    Task MarkAllReadAsync(string userId);
}
