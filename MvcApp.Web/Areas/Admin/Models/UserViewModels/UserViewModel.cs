using System.ComponentModel.DataAnnotations;
using MvcApp.Core;

namespace MvcApp.Web.Areas.Admin.Models.UserViewModels
{
    public class UserViewModel
    {
        public string? Id { get; set; }

        [Display(Name = "UserName")]
        public string? UserName { get; set; }

        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        [Display(Name = "Email")]
        public string? Email { get; set; }
        public bool EmailConfirmed { get; set; } = false;

        [Display(Name = "Password")]
        public string? Password { get; set; }

        [Phone]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }

        public bool PhoneNumberConfirmed { get; set; } = false;

        [Display(Name = "Known As")]
        public string? KnownAs { get; set; }

        [Display(Name = "Introduction")]
        public string? Introduction { get; set; }

        [Display(Name = "Looking For")]
        public string? LookingFor { get; set; }

        [Display(Name = "Gender")]
        public string? Gender { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "City")]
        public string? City { get; set; }

        [Display(Name = "Country")]
        public string? Country { get; set; }

        [Display(Name = "Profile Complete")]
        public bool IsProfileComplete { get; set; }
    }

    public class UserIndexViewModel
    {
        public IEnumerable<UserDetails> Users { get; set; } = [];
        public string SearchQuery { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
    }
}
