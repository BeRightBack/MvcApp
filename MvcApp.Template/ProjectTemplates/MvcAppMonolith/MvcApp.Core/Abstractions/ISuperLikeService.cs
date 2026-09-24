namespace MvcApp.Core.Abstractions
{
    public interface ISuperLikeService
    {
        Task<bool> CanSuperLikeTodayAsync(string userId);
        Task<int> GetSuperLikesUsedTodayAsync(string userId);
        Task<int> GetSuperLikeLimitAsync(string userId);
        Task<bool> HasSuperLikedAsync(string sourceUserId, string targetUserId);
        Task<bool> SendSuperLikeAsync(string sourceUserId, string targetUserId);
        Task<HashSet<string>> GetSuperLikedUserIdsAsync(string sourceUserId);
        Task<HashSet<string>> GetSuperLikedByUserIdsAsync(string targetUserId);
        Task<bool> IsBoostActiveAsync(string userId);
        Task<int> GetBoostsUsedTodayAsync(string userId);
        Task<int> GetBoostLimitAsync(string userId);
        Task<bool> ActivateBoostAsync(string userId);
        Task<List<string>> GetBoostedUserIdsAsync();
    }
}
