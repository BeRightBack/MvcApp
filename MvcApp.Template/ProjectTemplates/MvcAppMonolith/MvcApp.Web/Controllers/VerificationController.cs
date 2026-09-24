using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Infrastructure;

namespace MvcApp.Web.Controllers;

[Authorize]
public class VerificationController(UserDbContext db, UserManager<UserDetails> userManager) : Controller
{
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

    public async Task<IActionResult> Index()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (user.IsVerified)
            return View("Verified");

        var existingRequest = await db.VerificationRequests
            .Where(v => v.UserId == user.Id)
            .OrderByDescending(v => v.RequestedAt)
            .FirstOrDefaultAsync();

        return View("Status", existingRequest);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(string displayNumber, IFormFile selfieFile)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (user.IsVerified)
        {
            TempData["Error"] = "You are already verified.";
            return RedirectToAction(nameof(Index));
        }

        var hasPendingRequest = await db.VerificationRequests
            .AnyAsync(v => v.UserId == user.Id && v.Status == VerificationStatus.Pending);
        if (hasPendingRequest)
        {
            TempData["Error"] = "You already have a pending verification request.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(displayNumber))
        {
            TempData["Error"] = "Please enter the number shown in your photo.";
            ViewBag.DisplayNumber = displayNumber;
            return View("Status", null);
        }

        if (selfieFile == null || selfieFile.Length == 0)
        {
            TempData["Error"] = "Please upload a selfie photo.";
            return RedirectToAction(nameof(Index));
        }

        if (!AllowedImageTypes.Contains(selfieFile.ContentType.ToLower()))
        {
            TempData["Error"] = "Only JPEG, PNG, and WebP images are allowed.";
            return RedirectToAction(nameof(Index));
        }

        if (selfieFile.Length > MaxFileSize)
        {
            TempData["Error"] = "Image must be under 10MB.";
            return RedirectToAction(nameof(Index));
        }

        var ext = Path.GetExtension(selfieFile.FileName);
        var fileName = $"verify_{Guid.NewGuid()}{ext}";
        var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Verifications", user.UserName ?? user.Id);

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var filePath = Path.Combine(dir, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await selfieFile.CopyToAsync(stream);
        }

        var request = new VerificationRequest
        {
            UserId = user.Id,
            Filename = fileName,
            DisplayNumber = displayNumber.Trim(),
            Status = VerificationStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };

        db.VerificationRequests.Add(request);
        await db.SaveChangesAsync();

        TempData["Message"] = "Verification request submitted! Our team will review it shortly.";
        return RedirectToAction(nameof(Index));
    }
}
