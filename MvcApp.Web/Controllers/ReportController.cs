using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Infrastructure;

namespace MvcApp.Web.Controllers;

[Authorize]
public class ReportController(UserDbContext db, UserManager<UserDetails> userManager) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(string reportedUserId, ReportReason reason, string? description)
    {
        var reporter = await userManager.GetUserAsync(User);
        if (reporter == null) return Challenge();

        if (reporter.Id == reportedUserId)
        {
            TempData["Error"] = "You cannot report yourself.";
            return RedirectToAction("Details", "Members", new { id = reportedUserId });
        }

        var alreadyReported = await db.Reports
            .AnyAsync(r => r.ReporterId == reporter.Id && r.ReportedUserId == reportedUserId && r.Status == ReportStatus.Pending);
        if (alreadyReported)
        {
            TempData["Error"] = "You have already reported this user. Our team is reviewing it.";
            return RedirectToAction("Details", "Members", new { id = reportedUserId });
        }

        var reportedUser = await userManager.FindByIdAsync(reportedUserId);
        if (reportedUser == null) return NotFound();

        var report = new Report
        {
            ReporterId = reporter.Id,
            ReportedUserId = reportedUserId,
            Reason = reason,
            Description = description?.Trim(),
            Status = ReportStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        db.Reports.Add(report);
        await db.SaveChangesAsync();

        TempData["Message"] = $"Thank you for your report. Our team will review {reportedUser.KnownAs}'s profile within 24 hours.";
        return RedirectToAction("Details", "Members", new { id = reportedUser.UserName });
    }

    public async Task<IActionResult> MyReports()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var reports = await db.Reports
            .Where(r => r.ReporterId == user.Id)
            .Include(r => r.ReportedUser)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return View(reports);
    }
}
