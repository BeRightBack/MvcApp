using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace MvcApp.Core
{
    public class UserRole : IdentityRole
    {
        public UserRole() : base() { }
        public UserRole(string roleName) : base(roleName) { }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [NotMapped]
        public bool Selected { get; set; } = false;

        public string? Description { get; set; }

    }
}
