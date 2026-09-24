using MvcApp.Core;

namespace MvcApp.Web.Areas.Admin.Models.UserViewModels
{
    public class RoleWithUsersViewModel
    {
        public UserRole Role { get; set; } = new UserRole();
        public IEnumerable<UserProfile> Users { get; set; } = new List<UserProfile>();
    }
}
