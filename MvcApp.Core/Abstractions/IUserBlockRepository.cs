using MvcApp.Core.Models;

namespace MvcApp.Core.Abstractions;

public interface IUserBlockRepository
{
    Task<UserBlock?> GetBlockAsync(string sourceUserId, string blockedUserId);
    Task<HashSet<string>> GetBlockedUserIdsAsync(string userId);
    Task<HashSet<string>> GetBlockersOfAsync(string userId);
    Task<List<LikedMemberModel>> GetBlockedUsersAsync(string userId);
    Task AddAsync(UserBlock block);
    Task RemoveAsync(string sourceUserId, string blockedUserId);
    Task<bool> IsBlockedAsync(string userIdA, string userIdB);
}
