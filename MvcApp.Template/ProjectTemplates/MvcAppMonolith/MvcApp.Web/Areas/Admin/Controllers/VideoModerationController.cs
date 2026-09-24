using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Infrastructure;

namespace MvcApp.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class VideoModerationController(UserDbContext db) : Controller
{
    private const int PageSize = 12;

    public async Task<IActionResult> Index(string filter = "pending", int page = 1)
    {
        IQueryable<VideoUpload> query = db.VideoUploads
            .Where(v => !v.IsDeleted)
            .Include(v => v.User);

        var pendingCount = await db.VideoUploads.CountAsync(v => !v.IsDeleted && !v.IsApproved);

        query = filter switch
        {
            "approved" => query.Where(v => v.IsApproved),
            "rejected" => query.Where(v => !v.IsApproved),
            _ => query.Where(v => !v.IsApproved)
        };

        var total = await query.CountAsync();
        var videos = await query
            .OrderByDescending(v => v.UploadedAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        ViewBag.Filter = filter;
        ViewBag.PendingCount = pendingCount;
        ViewBag.TotalCount = await db.VideoUploads.CountAsync(v => !v.IsDeleted);

        return View(videos);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var video = await db.VideoUploads.FindAsync(id);
        if (video == null) return NotFound();

        video.IsApproved = true;
        await db.SaveChangesAsync();

        TempData["Message"] = $"Video \"{video.Title}\" approved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var video = await db.VideoUploads.FindAsync(id);
        if (video == null) return NotFound();

        video.IsApproved = false;
        await db.SaveChangesAsync();

        TempData["Message"] = $"Video \"{video.Title}\" rejected.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var video = await db.VideoUploads.FindAsync(id);
        if (video == null) return NotFound();

        video.IsDeleted = true;
        await db.SaveChangesAsync();

        TempData["Message"] = $"Video \"{video.Title}\" deleted.";
        return RedirectToAction(nameof(Index));
    }
}
