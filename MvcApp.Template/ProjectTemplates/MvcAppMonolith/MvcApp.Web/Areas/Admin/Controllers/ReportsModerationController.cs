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
public class ReportsModerationController(
    UserDbContext db,
    UserManager<UserDetails> userManager,
    IBanService banService,
    IAuditService auditService) : Controller
{
    private const int PageSize = 20;

    public async Task<IActionResult> Index(string filter = "pending", int page = 1)
    {
        IQueryable<Report> query = db.Reports
            .Include(r => r.Reporter)
            .Include(r => r.ReportedUser);

        var pendingCount = await db.Reports.CountAsync(r => r.Status == ReportStatus.Pending);

        query = filter switch
        {
            "reviewed" => query.Where(r => r.Status == ReportStatus.Reviewed),
            "dismissed" => query.Where(r => r.Status == ReportStatus.Dismissed),
            _ => query.Where(r => r.Status == ReportStatus.Pending)
        };

        var total = await query.CountAsync();
        var reports = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        ViewBag.Filter = filter;
        ViewBag.PendingCount = pendingCount;

        return View(reports);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Dismiss(int id, string? notes)
    {
        var report = await db.Reports.FindAsync(id);
        if (report == null) return NotFound();

        report.Status = ReportStatus.Dismissed;
        report.AdminNotes = notes;
        report.ReviewedAt = DateTime.UtcNow;
        report.ReviewedBy = User.Identity?.Name;

        await db.SaveChangesAsync();

        TempData["Message"] = "Report dismissed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BanAndDismiss(int id, string? notes, int durationHours = 72)
    {
        var report = await db.Reports.FindAsync(id);
        if (report == null) return NotFound();

        var admin = await userManager.GetUserAsync(User);
        if (admin == null) return Challenge();

        var duration = durationHours == -1 ? (TimeSpan?)null : TimeSpan.FromHours(durationHours);
        await banService.BanUserAsync(report.ReportedUserId, notes ?? $"Banned via report #{id}", admin.Id, duration);

        report.Status = ReportStatus.Reviewed;
        report.AdminNotes = $"Banned user. {notes}";
        report.ReviewedAt = DateTime.UtcNow;
        report.ReviewedBy = User.Identity?.Name;

        await db.SaveChangesAsync();
        await auditService.LogAsync("BanViaReport", "Report", id.ToString(), $"Banned user {report.ReportedUserId} via report #{id}");

        TempData["Message"] = "User banned and report resolved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkReviewed(int id, string? notes)
    {
        var report = await db.Reports.FindAsync(id);
        if (report == null) return NotFound();

        report.Status = ReportStatus.Reviewed;
        report.AdminNotes = notes;
        report.ReviewedAt = DateTime.UtcNow;
        report.ReviewedBy = User.Identity?.Name;

        await db.SaveChangesAsync();

        TempData["Message"] = "Report marked as reviewed.";
        return RedirectToAction(nameof(Index));
    }
}
