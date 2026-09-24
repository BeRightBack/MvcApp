namespace MvcApp.Core.Abstractions;

public interface INotificationSettingsRepository
{
    Task<UserNotificationSettings?> GetAsync(string userId);
    Task<UserNotificationSettings> GetOrCreateAsync(string userId);
    Task UpdateAsync(string userId, bool emailOnLike, bool emailOnMatch);
}
