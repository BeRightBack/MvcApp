using MvcApp.Core.Models;

namespace MvcApp.Web.Models
{
    public class MemberProfileViewModel
    {
        public LikedMemberModel Member { get; set; } = new();
        public IList<PhotoModel> Photos { get; set; } = [];
    }
}
