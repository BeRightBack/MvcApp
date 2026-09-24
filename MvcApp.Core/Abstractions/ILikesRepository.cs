using MvcApp.Core;
using MvcApp.Core.Models;
using MvcApp.Core.Pagination;

namespace MvcApp.Core.Abstractions;

public interface ILikesRepository
{
    Task<UserLike?> GetUserLikeAsync(string sourceUserId, string likedUserId);
    Task<UserDetails?> GetUserWithLikesAsync(string userId);
    Task<PaginationList<LikedMemberModel>> GetUserLikesAsync(LikesParameters likesParameters);
    Task<(int Liked, int LikedBy)> GetLikeCountsAsync(string userId);
    Task<HashSet<string>> GetLikedUserIdsAsync(string userId);
    Task<HashSet<string>> GetLikedByUserIdsAsync(string userId);
    Task<List<LikedMemberModel>> GetMatchesAsync(string userId);
}
