using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Infrastructure;

namespace MvcApp.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class InterestTagController(UserDbContext db) : Controller
    {
        public async Task<IActionResult> Index(string? category, string search = "", int page = 1, int pageSize = 30)
        {
            IQueryable<InterestTag> query = db.InterestTags.AsQueryable();

            if (!string.IsNullOrEmpty(category) && Enum.TryParse<InterestCategory>(category, out var cat))
                query = query.Where(t => t.Category == cat);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(t => t.Name.Contains(search));

            var total = await query.CountAsync();
            var tags = await query
                .OrderBy(t => t.Category).ThenBy(t => t.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.Search = search;
            ViewBag.Category = category;
            ViewBag.TotalCount = await db.InterestTags.CountAsync();
            ViewBag.UserCount = await db.UserInterestTags.CountAsync();

            return View(tags);
        }

        public IActionResult Create()
        {
            ViewBag.Categories = Enum.GetValues<InterestCategory>();
            return View(new InterestTag());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InterestTag model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "Name is required.");
                ViewBag.Categories = Enum.GetValues<InterestCategory>();
                return View(model);
            }

            var exists = await db.InterestTags.AnyAsync(t => t.Name == model.Name && t.Category == model.Category);
            if (exists)
            {
                ModelState.AddModelError("Name", "A tag with this name and category already exists.");
                ViewBag.Categories = Enum.GetValues<InterestCategory>();
                return View(model);
            }

            model.IsCurated = true;
            db.InterestTags.Add(model);
            await db.SaveChangesAsync();

            TempData["Message"] = $"Tag \"{model.Name}\" created.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var tag = await db.InterestTags.FindAsync(id.Value);
            if (tag == null) return NotFound();

            ViewBag.Categories = Enum.GetValues<InterestCategory>();
            return View(tag);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, InterestTag model)
        {
            if (id != model.Id) return NotFound();

            var tag = await db.InterestTags.FindAsync(id);
            if (tag == null) return NotFound();

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "Name is required.");
                ViewBag.Categories = Enum.GetValues<InterestCategory>();
                return View(model);
            }

            tag.Name = model.Name;
            tag.Category = model.Category;
            await db.SaveChangesAsync();

            TempData["Message"] = $"Tag \"{tag.Name}\" updated.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var tag = await db.InterestTags.FindAsync(id.Value);
            if (tag == null) return NotFound();

            ViewBag.UserCount = await db.UserInterestTags.CountAsync(ut => ut.TagId == tag.Id);
            return View(tag);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tag = await db.InterestTags.FindAsync(id);
            if (tag != null)
            {
                db.InterestTags.Remove(tag);
                await db.SaveChangesAsync();
                TempData["Message"] = $"Tag \"{tag.Name}\" deleted.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete(int[] ids)
        {
            var tags = await db.InterestTags.Where(t => ids.Contains(t.Id)).ToListAsync();
            db.InterestTags.RemoveRange(tags);
            await db.SaveChangesAsync();

            TempData["Message"] = $"{tags.Count} tag(s) deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
