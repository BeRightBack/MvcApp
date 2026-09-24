using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MvcApp.Common.Filters;
using MvcApp.Module.Ads.Entities;
using MvcApp.Module.Ads.Services;

namespace MvcApp.Module.Ads.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
[ModuleEnabledFilter("Ads")]
[Route("Admin/Ads/Banners")]
public class AdBannersController : Controller
{
    private readonly IAdBannerService _bannerService;
    private readonly IAdZoneService _zoneService;
    private readonly IAdMediaService _mediaService;

    public AdBannersController(IAdBannerService bannerService, IAdZoneService zoneService, IAdMediaService mediaService)
    {
        _bannerService = bannerService;
        _zoneService = zoneService;
        _mediaService = mediaService;
    }

    // GET: Admin/Ads/Banners?zoneId=1
    [HttpGet("")]
    public async Task<IActionResult> Index(int? zoneId)
    {
        var zones = await _zoneService.GetAllAsync(false);
        ViewBag.Zones = new SelectList(zones, "Id", "Name", zoneId);
        ViewBag.SelectedZoneId = zoneId;

        if (zoneId.HasValue)
        {
            var banners = await _bannerService.GetAllAsync(zoneId.Value, true);
            return View(banners);
        }
        return View(new List<AdBanner>());
    }

    // GET: Admin/Ads/Banners/Create?zoneId=1
    [HttpGet("Create")]
    public async Task<IActionResult> Create(int? zoneId)
    {
        ViewData["Action"] = "Create";
        await PopulateFormListsAsync(zoneId);
        return View(new AdBanner { ZoneId = zoneId ?? 0, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
    }

    // POST: Admin/Ads/Banners/Create
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdBanner banner, IFormFile? imageFile)
    {
        await HandleUploadAsync(banner, imageFile);

        if (ModelState.IsValid)
        {
            await _bannerService.CreateAsync(banner);
            TempData["Success"] = "Banner created successfully.";
            return RedirectToAction(nameof(Index), new { zoneId = banner.ZoneId });
        }
        await PopulateFormListsAsync(banner.ZoneId);
        return View(banner);
    }

    // GET: Admin/Ads/Banners/Edit/5
    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var banner = await _bannerService.GetByIdAsync(id);
        if (banner == null) return NotFound();

        await PopulateFormListsAsync(banner.ZoneId);
        return View(banner);
    }

    // POST: Admin/Ads/Banners/Edit/5
    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdBanner banner, IFormFile? imageFile)
    {
        if (id != banner.Id) return NotFound();

        await HandleUploadAsync(banner, imageFile);

        if (ModelState.IsValid)
        {
            await _bannerService.UpdateAsync(banner);
            TempData["Success"] = "Banner updated successfully.";
            return RedirectToAction(nameof(Index), new { zoneId = banner.ZoneId });
        }
        await PopulateFormListsAsync(banner.ZoneId);
        return View(banner);
    }

    // POST: Admin/Ads/Banners/Delete/5
    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int zoneId)
    {
        var banner = await _bannerService.GetByIdAsync(id);
        if (banner != null)
        {
            _mediaService.DeleteBannerImage(banner.Content);
            await _bannerService.DeleteAsync(id);
        }
        TempData["Success"] = "Banner deleted.";
        return RedirectToAction(nameof(Index), new { zoneId });
    }

    // POST: Admin/Ads/Banners/Reorder
    [HttpPost("Reorder")]
    public async Task<IActionResult> Reorder(int zoneId, [FromBody] int[] bannerIdsInOrder)
    {
        await _bannerService.ReorderAsync(zoneId, bannerIdsInOrder);
        return Ok();
    }

    /// <summary>
    /// When an image file is uploaded for an Image banner, validate it against the zone's
    /// declared format and save it under /images/ads/{zoneKey}/, replacing the Content URL.
    /// </summary>
    private async Task HandleUploadAsync(AdBanner banner, IFormFile? imageFile)
    {
        if (banner.Type != AdBannerType.Image || imageFile is null || imageFile.Length == 0)
            return;

        var zone = await _zoneService.GetByIdAsync(banner.ZoneId);
        if (zone is null)
        {
            ModelState.AddModelError(nameof(banner.ZoneId), "Select a zone before uploading.");
            return;
        }

        var result = await _mediaService.SaveBannerImageAsync(imageFile, zone.Key, zone.BannerWidth, zone.BannerHeight);
        if (!result.Success)
        {
            ModelState.AddModelError("imageFile", result.Error!);
            return;
        }

        if (!string.Equals(banner.Content, result.Url, StringComparison.OrdinalIgnoreCase))
            _mediaService.DeleteBannerImage(banner.Content); // replace previously uploaded file
        banner.Content = result.Url;
    }

    private async Task PopulateFormListsAsync(int? zoneId)
    {
        var zones = await _zoneService.GetAllAsync(true);
        ViewBag.Zones = new SelectList(zones, "Id", "Name", zoneId);
        ViewBag.BannerTypes = Enum.GetValues(typeof(AdBannerType)).Cast<AdBannerType>().Select(t => new SelectListItem { Text = t.ToString(), Value = ((int)t).ToString() });

        var zone = zoneId.HasValue ? zones.FirstOrDefault(z => z.Id == zoneId.Value) : null;
        ViewBag.ZoneFormat = zone?.Format;
    }
}
