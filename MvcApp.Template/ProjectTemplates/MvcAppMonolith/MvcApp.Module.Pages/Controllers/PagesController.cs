using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MvcApp.Common.Filters;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Localization;
using MvcApp.Module.Pages.Services;

namespace MvcApp.Module.Pages.Controllers;

[ModuleEnabledFilter("Pages")]
public class PagesController : PagesBaseController
{
    private readonly IRepository<ContentPage> _pageRepo;
    private readonly IPageRenderer _renderer;

    public PagesController(
        IRepository<ContentPage> pageRepo,
        IPageRenderer renderer,
        IStringLocalizer<SharedResource> localizer) : base(localizer)
    {
        _pageRepo = pageRepo;
        _renderer = renderer;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var pages = await _pageRepo.Query()
            .Where(p => p.IsPublished)
            .OrderBy(p => p.PublishedAt ?? p.CreatedAt)
            .ToListAsync();
        return View(pages);
    }

    [AllowAnonymous]
    [HttpGet("pages/{slug}")]
    public new async Task<IActionResult> View(string slug)
    {
        var page = await _pageRepo.Query()
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);
        if (page == null) return NotFound();

        string html;
        try
        {
            html = await _renderer.RenderAsync(page.Body, page, User);
        }
        catch (Exception ex)
        {
            html = $"<div class=\"alert alert-danger\">Page rendering failed: {ex.Message}</div>";
        }

        ViewData["RenderedBody"] = html;
        return View(page);
    }
}
