using MvcApp.Core;

namespace MvcApp.Core.Abstractions;

public interface IBanService
{
    Task<UserBan?> GetActiveBanAsync(string userId);
    Task<bool> IsBannedAsync(string userId);
    Task<IReadOnlyList<UserBan>> GetBansAsync();
    Task<UserBan?> BanUserAsync(string userId, string reason, string bannedById, TimeSpan? duration);
    Task<bool> RevokeAsync(Guid banId, string revokedById);
}
