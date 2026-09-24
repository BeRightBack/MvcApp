using MvcApp.Localization.Custom;
using System.ComponentModel.DataAnnotations;

namespace MvcApp.Web.Models.HomeViewModels
{
    public class ContactViewModel
    {
        [CustomRequired("The {0} field is required.")]
        [CustomStringLength(6, 100, "The {0} must be at least {1} and at max {2} characters long.")]
        [LocalizedDisplayName("Name")]
        public required string Name { get; set; }

        [CustomRequired("The {0} field is required.")]
        [CustomEmailAddress]
        [LocalizedDisplayName("Email")]
        public required string Email { get; set; }

        [CustomRequired("The {0} field is required.")]
        [CustomStringLength(5, 100, "The {0} must be at least {1} and at max {2} characters long.")]
        [LocalizedDisplayName("Subject")]
        public required string Subject { get; set; }

        [CustomRequired("The {0} field is required.")]
        [CustomStringLength(6, 1024, "The {0} must be at least {1} and at max {2} characters long.")]
        [LocalizedDisplayName("Message")]
        public required string Message { get; set; }

        [Required]
        [Display(Name = "Captcha Code")]
        public required string CaptchaCode { get; set; }
    }
}
