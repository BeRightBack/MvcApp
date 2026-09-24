using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Common.Filters;
using MvcApp.Core;
using MvcApp.Core.Abstractions;

namespace MvcApp.Module.Pages.Controllers.Admin
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [ModuleEnabledFilter("Pages")]
    public class SnippetsController : Controller
    {
        private readonly IRepository<ContentPageSnippet> _repo;

        public SnippetsController(IRepository<ContentPageSnippet> repo)
        {
            _repo = repo;
        }

        public async Task<IActionResult> Index()
        {
            var snippets = await _repo.Query()
                .OrderBy(s => s.Category).ThenBy(s => s.Name)
                .ToListAsync();
            return View(snippets);
        }

        public IActionResult Create()
        {
            return View(new ContentPageSnippet());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ContentPageSnippet snippet)
        {
            if (string.IsNullOrWhiteSpace(snippet.Name) || string.IsNullOrWhiteSpace(snippet.Content))
            {
                ModelState.AddModelError("", "Name and content are required.");
                return View(snippet);
            }

            snippet.Name = snippet.Name.Trim();
            snippet.Content = snippet.Content.Trim();
            if (string.IsNullOrWhiteSpace(snippet.Category)) snippet.Category = "General";
            snippet.CreatedAt = DateTime.UtcNow;

            await _repo.AddAsync(snippet);
            TempData["Success"] = "Snippet created.";
            return RedirectToAction(nameof(Edit), new { id = snippet.Id });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var snippet = await _repo.GetByIdAsync(id);
            if (snippet == null) return NotFound();
            return View(snippet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ContentPageSnippet snippet)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return NotFound();

            if (string.IsNullOrWhiteSpace(snippet.Name) || string.IsNullOrWhiteSpace(snippet.Content))
            {
                ModelState.AddModelError("", "Name and content are required.");
                snippet.Id = id;
                return View(snippet);
            }

            existing.Name = snippet.Name.Trim();
            existing.Description = snippet.Description;
            existing.Category = string.IsNullOrWhiteSpace(snippet.Category) ? "General" : snippet.Category.Trim();
            existing.Content = snippet.Content.Trim();
            existing.UpdatedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(existing);
            TempData["Success"] = "Snippet updated.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var snippet = await _repo.GetByIdAsync(id);
            if (snippet == null) return NotFound();
            await _repo.DeleteAsync(snippet);
            TempData["Success"] = "Snippet deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
