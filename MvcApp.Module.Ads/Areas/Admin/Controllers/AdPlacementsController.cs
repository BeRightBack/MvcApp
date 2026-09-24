using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MvcApp.Common.Filters;
using MvcApp.Module.Ads.Entities;
using MvcApp.Module.Ads.Services;

namespace MvcApp.Module.Ads.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
[ModuleEnabledFilter("Ads")]
[Route("Admin/Ads/Placements")]
public class AdPlacementsController : Controller
{
    private readonly IAdPlacementService _placementService;
    private readonly IAdZoneService _zoneService;

    public AdPlacementsController(IAdPlacementService placementService, IAdZoneService zoneService)
    {
        _placementService = placementService;
        _zoneService = zoneService;
    }

    // GET: Admin/Ads/Placements?zoneId=1
    [HttpGet("")]
    public async Task<IActionResult> Index(int? zoneId)
    {
        var zones = await _zoneService.GetAllAsync(true);
        ViewBag.Zones = new SelectList(zones, "Id", "Name", zoneId);
        ViewBag.SelectedZoneId = zoneId;

        if (zoneId.HasValue)
        {
            var placements = await _placementService.GetByZoneAsync(zoneId.Value, false);
            return View(placements);
        }
        return View(new List<AdPlacement>());
    }

    // GET: Admin/Ads/Placements/Create?zoneId=1
    [HttpGet("Create")]
    public async Task<IActionResult> Create(int? zoneId)
    {
        ViewData["Action"] = "Create";
        var zones = await _zoneService.GetAllAsync(true);
        ViewBag.Zones = new SelectList(zones, "Id", "Name", zoneId);
        return View(new AdPlacement { ZoneId = zoneId ?? 0, PageSlug = "*", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
    }

    // POST: Admin/Ads/Placements/Create
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdPlacement placement)
    {
        if (ModelState.IsValid)
        {
            await _placementService.CreateAsync(placement);
            TempData["Success"] = "Placement created successfully.";
            return RedirectToAction(nameof(Index), new { zoneId = placement.ZoneId });
        }
        var zones = await _zoneService.GetAllAsync(true);
        ViewBag.Zones = new SelectList(zones, "Id", "Name", placement.ZoneId);
        return View(placement);
    }

    // GET: Admin/Ads/Placements/Edit/5
    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var placement = await _placementService.GetByIdAsync(id);
        if (placement == null) return NotFound();

        var zones = await _zoneService.GetAllAsync(true);
        ViewBag.Zones = new SelectList(zones, "Id", "Name", placement.ZoneId);
        return View(placement);
    }

    // POST: Admin/Ads/Placements/Edit/5
    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdPlacement placement)
    {
        if (id != placement.Id) return NotFound();

        if (ModelState.IsValid)
        {
            await _placementService.UpdateAsync(placement);
            TempData["Success"] = "Placement updated successfully.";
            return RedirectToAction(nameof(Index), new { zoneId = placement.ZoneId });
        }
        var zones = await _zoneService.GetAllAsync(true);
        ViewBag.Zones = new SelectList(zones, "Id", "Name", placement.ZoneId);
        return View(placement);
    }

    // POST: Admin/Ads/Placements/Delete/5
    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int zoneId)
    {
        await _placementService.DeleteAsync(id);
        TempData["Success"] = "Placement deleted.";
        return RedirectToAction(nameof(Index), new { zoneId });
    }
}