using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MvcApp.Web.Controllers
{
    [Authorize]
    public class GalleryController : Controller
    {
        private readonly UserDbContext _db;
        private readonly UserManager<UserDetails> _userManager;
        private readonly IGamificationService _gamification;

        public GalleryController(UserDbContext db, UserManager<UserDetails> userManager, IGamificationService gamification)
        {
            _db = db;
            _userManager = userManager;
            _gamification = gamification;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var photos = await _db.Photos
                .Where(p => p.UserDetailsId == user.Id)
                .OrderByDescending(p => p.IsMain)
                .ThenByDescending(p => p.Id)
                .ToListAsync();

            ViewBag.Username = user.UserName;
            ViewBag.PendingCount = await _db.Photos.CountAsync(p => p.UserDetailsId == user.Id && !p.IsApproved);

            return View(photos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file, PrivacyLevel privacy = PrivacyLevel.Public)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file.";
                return RedirectToAction(nameof(Index));
            }

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
            {
                TempData["Error"] = "Only JPEG, PNG, GIF, and WebP images are allowed.";
                return RedirectToAction(nameof(Index));
            }

            if (file.Length > 10 * 1024 * 1024)
            {
                TempData["Error"] = "File size must be under 10MB.";
                return RedirectToAction(nameof(Index));
            }

            var photoCount = await _db.Photos.CountAsync(p => p.UserDetailsId == user.Id);
            if (photoCount >= 10)
            {
                TempData["Error"] = "Maximum 10 photos allowed.";
                return RedirectToAction(nameof(Index));
            }

            var fileExtension = Path.GetExtension(file.FileName);
            var newFileName = $"{Guid.NewGuid()}{fileExtension}";
            var subPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Photos", user.UserName ?? user.Id);

            if (!Directory.Exists(subPath))
                Directory.CreateDirectory(subPath);

            var filePath = Path.Combine(subPath, newFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var isFirst = !await _db.Photos.AnyAsync(p => p.UserDetailsId == user.Id);
            var photo = new Photo
            {
                Filename = newFileName,
                IsMain = isFirst,
                IsApproved = true,
                PrivacyLevel = privacy,
                UserDetails = user,
                UserDetailsId = user.Id
            };

            _db.Photos.Add(photo);
            await _db.SaveChangesAsync();

            await _gamification.AwardPointsAsync(user.Id, 10, "Uploaded a photo", "Photo", photo.Id);

            TempData["Message"] = "Photo uploaded successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetMain(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var photo = await _db.Photos.FirstOrDefaultAsync(p => p.Id == id && p.UserDetailsId == user.Id);
            if (photo == null) return NotFound();

            var currentMain = await _db.Photos.FirstOrDefaultAsync(p => p.UserDetailsId == user.Id && p.IsMain);
            if (currentMain != null) currentMain.IsMain = false;

            photo.IsMain = true;
            await _db.SaveChangesAsync();

            TempData["Message"] = "Main photo updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPrivacy(int id, PrivacyLevel privacy)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var photo = await _db.Photos.FirstOrDefaultAsync(p => p.Id == id && p.UserDetailsId == user.Id);
            if (photo == null) return NotFound();

            photo.PrivacyLevel = privacy;
            await _db.SaveChangesAsync();

            TempData["Message"] = "Privacy level updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var photo = await _db.Photos.FirstOrDefaultAsync(p => p.Id == id && p.UserDetailsId == user.Id);
            if (photo == null) return NotFound();

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Photos", user.UserName ?? user.Id, photo.Filename);
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            _db.Photos.Remove(photo);

            if (photo.IsMain)
            {
                var nextPhoto = await _db.Photos.FirstOrDefaultAsync(p => p.UserDetailsId == user.Id);
                if (nextPhoto != null) nextPhoto.IsMain = true;
            }

            await _db.SaveChangesAsync();

            TempData["Message"] = "Photo deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
