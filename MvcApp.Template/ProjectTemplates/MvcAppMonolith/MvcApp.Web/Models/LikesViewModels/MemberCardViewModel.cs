using MvcApp.Core.Models;

namespace MvcApp.Web.Models.LikesViewModels;

public class MemberCardViewModel
{
    public LikedMemberModel Member { get; set; } = new();

    public string ModalId { get; set; } = string.Empty;

    public bool ShowLikeButton { get; set; } = true;

    public bool ShowMessageButton { get; set; } = true;

    public string LikeLabel { get; set; } = "Like";

    public string CurrentUserId { get; set; } = string.Empty;

    public bool IsBlockedByYou { get; set; }
}
