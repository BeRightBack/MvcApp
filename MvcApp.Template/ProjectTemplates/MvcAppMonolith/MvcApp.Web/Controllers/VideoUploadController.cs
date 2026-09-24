using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Infrastructure;

namespace MvcApp.Web.Controllers;

[Authorize]
public class VideoUploadController(UserDbContext db, UserManager<UserDetails> userManager, IGamificationService gamification) : Controller
{
    private static readonly string[] AllowedVideoTypes = [
        "video/mp4", "video/webm", "video/ogg", "video/quicktime", "video/x-msvideo"
    ];
    private const long MaxFileSize = 100 * 1024 * 1024; // 100MB
    private const int PageSize = 12;

    public async Task<IActionResult> Index(string? category, string search = "", int page = 1)
    {
        var query = db.VideoUploads
            .Where(v => !v.IsDeleted && v.IsApproved)
            .Include(v => v.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(category) && Enum.TryParse<VideoCategory>(category, out var cat))
            query = query.Where(v => v.Category == cat);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(v => v.Title.Contains(search) || v.Description.Contains(search));

        var total = await query.CountAsync();
        var videos = await query
            .OrderByDescending(v => v.UploadedAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        ViewBag.Search = search;
        ViewBag.Category = category;
        ViewBag.Categories = Enum.GetValues<VideoCategory>();

        return View(videos);
    }

    [HttpGet]
    public IActionResult Upload()
    {
        ViewBag.Categories = Enum.GetValues<VideoCategory>();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(string title, string description, VideoCategory category, IFormFile videoFile, IFormFile? thumbnailFile)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["Error"] = "Title is required.";
            ViewBag.Categories = Enum.GetValues<VideoCategory>();
            return View();
        }

        if (videoFile == null || videoFile.Length == 0)
        {
            TempData["Error"] = "Please select a video file.";
            ViewBag.Categories = Enum.GetValues<VideoCategory>();
            return View();
        }

        if (!AllowedVideoTypes.Contains(videoFile.ContentType.ToLower()))
        {
            TempData["Error"] = "Only MP4, WebM, OGG, MOV, and AVI videos are allowed.";
            ViewBag.Categories = Enum.GetValues<VideoCategory>();
            return View();
        }

        if (videoFile.Length > MaxFileSize)
        {
            TempData["Error"] = "Video file must be under 100MB.";
            ViewBag.Categories = Enum.GetValues<VideoCategory>();
            return View();
        }

        var videoExt = Path.GetExtension(videoFile.FileName);
        var videoName = $"{Guid.NewGuid()}{videoExt}";
        var userDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Videos", user.UserName ?? user.Id);

        if (!Directory.Exists(userDir))
            Directory.CreateDirectory(userDir);

        var videoPath = Path.Combine(userDir, videoName);
        using (var stream = new FileStream(videoPath, FileMode.Create))
        {
            await videoFile.CopyToAsync(stream);
        }

        string? thumbName = null;
        if (thumbnailFile != null && thumbnailFile.Length > 0)
        {
            var thumbExt = Path.GetExtension(thumbnailFile.FileName);
            thumbName = $"{Guid.NewGuid()}{thumbExt}";
            var thumbPath = Path.Combine(userDir, thumbName);
            using (var stream = new FileStream(thumbPath, FileMode.Create))
            {
                await thumbnailFile.CopyToAsync(stream);
            }
        }

        var video = new VideoUpload
        {
            Title = title.Trim(),
            Description = description?.Trim() ?? "",
            Filename = videoName,
            ThumbnailFilename = thumbName,
            Category = category,
            UserId = user.Id,
            IsApproved = false
        };

        db.VideoUploads.Add(video);
        await db.SaveChangesAsync();

        await gamification.AwardPointsAsync(user.Id, 15, "Uploaded a video", "Video", video.Id);

        TempData["Message"] = "Video uploaded! It will appear after moderator approval.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Watch(int id)
    {
        var video = await db.VideoUploads
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

        if (video == null) return NotFound();

        video.ViewCount++;
        await db.SaveChangesAsync();

        return View(video);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var video = await db.VideoUploads.FirstOrDefaultAsync(v => v.Id == id && v.UserId == user.Id);
        if (video == null) return NotFound();

        video.IsDeleted = true;
        await db.SaveChangesAsync();

        TempData["Message"] = "Video deleted.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> MyVideos()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var videos = await db.VideoUploads
            .Where(v => v.UserId == user.Id && !v.IsDeleted)
            .OrderByDescending(v => v.UploadedAt)
            .ToListAsync();

        return View("Index", videos);
    }
}
