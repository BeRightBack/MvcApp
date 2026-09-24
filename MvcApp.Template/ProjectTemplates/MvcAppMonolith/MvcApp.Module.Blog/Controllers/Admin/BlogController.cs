using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Common.Filters;
using MvcApp.Core.Abstractions;
using MvcApp.Infrastructure;
using System.Text.RegularExpressions;

namespace MvcApp.Module.Blog.Controllers.Admin
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [ModuleEnabledFilter("Blog")]
    public class BlogController : Controller
    {
        private readonly UserDbContext _db;
        private readonly UserManager<UserDetails> _userManager;
        private readonly IRepository<BlogPost> _postRepo;
        private readonly IRepository<BlogCategory> _categoryRepo;
        private readonly IRepository<BlogTag> _tagRepo;
        private readonly IRepository<BlogComment> _commentRepo;

        public BlogController(
            UserDbContext db,
            UserManager<UserDetails> userManager,
            IRepository<BlogPost> postRepo,
            IRepository<BlogCategory> categoryRepo,
            IRepository<BlogTag> tagRepo,
            IRepository<BlogComment> commentRepo)
        {
            _db = db;
            _userManager = userManager;
            _postRepo = postRepo;
            _categoryRepo = categoryRepo;
            _tagRepo = tagRepo;
            _commentRepo = commentRepo;
        }

        public async Task<IActionResult> Index()
        {
            var posts = await _postRepo.Query()
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var categories = await _db.BlogCategories.ToListAsync();
            ViewBag.Categories = categories;
            return View(posts);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _db.BlogCategories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Tags = await _db.BlogTags.OrderBy(t => t.Name).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string title, string? excerpt, string content, bool isPublished, bool isFeatured,
            int? categoryId, string? tagIds, IFormFile? featuredImage)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            {
                ModelState.AddModelError("", "Title and content are required.");
                ViewBag.Categories = await _db.BlogCategories.OrderBy(c => c.Name).ToListAsync();
                ViewBag.Tags = await _db.BlogTags.OrderBy(t => t.Name).ToListAsync();
                return View();
            }

            var slug = GenerateSlug(title);
            if (string.IsNullOrWhiteSpace(slug)) slug = "post";

            var existing = await _postRepo.Query().AnyAsync(p => p.Slug == slug);
            if (existing) slug += "-" + Guid.NewGuid().ToString("N")[..6];

            var post = new BlogPost
            {
                Title = title.Trim(),
                Slug = slug,
                Excerpt = excerpt?.Trim(),
                Content = content,
                CreatedById = currentUser.Id,
                CreatedByUsername = currentUser.UserName!,
                CreatedAt = DateTime.UtcNow,
                IsPublished = isPublished,
                IsFeatured = isFeatured,
                CategoryId = categoryId
            };

            if (isPublished) post.PublishedAt = DateTime.UtcNow;

            await _postRepo.AddAsync(post);

            if (!string.IsNullOrWhiteSpace(tagIds))
            {
                foreach (var tid in tagIds.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(tid, out var tagId))
                    {
                        _db.BlogPostTags.Add(new BlogPostTag { PostId = post.Id, TagId = tagId });
                    }
                }
                await _db.SaveChangesAsync();
            }

            TempData["Success"] = "Post created.";
            return RedirectToAction(nameof(Edit), new { id = post.Id });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var post = await _postRepo.GetByIdAsync(id);
            if (post == null) return NotFound();

            ViewBag.Categories = await _db.BlogCategories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Tags = await _db.BlogTags.OrderBy(t => t.Name).ToListAsync();
            ViewBag.SelectedTagIds = await _db.BlogPostTags.Where(pt => pt.PostId == id).Select(pt => pt.TagId).ToListAsync();
            return View(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, string title, string? excerpt, string content, bool isPublished, bool isFeatured,
            int? categoryId, string? tagIds, IFormFile? featuredImage)
        {
            var post = await _postRepo.GetByIdAsync(id);
            if (post == null) return NotFound();

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            {
                ModelState.AddModelError("", "Title and content are required.");
                ViewBag.Categories = await _db.BlogCategories.OrderBy(c => c.Name).ToListAsync();
                ViewBag.Tags = await _db.BlogTags.OrderBy(t => t.Name).ToListAsync();
                return View(post);
            }

            var slug = GenerateSlug(title);
            if (string.IsNullOrWhiteSpace(slug)) slug = "post";

            var existing = await _postRepo.Query().AnyAsync(p => p.Slug == slug && p.Id != id);
            if (existing) slug += "-" + Guid.NewGuid().ToString("N")[..6];

            post.Title = title.Trim();
            post.Slug = slug;
            post.Excerpt = excerpt?.Trim();
            post.Content = content;
            post.UpdatedAt = DateTime.UtcNow;
            post.IsPublished = isPublished;
            post.IsFeatured = isFeatured;
            post.CategoryId = categoryId;

            if (isPublished && post.PublishedAt == null)
                post.PublishedAt = DateTime.UtcNow;

            await _postRepo.UpdateAsync(post);

            var oldTags = await _db.BlogPostTags.Where(pt => pt.PostId == id).ToListAsync();
            _db.BlogPostTags.RemoveRange(oldTags);

            if (!string.IsNullOrWhiteSpace(tagIds))
            {
                foreach (var tid in tagIds.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(tid, out var tagId))
                    {
                        _db.BlogPostTags.Add(new BlogPostTag { PostId = id, TagId = tagId });
                    }
                }
            }
            await _db.SaveChangesAsync();

            TempData["Success"] = "Post updated.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var post = await _postRepo.GetByIdAsync(id);
            if (post == null) return NotFound();
            await _postRepo.DeleteAsync(post);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Post deleted.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Categories()
        {
            var categories = await _db.BlogCategories.OrderBy(c => c.Name).ToListAsync();
            return View(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(string name, string? description)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Name is required.";
                return RedirectToAction(nameof(Categories));
            }

            var slug = GenerateSlug(name);
            if (string.IsNullOrWhiteSpace(slug)) slug = "category";

            var existing = await _db.BlogCategories.AnyAsync(c => c.Slug == slug);
            if (existing) slug += "-" + Guid.NewGuid().ToString("N")[..6];

            _db.BlogCategories.Add(new BlogCategory
            {
                Name = name.Trim(),
                Slug = slug,
                Description = description?.Trim()
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Category created.";
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, string name, string? description)
        {
            var category = await _db.BlogCategories.FindAsync(id);
            if (category == null) return NotFound();

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Name is required.";
                return RedirectToAction(nameof(Categories));
            }

            category.Name = name.Trim();
            category.Description = description?.Trim();
            await _db.SaveChangesAsync();
            TempData["Success"] = "Category updated.";
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _db.BlogCategories.FindAsync(id);
            if (category == null) return NotFound();
            _db.BlogCategories.Remove(category);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Category deleted.";
            return RedirectToAction(nameof(Categories));
        }

        public async Task<IActionResult> Tags()
        {
            var tags = await _db.BlogTags.OrderBy(t => t.Name).ToListAsync();
            return View(tags);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTag(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Name is required.";
                return RedirectToAction(nameof(Tags));
            }

            var slug = GenerateSlug(name);
            if (string.IsNullOrWhiteSpace(slug)) slug = "tag";

            var existing = await _db.BlogTags.AnyAsync(t => t.Slug == slug);
            if (existing) slug += "-" + Guid.NewGuid().ToString("N")[..6];

            _db.BlogTags.Add(new BlogTag
            {
                Name = name.Trim(),
                Slug = slug
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Tag created.";
            return RedirectToAction(nameof(Tags));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTag(int id)
        {
            var tag = await _db.BlogTags.FindAsync(id);
            if (tag == null) return NotFound();
            _db.BlogTags.Remove(tag);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Tag deleted.";
            return RedirectToAction(nameof(Tags));
        }

        public async Task<IActionResult> Comments()
        {
            var comments = await _db.BlogComments
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            var postIds = comments.Select(c => c.PostId).Distinct().ToList();
            var posts = await _db.BlogPosts.Where(p => postIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Title);
            ViewBag.PostTitles = posts;
            return View(comments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveComment(int id)
        {
            var comment = await _db.BlogComments.FindAsync(id);
            if (comment == null) return NotFound();
            comment.IsApproved = true;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Comment approved.";
            return RedirectToAction(nameof(Comments));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _db.BlogComments.FindAsync(id);
            if (comment == null) return NotFound();
            _db.BlogComments.Remove(comment);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Comment deleted.";
            return RedirectToAction(nameof(Comments));
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
}
