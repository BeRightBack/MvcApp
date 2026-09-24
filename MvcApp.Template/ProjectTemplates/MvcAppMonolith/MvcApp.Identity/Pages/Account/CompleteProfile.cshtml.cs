using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MvcApp.Core;
using System.ComponentModel.DataAnnotations;

namespace MvcApp.Identity.Pages.Account
{
    [Authorize]
    public class CompleteProfileModel : PageModel
    {
        private readonly UserManager<UserDetails> _userManager;
        private readonly SignInManager<UserDetails> _signInManager;

        public CompleteProfileModel(
            UserManager<UserDetails> userManager,
            SignInManager<UserDetails> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "First name is required")]
            [Display(Name = "First Name")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Last name is required")]
            [Display(Name = "Last Name")]
            public string LastName { get; set; } = string.Empty;

            [Display(Name = "Known As")]
            public string? KnownAs { get; set; }

            [Display(Name = "Introduction")]
            public string? Introduction { get; set; }

            [Display(Name = "Looking For")]
            public string? LookingFor { get; set; }

            [Required(ErrorMessage = "Gender is required")]
            [Display(Name = "Gender")]
            public string Gender { get; set; } = string.Empty;

            [Required(ErrorMessage = "Date of birth is required")]
            [DataType(DataType.Date)]
            [Display(Name = "Date of Birth")]
            public DateTime DateOfBirth { get; set; }

            [Display(Name = "City")]
            public string? City { get; set; }

            [Display(Name = "Country")]
            public string? Country { get; set; }

            public IFormFile? ProfilePictureFile { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            if (user.IsProfileComplete)
                return RedirectToPage("/Index");

            // Pre-fill from existing data
            Input.FirstName = user.FirstName ?? "";
            Input.LastName = user.LastName ?? "";
            Input.KnownAs = user.KnownAs;
            Input.Introduction = user.Introduction;
            Input.LookingFor = user.LookingFor;
            Input.Gender = user.Gender;
            Input.DateOfBirth = user.DateOfBirth;
            Input.City = user.City;
            Input.Country = user.Country;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            if (user.IsProfileComplete)
                return RedirectToPage("/Index");

            if (!ModelState.IsValid)
                return Page();

            user.FirstName = Input.FirstName;
            user.LastName = Input.LastName;
            user.KnownAs = Input.KnownAs ?? "";
            user.Introduction = Input.Introduction ?? "";
            user.LookingFor = Input.LookingFor ?? "";
            user.Gender = Input.Gender;
            user.DateOfBirth = Input.DateOfBirth;
            user.City = Input.City ?? "";
            user.Country = Input.Country ?? "";
            user.IsProfileComplete = true;

            if (Input.ProfilePictureFile != null)
            {
                var file = Input.ProfilePictureFile;
                var fileExtension = Path.GetExtension(file.FileName);
                var newFileName = $"{Guid.NewGuid()}{fileExtension}";
                var subPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", user.Id);

                if (!Directory.Exists(subPath))
                    Directory.CreateDirectory(subPath);

                var filePath = Path.Combine(subPath, newFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                user.ProfilePicturePath = $"/images/{user.Id}/{newFileName}";
                user.ProfilePicture = null;
            }

            await _userManager.UpdateAsync(user);
            await _signInManager.RefreshSignInAsync(user);

            return RedirectToPage("/Index");
        }
    }
}
