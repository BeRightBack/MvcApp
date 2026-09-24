using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MvcApp.Core;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MvcApp.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<UserDetails> _userManager;
        private readonly SignInManager<UserDetails> _signInManager;
        private readonly MvcApp.Infrastructure.UserDbContext _db;

        public IndexModel(
            UserManager<UserDetails> userManager,
            SignInManager<UserDetails> signInManager,
            MvcApp.Infrastructure.UserDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
        }

        public string? Username { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? UserNameChangeLimitMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Phone]
            [Display(Name = "Phone number")]
            public string? PhoneNumber { get; set; }

            [Display(Name = "First name")]
            public string? FirstName { get; set; }

            [Display(Name = "Last name")]
            public string? LastName { get; set; }

            [Display(Name = "Username")]
            public string? Username { get; set; }

            public byte[]? ProfilePicture { get; set; }
            public string? ProfilePicturePath { get; set; }
            public string? StatusMessage { get; set; }
            public string? UserNameChangeLimitMessage { get; set; }

            public IFormFile? ProfilePictureFile { get; set; }

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

            [Display(Name = "Incognito Mode")]
            public bool IsIncognito { get; set; }

            [Display(Name = "Who can message you")]
            public MessagingPermission MessagingPermission { get; set; }

            public List<int> InterestTagIds { get; set; } = [];
        }

        public List<InterestTag> AvailableTags { get; set; } = [];

        private async Task LoadAsync(UserDetails user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName;

            UserNameChangeLimitMessage = $"You can change your username {user.UsernameChangeLimit} more time(s).";

            AvailableTags = await _db.InterestTags.OrderBy(t => t.Category).ThenBy(t => t.Name).ToListAsync();

            var userTagIds = await _db.UserInterestTags
                .Where(ut => ut.UserId == user.Id)
                .Select(ut => ut.TagId)
                .ToListAsync();

            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Username = userName,
                ProfilePicture = user.ProfilePicture,
                ProfilePicturePath = user.ProfilePicturePath,
                StatusMessage = StatusMessage,
                UserNameChangeLimitMessage = UserNameChangeLimitMessage,
                KnownAs = user.KnownAs,
                Introduction = user.Introduction,
                LookingFor = user.LookingFor,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth == DateTime.MinValue ? null : user.DateOfBirth,
                City = user.City,
                Country = user.Country,
                IsIncognito = user.IsIncognito,
                MessagingPermission = user.MessagingPermission,
                InterestTagIds = userTagIds
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            if (Input.FirstName != user.FirstName)
            {
                user.FirstName = Input.FirstName;
                await _userManager.UpdateAsync(user);

                var claims = await _userManager.GetClaimsAsync(user);
                var claim = claims.FirstOrDefault(x => x.Type == "user.firstname");
                if (claim != null)
                    await _userManager.RemoveClaimAsync(user, claim);

                if (!string.IsNullOrEmpty(Input.FirstName))
                    await _userManager.AddClaimAsync(user, new Claim("user.firstname", Input.FirstName));
            }

            if (Input.LastName != user.LastName)
            {
                user.LastName = Input.LastName;
                await _userManager.UpdateAsync(user);

                var claims = await _userManager.GetClaimsAsync(user);
                var claim = claims.FirstOrDefault(x => x.Type == "user.lastname");
                if (claim != null)
                    await _userManager.RemoveClaimAsync(user, claim);

                if (!string.IsNullOrEmpty(Input.LastName))
                    await _userManager.AddClaimAsync(user, new Claim("user.lastname", Input.LastName));
            }

            if (user.UsernameChangeLimit > 0)
            {
                if (!string.IsNullOrEmpty(Input.Username) && Input.Username != user.UserName)
                {
                    var userNameExists = await _userManager.FindByNameAsync(Input.Username);
                    if (userNameExists != null)
                    {
                        StatusMessage = "User name already taken. Select a different username.";
                        return RedirectToPage();
                    }

                    var setUserName = await _userManager.SetUserNameAsync(user, Input.Username);
                    if (!setUserName.Succeeded)
                    {
                        StatusMessage = "Unexpected error when trying to set user name.";
                        return RedirectToPage();
                    }

                    user.UsernameChangeLimit -= 1;
                    await _userManager.UpdateAsync(user);

                    var claims = await _userManager.GetClaimsAsync(user);
                    var claim = claims.FirstOrDefault(x => x.Type == "user.username");
                    if (claim != null)
                        await _userManager.RemoveClaimAsync(user, claim);

                    if (!string.IsNullOrEmpty(Input.Username))
                        await _userManager.AddClaimAsync(user, new Claim("user.username", Input.Username));
                }
            }

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

                await _userManager.UpdateAsync(user);

                var photoGalleryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Photos", user.UserName ?? user.Id);
                if (!Directory.Exists(photoGalleryPath))
                    Directory.CreateDirectory(photoGalleryPath);

                var galleryFilePath = Path.Combine(photoGalleryPath, newFileName);
                using (var stream = new FileStream(galleryFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var hasPhotos = user.Photos != null && user.Photos.Any();
                var photo = new Photo
                {
                    Filename = newFileName,
                    IsMain = !hasPhotos,
                    IsApproved = true,
                    UserDetails = user,
                    UserDetailsId = user.Id
                };
                user.Photos ??= [];
                user.Photos.Add(photo);
                await _userManager.UpdateAsync(user);
            }

            if (Input.KnownAs != user.KnownAs)
                user.KnownAs = Input.KnownAs ?? "";

            if (Input.Introduction != user.Introduction)
                user.Introduction = Input.Introduction ?? "";

            if (Input.LookingFor != user.LookingFor)
                user.LookingFor = Input.LookingFor ?? "";

            if (Input.Gender != user.Gender)
                user.Gender = Input.Gender ?? "";

            if (Input.DateOfBirth.HasValue && Input.DateOfBirth.Value != user.DateOfBirth)
                user.DateOfBirth = Input.DateOfBirth.Value;

            if (Input.City != user.City)
                user.City = Input.City ?? "";

            if (Input.Country != user.Country)
                user.Country = Input.Country ?? "";

            if (Input.IsIncognito != user.IsIncognito)
                user.IsIncognito = Input.IsIncognito;

            if (Input.MessagingPermission != user.MessagingPermission)
                user.MessagingPermission = Input.MessagingPermission;

            var existingTags = await _db.UserInterestTags.Where(ut => ut.UserId == user.Id).ToListAsync();
            _db.UserInterestTags.RemoveRange(existingTags);

            foreach (var tagId in Input.InterestTagIds.Distinct())
            {
                _db.UserInterestTags.Add(new UserInterestTag { UserId = user.Id, TagId = tagId });
            }

            await _db.SaveChangesAsync();
            await _userManager.UpdateAsync(user);
            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}
