namespace MvcApp.Core.Abstractions
{
    public interface ISuperLikeRepository
    {
        Task<HashSet<string>> GetSuperLikedUserIdsAsync(string sourceUserId);
        Task<HashSet<string>> GetSuperLikedByUserIdsAsync(string targetUserId);
    }
}
