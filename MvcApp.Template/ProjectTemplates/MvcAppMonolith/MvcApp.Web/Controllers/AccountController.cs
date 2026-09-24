using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MvcApp.Core;

namespace MvcApp.Web.Controllers;

public class AccountController(UserManager<UserDetails> userManager) : Controller
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
