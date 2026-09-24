namespace MvcApp.Core.Models;

public class LikedMemberModel : MemberModel
{
    public bool IsLikedByYou { get; set; }
    public bool IsMutual { get; set; }
    public bool IsSuperLikedByYou { get; set; }
    public bool IsBoosted { get; set; }
    public bool IsPhotoBlurred { get; set; }
}
