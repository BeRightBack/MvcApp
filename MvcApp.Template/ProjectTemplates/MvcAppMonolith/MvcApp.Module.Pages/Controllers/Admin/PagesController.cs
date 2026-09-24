using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MvcApp.Common.Filters;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Localization;
using MvcApp.Module.Pages.Services;
using System.Text.RegularExpressions;

namespace MvcApp.Module.Pages.Controllers.Admin
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [ModuleEnabledFilter("Pages")]
    public class PagesController : PagesBaseController
    {
        private readonly IRepository<ContentPage> _pageRepo;
        private readonly IRepository<ContentPageSnippet> _snippetRepo;
        private readonly IPageRenderer _renderer;
        private readonly BlazorComponentRegistry _blazorRegistry;
        private readonly UserManager<UserDetails> _userManager;

        public PagesController(
            IRepository<ContentPage> pageRepo,
            IRepository<ContentPageSnippet> snippetRepo,
            IPageRenderer renderer,
            BlazorComponentRegistry blazorRegistry,
            UserManager<UserDetails> userManager,
            IStringLocalizer<SharedResource> localizer) : base(localizer)
        {
            _pageRepo = pageRepo;
            _snippetRepo = snippetRepo;
            _renderer = renderer;
            _blazorRegistry = blazorRegistry;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var pages = await _pageRepo.Query()
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return View(pages);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateEditorAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string title, string? slug, string body, bool isPublished)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
            {
                ModelState.AddModelError("", "Title and body are required.");
                await PopulateEditorAsync();
                return View();
            }

            var generatedSlug = string.IsNullOrWhiteSpace(slug) ? GenerateSlug(title) : slug.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(generatedSlug)) generatedSlug = "page";

            var existing = await _pageRepo.Query().AnyAsync(p => p.Slug == generatedSlug);
            if (existing) generatedSlug += "-" + Guid.NewGuid().ToString("N")[..6];

            var page = new ContentPage
            {
                Title = title.Trim(),
                Slug = generatedSlug,
                Body = body,
                CreatedById = currentUser.Id,
                CreatedByUsername = currentUser.UserName!,
                CreatedAt = DateTime.UtcNow,
                IsPublished = isPublished
            };

            if (isPublished) page.PublishedAt = DateTime.UtcNow;

            await _pageRepo.AddAsync(page);
            TempData["Success"] = "Page created.";
            return RedirectToAction(nameof(Edit), new { id = page.Id });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var page = await _pageRepo.GetByIdAsync(id);
            if (page == null) return NotFound();
            await PopulateEditorAsync();
            return View(page);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string title, string? slug, string body, bool isPublished)
        {
            var page = await _pageRepo.GetByIdAsync(id);
            if (page == null) return NotFound();

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
            {
                ModelState.AddModelError("", "Title and body are required.");
                await PopulateEditorAsync();
                return View(page);
            }

            var generatedSlug = string.IsNullOrWhiteSpace(slug) ? GenerateSlug(title) : slug.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(generatedSlug)) generatedSlug = "page";

            var existing = await _pageRepo.Query().AnyAsync(p => p.Slug == generatedSlug && p.Id != id);
            if (existing) generatedSlug += "-" + Guid.NewGuid().ToString("N")[..6];

            page.Title = title.Trim();
            page.Slug = generatedSlug;
            page.Body = body;
            page.UpdatedAt = DateTime.UtcNow;
            page.IsPublished = isPublished;

            if (isPublished && page.PublishedAt == null)
                page.PublishedAt = DateTime.UtcNow;

            await _pageRepo.UpdateAsync(page);
            TempData["Success"] = "Page updated.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var page = await _pageRepo.GetByIdAsync(id);
            if (page == null) return NotFound();
            await _pageRepo.DeleteAsync(page);
            TempData["Success"] = "Page deleted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Preview(string? body, string? slug)
        {
            var page = new ContentPage
            {
                Title = slug ?? "Preview",
                Slug = slug ?? "preview",
                Body = body ?? string.Empty,
                IsPublished = true
            };

            string html;
            try
            {
                html = await _renderer.RenderAsync(page.Body, page, User);
            }
            catch (Exception ex)
            {
                html = $"<div class=\"alert alert-danger\">Preview rendering failed: {ex.Message}</div>";
            }

            ViewData["RenderedBody"] = html;
            return View("Preview", page);
        }

        private async Task PopulateEditorAsync()
        {
            var snippets = await _snippetRepo.Query()
                .OrderBy(s => s.Category).ThenBy(s => s.Name)
                .ToListAsync();
            ViewBag.PageSnippets = snippets;
            ViewBag.PageComponents = PageComponentCatalog.Items;
            ViewBag.PageBlazorComponents = _blazorRegistry.Components;
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
