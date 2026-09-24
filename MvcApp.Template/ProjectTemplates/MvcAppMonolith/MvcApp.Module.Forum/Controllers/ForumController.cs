using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Common.Filters;
using MvcApp.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace MvcApp.Module.Forum.Controllers;

[Authorize]
[ModuleEnabledFilter("Forum")]
public class ForumController(
    UserManager<UserDetails> userManager,
    IRepository<ForumCategory> categoryRepo,
    IRepository<MvcApp.Core.Forum> forumRepo,
    IRepository<ForumThread> threadRepo,
    IRepository<ForumPost> postRepo,
    IBanService banService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var categories = await categoryRepo.Query().OrderBy(c => c.SortOrder).ToListAsync();

        var forums = await forumRepo.Query().OrderBy(f => f.SortOrder).ToListAsync();

        ViewBag.Forums = forums;
        return View(categories);
    }

    public async Task<IActionResult> Forum(int id, int page = 1)
    {
        var forum = await forumRepo.GetFirstOrDefaultAsync(f => f.Id == id);
        if (forum == null) return NotFound();

        const int pageSize = 20;
        var threadsQuery = threadRepo.Query()
            .Where(t => t.ForumId == id && !t.IsDeleted)
            .OrderByDescending(t => t.IsPinned)
            .ThenByDescending(t => t.LastPostAt ?? t.CreatedAt);

        var totalThreads = await threadsQuery.CountAsync();
        var totalPages = (int)Math.Ceiling(totalThreads / (double)pageSize);
        var pagedThreads = await threadsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Forum = forum;
        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;
        return View(pagedThreads);
    }

    [Authorize]
    public IActionResult CreateThread(int forumId)
    {
        var forum = forumRepo.GetFirstOrDefaultAsync(f => f.Id == forumId).Result;
        if (forum == null) return NotFound();

        ViewBag.Forum = forum;
        return View();
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateThread(int forumId, string title, string content)
    {
        var currentUser = await userManager.GetUserAsync(User);
        if (currentUser == null) return Challenge();

        if (await banService.IsBannedAsync(currentUser.Id))
        {
            TempData["Error"] = "Your account is suspended. You cannot post to the forum.";
            return RedirectToAction(nameof(Forum), new { id = forumId });
        }

        var forum = await forumRepo.GetFirstOrDefaultAsync(f => f.Id == forumId);
        if (forum == null) return NotFound();

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
        {
            ModelState.AddModelError("", "Title and content are required.");
            ViewBag.Forum = forum;
            return View();
        }

        var thread = new ForumThread
        {
            ForumId = forumId,
            Title = title.Trim(),
            CreatedById = currentUser.Id,
            CreatedByUsername = currentUser.UserName!,
            CreatedAt = DateTime.UtcNow,
            LastPostAt = DateTime.UtcNow,
            LastPostByUserId = currentUser.Id,
            LastPostByUsername = currentUser.UserName
        };

        await threadRepo.AddAsync(thread);
        await postRepo.AddAsync(new ForumPost
        {
            ThreadId = thread.Id,
            CreatedById = currentUser.Id,
            CreatedByUsername = currentUser.UserName!,
            CreatedAt = DateTime.UtcNow,
            Content = content
        });

        thread.ReplyCount = 1;
        forum.ThreadCount++;
        forum.PostCount++;
        forum.LastPostAt = DateTime.UtcNow;
        forum.LastPostByUserId = currentUser.Id;
        forum.LastPostByUsername = currentUser.UserName;

        await UpdateAsync();
        return RedirectToAction(nameof(Thread), new { id = thread.Id });
    }

    public async Task<IActionResult> Thread(int id, int page = 1)
    {
        var currentUser = await userManager.GetUserAsync(User);
        if (currentUser == null) return Challenge();

        var thread = await threadRepo.GetFirstOrDefaultAsync(t => t.Id == id);
        if (thread == null || thread.IsDeleted) return NotFound();

        var forum = await forumRepo.GetFirstOrDefaultAsync(f => f.Id == thread.ForumId);

        const int pageSize = 10;
        var postsQuery = postRepo.Query()
            .Where(p => p.ThreadId == id && !p.IsDeleted)
            .OrderBy(p => p.CreatedAt);

        var totalPosts = await postsQuery.CountAsync();
        var totalPages = (int)Math.Ceiling(totalPosts / (double)pageSize);
        var pagedPosts = await postsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        thread.ViewCount++;
        await threadRepo.UpdateAsync(thread);
        await UpdateAsync();

        ViewBag.Thread = thread;
        ViewBag.Forum = forum;
        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.CurrentUserId = currentUser.Id;
        ViewBag.IsAdmin = User.IsInRole("Admin");
        return View(pagedPosts);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int threadId, string content)
    {
        var currentUser = await userManager.GetUserAsync(User);
        if (currentUser == null) return Challenge();

        if (await banService.IsBannedAsync(currentUser.Id))
        {
            TempData["Error"] = "Your account is suspended. You cannot post to the forum.";
            return RedirectToAction(nameof(Thread), new { id = threadId });
        }

        var thread = await threadRepo.GetFirstOrDefaultAsync(t => t.Id == threadId);
        if (thread == null || thread.IsLocked || thread.IsDeleted)
            return NotFound();

        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["Error"] = "Content is required.";
            return RedirectToAction(nameof(Thread), new { id = threadId });
        }

        var forum = await forumRepo.GetFirstOrDefaultAsync(f => f.Id == thread.ForumId);

        await postRepo.AddAsync(new ForumPost
        {
            ThreadId = threadId,
            CreatedById = currentUser.Id,
            CreatedByUsername = currentUser.UserName!,
            CreatedAt = DateTime.UtcNow,
            Content = content
        });

        thread.ReplyCount++;
        thread.LastPostAt = DateTime.UtcNow;
        thread.LastPostByUserId = currentUser.Id;
        thread.LastPostByUsername = currentUser.UserName;
        await threadRepo.UpdateAsync(thread);

        if (forum != null)
        {
            forum.PostCount++;
            forum.LastPostAt = DateTime.UtcNow;
            forum.LastPostByUserId = currentUser.Id;
            forum.LastPostByUsername = currentUser.UserName;
            await forumRepo.UpdateAsync(forum);
        }

        await UpdateAsync();
        return RedirectToAction(nameof(Thread), new { id = threadId });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePost(int postId)
    {
        var currentUser = await userManager.GetUserAsync(User);
        if (currentUser == null) return Challenge();

        var post = await postRepo.GetFirstOrDefaultAsync(p => p.Id == postId);
        if (post == null) return NotFound();

        var isAdmin = User.IsInRole("Admin");
        if (post.CreatedById != currentUser.Id && !isAdmin)
            return Forbid();

        post.IsDeleted = true;
        post.DeletedAt = DateTime.UtcNow;
        await postRepo.UpdateAsync(post);

        var thread = await threadRepo.GetFirstOrDefaultAsync(t => t.Id == post.ThreadId);
        if (thread != null)
        {
            thread.ReplyCount = Math.Max(0, thread.ReplyCount - 1);
            await threadRepo.UpdateAsync(thread);
        }

        var forumId = thread?.ForumId;
        var forum = await forumRepo.GetFirstOrDefaultAsync(f => f.Id == forumId);
        if (forum != null)
        {
            forum.PostCount = Math.Max(0, forum.PostCount - 1);
            await forumRepo.UpdateAsync(forum);
        }

        await UpdateAsync();
        TempData["Success"] = "Post deleted.";
        return RedirectToAction(nameof(Thread), new { id = post.ThreadId });
    }

    [Authorize]
    public async Task<IActionResult> EditPost(int id)
    {
        var currentUser = await userManager.GetUserAsync(User);
        if (currentUser == null) return Challenge();

        var post = await postRepo.GetFirstOrDefaultAsync(p => p.Id == id);
        if (post == null || post.IsDeleted) return NotFound();

        var isAdmin = User.IsInRole("Admin");
        if (post.CreatedById != currentUser.Id && !isAdmin)
            return Forbid();

        ViewBag.Post = post;
        return View();
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPost(int id, string content)
    {
        var currentUser = await userManager.GetUserAsync(User);
        if (currentUser == null) return Challenge();

        var post = await postRepo.GetFirstOrDefaultAsync(p => p.Id == id);
        if (post == null || post.IsDeleted) return NotFound();

        var isAdmin = User.IsInRole("Admin");
        if (post.CreatedById != currentUser.Id && !isAdmin)
            return Forbid();

        if (string.IsNullOrWhiteSpace(content))
        {
            ModelState.AddModelError("", "Content is required.");
            ViewBag.Post = post;
            return View();
        }

        post.Content = content;
        post.UpdatedAt = DateTime.UtcNow;
        post.UpdatedById = currentUser.Id;
        await postRepo.UpdateAsync(post);
        await UpdateAsync();

        TempData["Success"] = "Post updated.";
        return RedirectToAction(nameof(Thread), new { id = post.ThreadId });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PinThread(int threadId)
    {
        var thread = await threadRepo.GetFirstOrDefaultAsync(t => t.Id == threadId);
        if (thread == null) return NotFound();

        thread.IsPinned = !thread.IsPinned;
        await threadRepo.UpdateAsync(thread);
        await UpdateAsync();

        TempData["Success"] = thread.IsPinned ? "Thread pinned." : "Thread unpinned.";
        return RedirectToAction(nameof(Thread), new { id = threadId });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LockThread(int threadId)
    {
        var thread = await threadRepo.GetFirstOrDefaultAsync(t => t.Id == threadId);
        if (thread == null) return NotFound();

        thread.IsLocked = !thread.IsLocked;
        await threadRepo.UpdateAsync(thread);
        await UpdateAsync();

        TempData["Success"] = thread.IsLocked ? "Thread locked." : "Thread unlocked.";
        return RedirectToAction(nameof(Thread), new { id = threadId });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteThread(int threadId)
    {
        var thread = await threadRepo.GetFirstOrDefaultAsync(t => t.Id == threadId);
        if (thread == null) return NotFound();

        thread.IsDeleted = true;
        await threadRepo.UpdateAsync(thread);

        var forum = await forumRepo.GetFirstOrDefaultAsync(f => f.Id == thread.ForumId);
        if (forum != null)
        {
            forum.ThreadCount = Math.Max(0, forum.ThreadCount - 1);
            forum.PostCount = Math.Max(0, forum.PostCount - (thread.ReplyCount + 1));
            await forumRepo.UpdateAsync(forum);
        }

        await UpdateAsync();
        TempData["Success"] = "Thread deleted.";
        return RedirectToAction(nameof(Forum), new { id = thread.ForumId });
    }

    private async Task UpdateAsync()
    {
        var context = HttpContext.RequestServices.GetRequiredService<Infrastructure.UserDbContext>();
        await context.SaveChangesAsync();
    }
}