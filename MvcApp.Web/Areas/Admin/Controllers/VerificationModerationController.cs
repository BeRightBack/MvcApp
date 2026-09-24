using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Infrastructure;

namespace MvcApp.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class VerificationModerationController(
    UserDbContext db,
    UserManager<UserDetails> userManager,
    IGamificationService gamification) : Controller
{
    private const int PageSize = 12;

    public async Task<IActionResult> Index(string filter = "pending", int page = 1)
    {
        IQueryable<VerificationRequest> query = db.VerificationRequests
            .Include(v => v.User);

        var pendingCount = await db.VerificationRequests.CountAsync(v => v.Status == VerificationStatus.Pending);

        query = filter switch
        {
            "approved" => query.Where(v => v.Status == VerificationStatus.Approved),
            "rejected" => query.Where(v => v.Status == VerificationStatus.Rejected),
            _ => query.Where(v => v.Status == VerificationStatus.Pending)
        };

        var total = await query.CountAsync();
        var requests = await query
            .OrderByDescending(v => v.RequestedAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        ViewBag.Filter = filter;
        ViewBag.PendingCount = pendingCount;

        return View(requests);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? notes)
    {
        var request = await db.VerificationRequests.FindAsync(id);
        if (request == null) return NotFound();

        request.Status = VerificationStatus.Approved;
        request.AdminNotes = notes;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewedBy = User.Identity?.Name;

        var user = await userManager.FindByIdAsync(request.UserId);
        if (user != null)
        {
            user.IsVerified = true;
            await userManager.UpdateAsync(user);
            await gamification.AwardPointsAsync(user.Id, 100, "Verified account", "Verification");
        }

        await db.SaveChangesAsync();

        TempData["Message"] = $"Verification approved for {user?.KnownAs ?? user?.UserName}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? notes)
    {
        var request = await db.VerificationRequests.FindAsync(id);
        if (request == null) return NotFound();

        request.Status = VerificationStatus.Rejected;
        request.AdminNotes = notes;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewedBy = User.Identity?.Name;

        await db.SaveChangesAsync();

        TempData["Message"] = $"Verification rejected.";
        return RedirectToAction(nameof(Index));
    }
}
