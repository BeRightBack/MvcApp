using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Infrastructure;

namespace MvcApp.Web.Controllers;

public class AccountController(UserManager<UserDetails> userManager, UserDbContext db) : Controller
{
    public async Task<IActionResult> GetProfilePicture(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        if (!string.IsNullOrEmpty(user.ProfilePicturePath))
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfilePicturePath.TrimStart('/'));
            if (System.IO.File.Exists(path))
            {
                var contentType = GetContentType(user.ProfilePicturePath);
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                return PhysicalFile(path, contentType);
            }
        }

        if (user.ProfilePicture is { Length: > 0 })
        {
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            return File(user.ProfilePicture, "image/jpeg");
        }

        // Fallback: seeded users have no profile picture file/BLOB; show their main approved photo instead.
        var mainPhoto = await db.Photos
            .Where(p => p.UserDetailsId == user.Id && p.IsMain && p.IsApproved)
            .OrderByDescending(p => p.Id)
            .Select(p => p.Filename)
            .FirstOrDefaultAsync();

        if (!string.IsNullOrEmpty(mainPhoto))
        {
            if (mainPhoto.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                mainPhoto.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                return Redirect(mainPhoto);
            }

            var photoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Photos", user.UserName ?? user.Id, mainPhoto.TrimStart('/'));
            if (System.IO.File.Exists(photoPath))
            {
                var contentType = GetContentType(mainPhoto);
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                return PhysicalFile(photoPath, contentType);
            }
        }

        return NotFound();
    }

    private static string GetContentType(string? path)
    {
        var ext = Path.GetExtension(path)?.ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "image/jpeg"
        };
    }
}
