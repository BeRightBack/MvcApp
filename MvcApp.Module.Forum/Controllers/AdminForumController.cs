using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Common.Filters;
using MvcApp.Core.Abstractions;
using MvcApp.Infrastructure;

namespace MvcApp.Module.Forum.Controllers.Areas.Admin
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [ModuleEnabledFilter("Forum")]
    public class ForumController : Controller
    {
        private readonly UserDbContext _db;
        private readonly IRepository<ForumCategory> _categoryRepo;
        private readonly IRepository<MvcApp.Core.Forum> _forumRepo;
        private readonly IRepository<ForumThread> _threadRepo;

        public ForumController(
            UserDbContext db,
            IRepository<ForumCategory> categoryRepo,
            IRepository<MvcApp.Core.Forum> forumRepo,
            IRepository<ForumThread> threadRepo)
        {
            _db = db;
            _categoryRepo = categoryRepo;
            _forumRepo = forumRepo;
            _threadRepo = threadRepo;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _db.Set<ForumCategory>()
                .OrderBy(c => c.SortOrder)
                .Include(c => c.Forums.OrderBy(f => f.SortOrder))
                .ToListAsync();
            return View(categories);
        }

        public IActionResult CreateCategory()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(string name, string? description, int sortOrder)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError("", "Name is required.");
                return View();
            }

            var category = new ForumCategory
            {
                Name = name.Trim(),
                Description = description?.Trim(),
                SortOrder = sortOrder
            };
            await _categoryRepo.AddAsync(category);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Category created.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _categoryRepo.GetByIdAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, string name, string? description, int sortOrder)
        {
            var category = await _categoryRepo.GetByIdAsync(id);
            if (category == null) return NotFound();

            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError("", "Name is required.");
                return View(category);
            }

            category.Name = name.Trim();
            category.Description = description?.Trim();
            category.SortOrder = sortOrder;
            await _categoryRepo.UpdateAsync(category);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Category updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _categoryRepo.GetByIdAsync(id);
            if (category == null) return NotFound();
            await _categoryRepo.DeleteAsync(category);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Category deleted.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult CreateForum(int categoryId)
        {
            ViewBag.CategoryId = categoryId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateForum(int categoryId, string name, string? description, int sortOrder)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError("", "Name is required.");
                ViewBag.CategoryId = categoryId;
                return View();
            }

            var category = await _categoryRepo.GetByIdAsync(categoryId);
            if (category == null) return NotFound();

            var forum = new MvcApp.Core.Forum
            {
                CategoryId = categoryId,
                Name = name.Trim(),
                Description = description?.Trim(),
                SortOrder = sortOrder
            };
            await _forumRepo.AddAsync(forum);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Forum created.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> EditForum(int id)
        {
            var forum = await _forumRepo.GetByIdAsync(id);
            if (forum == null) return NotFound();

            var categories = await _db.Set<ForumCategory>().OrderBy(c => c.SortOrder).ToListAsync();
            ViewBag.Categories = categories;
            return View(forum);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditForum(int id, string name, string? description, int sortOrder, int categoryId)
        {
            var forum = await _forumRepo.GetByIdAsync(id);
            if (forum == null) return NotFound();

            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError("", "Name is required.");
                var categories = await _db.Set<ForumCategory>().OrderBy(c => c.SortOrder).ToListAsync();
                ViewBag.Categories = categories;
                return View(forum);
            }

            forum.Name = name.Trim();
            forum.Description = description?.Trim();
            forum.SortOrder = sortOrder;
            forum.CategoryId = categoryId;
            await _forumRepo.UpdateAsync(forum);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Forum updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteForum(int id)
        {
            var forum = await _forumRepo.GetByIdAsync(id);
            if (forum == null) return NotFound();
            await _forumRepo.DeleteAsync(forum);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Forum deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
