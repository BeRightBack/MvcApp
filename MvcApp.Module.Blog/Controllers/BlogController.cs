using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MvcApp.Core;
using MvcApp.Common.Filters;
using MvcApp.Core.Abstractions;
using MvcApp.Infrastructure;
using System.Text.RegularExpressions;

namespace MvcApp.Module.Blog.Controllers;

[ModuleEnabledFilter("Blog")]
public partial class BlogController(
    UserManager<UserDetails> userManager,
    IRepository<BlogPost> postRepo,
    IRepository<BlogCategory> categoryRepo,
    IRepository<BlogTag> tagRepo,
    IRepository<BlogComment> commentRepo,
    IBanService banService) : Controller
{
    [AllowAnonymous]
    public async Task<IActionResult> Index(int page = 1)
    {
        const int pageSize = 10;
        var postsQuery = postRepo.Query().Where(p => p.IsPublished)
            .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt);

        var totalPosts = await postsQuery.CountAsync();
        var totalPages = (int)Math.Ceiling(totalPosts / (double)pageSize);
        var pagedPosts = await postsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var categories = await categoryRepo.Query().OrderBy(c => c.Name).ToListAsync();
        var tags = await tagRepo.Query().OrderBy(t => t.Name).ToListAsync();
        var recentPosts = await postRepo.Query()
            .Where(p => p.IsPublished)
            .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
            .Take(5)
            .ToListAsync();

        ViewBag.Categories = categories;
        ViewBag.Tags = tags;
        ViewBag.RecentPosts = recentPosts;
        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;

        var db = HttpContext.RequestServices.GetRequiredService<UserDbContext>();
        foreach (var post in pagedPosts)
        {
            post.Category = await db.BlogCategories.FindAsync(post.CategoryId);
        }

        return View(pagedPosts);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Post(string slug)
    {
        var post = await postRepo.Query()
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);
        if (post == null) return NotFound();

        post.ViewCount++;
        await postRepo.UpdateAsync(post);

        var categories = await categoryRepo.Query().OrderBy(c => c.Name).ToListAsync();
        var tags = await tagRepo.Query().OrderBy(t => t.Name).ToListAsync();
        var recentPosts = await postRepo.Query()
            .Where(p => p.IsPublished)
            .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
            .Take(5)
            .ToListAsync();

        ViewBag.Categories = categories;
        ViewBag.Tags = tags;
        ViewBag.RecentPosts = recentPosts;
        ViewBag.CurrentUserId = (await userManager.GetUserAsync(User))?.Id;

        var db = HttpContext.RequestServices.GetRequiredService<UserDbContext>();
        post.Category = await db.BlogCategories.FindAsync(post.CategoryId);
        post.PostTags = await db.BlogPostTags.Where(pt => pt.PostId == post.Id).Include(pt => pt.Tag).ToListAsync();

        var comments = await commentRepo.Query()
            .Where(c => c.PostId == post.Id && c.IsApproved && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        ViewBag.Comments = comments;
        return View(post);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Category(string slug, int page = 1)
    {
        var category = await categoryRepo.Query().FirstOrDefaultAsync(c => c.Slug == slug);
        if (category == null) return NotFound();

        const int pageSize = 10;
        var postsQuery = postRepo.Query()
            .Where(p => p.IsPublished && p.CategoryId == category.Id)
            .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt);

        var totalPosts = await postsQuery.CountAsync();
        var totalPages = (int)Math.Ceiling(totalPosts / (double)pageSize);
        var pagedPosts = await postsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var allCategories = await categoryRepo.Query().OrderBy(c => c.Name).ToListAsync();
        var tags = await tagRepo.Query().OrderBy(t => t.Name).ToListAsync();
        var recentPosts = await postRepo.Query()
            .Where(p => p.IsPublished)
            .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
            .Take(5)
            .ToListAsync();

        ViewBag.Category = category;
        ViewBag.Categories = allCategories;
        ViewBag.Tags = tags;
        ViewBag.RecentPosts = recentPosts;
        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;

        return View(pagedPosts);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Tag(string slug, int page = 1)
    {
        var tag = await tagRepo.Query().FirstOrDefaultAsync(t => t.Slug == slug);
        if (tag == null) return NotFound();

        const int pageSize = 10;
        var db = HttpContext.RequestServices.GetRequiredService<UserDbContext>();
        var postIds = await db.BlogPostTags.Where(pt => pt.TagId == tag.Id).Select(pt => pt.PostId).ToListAsync();
        var postsQuery = postRepo.Query()
            .Where(p => p.IsPublished && postIds.Contains(p.Id))
            .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt);

        var totalPosts = await postsQuery.CountAsync();
        var totalPages = (int)Math.Ceiling(totalPosts / (double)pageSize);
        var pagedPosts = await postsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var categories = await categoryRepo.Query().OrderBy(c => c.Name).ToListAsync();
        var tags = await tagRepo.Query().OrderBy(t => t.Name).ToListAsync();
        var recentPosts = await postRepo.Query()
            .Where(p => p.IsPublished)
            .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
            .Take(5)
            .ToListAsync();

        ViewBag.Tag = tag;
        ViewBag.Categories = categories;
        ViewBag.Tags = tags;
        ViewBag.RecentPosts = recentPosts;
        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;

        return View(pagedPosts);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Search(string q, int page = 1)
    {
        if (string.IsNullOrWhiteSpace(q))
            return RedirectToAction(nameof(Index));

        const int pageSize = 10;
        var query = q.Trim().ToLower();
        var postsQuery = postRepo.Query()
            .Where(p => p.IsPublished &&
                (p.Title.ToLower().Contains(query) ||
                 p.Content.ToLower().Contains(query) ||
                 (p.Excerpt ?? "").ToLower().Contains(query)))
            .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt);

        var totalPosts = await postsQuery.CountAsync();
        var totalPages = (int)Math.Ceiling(totalPosts / (double)pageSize);
        var pagedPosts = await postsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var categories = await categoryRepo.Query().OrderBy(c => c.Name).ToListAsync();
        var tags = await tagRepo.Query().OrderBy(t => t.Name).ToListAsync();
        var recentPosts = await postRepo.Query()
            .Where(p => p.IsPublished)
            .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
            .Take(5)
            .ToListAsync();

        ViewBag.SearchQuery = q;
        ViewBag.Categories = categories;
        ViewBag.Tags = tags;
        ViewBag.RecentPosts = recentPosts;
        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;

        return View(pagedPosts);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Archive(int? year, int? month, int page = 1)
    {
        const int pageSize = 10;
        var postsQuery = postRepo.Query().Where(p => p.IsPublished);

        if (year.HasValue)
            postsQuery = postsQuery.Where(p => (p.PublishedAt ?? p.CreatedAt).Year == year.Value);
        if (month.HasValue)
            postsQuery = postsQuery.Where(p => (p.PublishedAt ?? p.CreatedAt).Month == month.Value);

        postsQuery = postsQuery.OrderByDescending(p => p.PublishedAt ?? p.CreatedAt);

        var totalPosts = await postsQuery.CountAsync();
        var totalPages = (int)Math.Ceiling(totalPosts / (double)pageSize);
        var pagedPosts = await postsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var archiveMonths = await postRepo.Query()
            .Where(p => p.IsPublished)
            .GroupBy(p => new { y = (p.PublishedAt ?? p.CreatedAt).Year, m = (p.PublishedAt ?? p.CreatedAt).Month })
            .OrderByDescending(g => g.Key.y).ThenByDescending(g => g.Key.m)
            .Select(g => new { g.Key.y, g.Key.m, Count = g.Count() })
            .ToListAsync();

        ViewBag.ArchiveYear = year;
        ViewBag.ArchiveMonth = month;
        ViewBag.ArchiveMonths = archiveMonths;
        ViewBag.Categories = await categoryRepo.Query().OrderBy(c => c.Name).ToListAsync();
        ViewBag.Tags = await tagRepo.Query().OrderBy(t => t.Name).ToListAsync();
        ViewBag.RecentPosts = await postRepo.Query()
            .Where(p => p.IsPublished).OrderByDescending(p => p.PublishedAt ?? p.CreatedAt).Take(5).ToListAsync();
        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;

        return View(pagedPosts);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int postId, string content, string? guestName, string? guestEmail)
    {
        var post = await postRepo.Query().FirstOrDefaultAsync(p => p.Id == postId && p.IsPublished);
        if (post == null) return NotFound();

        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["Error"] = "Comment cannot be empty.";
            return RedirectToAction(nameof(Post), "Blog", new { slug = post.Slug });
        }

        var currentUser = await userManager.GetUserAsync(User);

        if (currentUser != null && await banService.IsBannedAsync(currentUser.Id))
        {
            TempData["Error"] = "Your account is suspended. You cannot post comments.";
            return RedirectToAction(nameof(Post), "Blog", new { slug = post.Slug });
        }

        var comment = new BlogComment
        {
            PostId = postId,
            Content = content,
            CreatedById = currentUser?.Id,
            CreatedByUsername = currentUser?.UserName,
            GuestName = currentUser == null ? guestName : null,
            GuestEmail = currentUser == null ? guestEmail : null,
            IsApproved = currentUser != null
        };

        await commentRepo.AddAsync(comment);
        TempData["Success"] = currentUser != null
            ? "Comment posted."
            : "Comment submitted for moderation.";
        return RedirectToAction(nameof(Post), "Blog", new { slug = post.Slug });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int id)
    {
        var currentUser = await userManager.GetUserAsync(User);
        if (currentUser == null) return Challenge();

        var comment = await commentRepo.GetFirstOrDefaultAsync(c => c.Id == id);
        if (comment == null) return NotFound();

        var isAdmin = User.IsInRole("Admin");
        if (comment.CreatedById != currentUser.Id && !isAdmin)
            return Forbid();

        comment.IsDeleted = true;
        await commentRepo.UpdateAsync(comment);

        var post = await postRepo.Query().FirstOrDefaultAsync(p => p.Id == comment.PostId);
        TempData["Success"] = "Comment deleted.";
        return RedirectToAction(nameof(Post), "Blog", new { slug = post?.Slug });
    }

    private static string GenerateSlug(string title)
    {
        var slug = title.ToLowerInvariant().Trim();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        return slug.TrimStart('-').TrimEnd('-');
    }
}