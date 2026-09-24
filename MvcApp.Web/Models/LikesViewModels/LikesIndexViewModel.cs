using MvcApp.Core.Models;
using MvcApp.Core.Pagination;

namespace MvcApp.Web.Models.LikesViewModels;

public class LikesIndexViewModel
{
    public PaginationList<LikedMemberModel> Members { get; set; } = new([], 0, 1, 20);

    public List<MemberCardViewModel> Cards { get; set; } = [];

    public string Predicate { get; set; } = "liked";

    public string Gender { get; set; } = "All";

    public string OrderBy { get; set; } = "LastActive";

    public int LikedCount { get; set; }

    public int LikedByCount { get; set; }

    public string CurrentUserId { get; set; } = string.Empty;

    public string ReturnUrl { get; set; } = string.Empty;
}
