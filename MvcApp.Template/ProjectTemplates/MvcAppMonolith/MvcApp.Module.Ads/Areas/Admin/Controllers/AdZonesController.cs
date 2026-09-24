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
[Route("Admin/Ads/Zones")]
public class AdZonesController : Controller
{
    private readonly IAdZoneService _zoneService;

    public AdZonesController(IAdZoneService zoneService) => _zoneService = zoneService;

    // GET: Admin/Ads/Zones
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var zones = await _zoneService.GetAllAsync(false);
        return View(zones);
    }

    // GET: Admin/Ads/Zones/Create
    [HttpGet("Create")]
    public IActionResult Create()
    {
        ViewData["Action"] = "Create";
        return View(new AdZone { IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
    }

    // POST: Admin/Ads/Zones/Create
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdZone zone)
    {
        if (await _zoneService.ExistsKeyAsync(zone.Key))
            ModelState.AddModelError(nameof(zone.Key), "Zone key already exists.");

        if (ModelState.IsValid)
        {
            await _zoneService.CreateAsync(zone);
            TempData["Success"] = "Zone created successfully.";
            return RedirectToAction(nameof(Index));
        }
        return View(zone);
    }

    // GET: Admin/Ads/Zones/Edit/5
    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var zone = await _zoneService.GetByIdAsync(id);
        if (zone == null) return NotFound();
        return View(zone);
    }

    // POST: Admin/Ads/Zones/Edit/5
    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdZone zone)
    {
        if (id != zone.Id) return NotFound();

        if (await _zoneService.ExistsKeyAsync(zone.Key, id))
            ModelState.AddModelError(nameof(zone.Key), "Zone key already exists.");

        if (ModelState.IsValid)
        {
            await _zoneService.UpdateAsync(zone);
            TempData["Success"] = "Zone updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        return View(zone);
    }

    // POST: Admin/Ads/Zones/Delete/5
    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _zoneService.DeleteAsync(id);
        TempData["Success"] = "Zone deleted.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Admin/Ads/Zones/Reorder
    [HttpPost("Reorder")]
    public async Task<IActionResult> Reorder([FromBody] int[] zoneIdsInOrder)
    {
        // Could implement display order update here
        return Ok();
    }
}