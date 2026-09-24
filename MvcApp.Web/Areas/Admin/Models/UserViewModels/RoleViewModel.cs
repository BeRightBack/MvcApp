using MvcApp.Core;
using System.ComponentModel.DataAnnotations;

namespace MvcApp.Web.Areas.Admin.Models.UserViewModels
{
    public class RoleViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Role Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Role Description")]
        public string Description { get; set; } = string.Empty;

        public IEnumerable<UserDetails> Members { get; set; } = new List<UserDetails>();
        public IEnumerable<UserDetails> NonMembers { get; set; } = new List<UserDetails>();
        public string[]? IdsToAdd { get; set; }
        public string[]? IdsToDelete { get; set; }
    }

    
}
