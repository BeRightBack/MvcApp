using Microsoft.EntityFrameworkCore;
using MvcApp.Core.Abstractions;
using MvcApp.Module.Ads.Entities;

namespace MvcApp.Module.Ads.Services;

public interface IAdPlacementService
{
    Task<AdPlacement?> GetByIdAsync(int id);
    Task<IReadOnlyList<AdPlacement>> GetByZoneAsync(int zoneId, bool activeOnly = true);
    Task<AdPlacement?> GetForPageAsync(int zoneId, string pageSlug);
    Task<AdPlacement> CreateAsync(AdPlacement placement);
    Task<AdPlacement> UpdateAsync(AdPlacement placement);
    Task DeleteAsync(int id);
    Task<bool> IsZoneExcludedAsync(int zoneId, string pageSlug, bool isLandingPage);
    Task<int?> GetMaxBannersForPageAsync(int zoneId, string pageSlug);
    Task<string?> GetCssClassForPageAsync(int zoneId, string pageSlug);
    Task<string?> GetWrapperTemplateForPageAsync(int zoneId, string pageSlug);
}

public class AdPlacementService : IAdPlacementService
{
    private readonly IRepository<AdPlacement> _repo;

    public AdPlacementService(IRepository<AdPlacement> repo) => _repo = repo;

    public async Task<AdPlacement?> GetByIdAsync(int id)
        => await _repo.GetFirstOrDefaultAsync(p => p.Id == id);

    public async Task<IReadOnlyList<AdPlacement>> GetByZoneAsync(int zoneId, bool activeOnly = true)
    {
        var q = _repo.Query().Where(p => p.ZoneId == zoneId);
        if (activeOnly) q = q.Where(p => p.IsActive);
        return await q.OrderBy(p => p.DisplayOrder).ThenBy(p => p.PageSlug).ToListAsync();
    }

    public async Task<AdPlacement?> GetForPageAsync(int zoneId, string pageSlug)
    {
        // First check for exact page match, then wildcard
        return await _repo.Query()
            .Where(p => p.ZoneId == zoneId && p.IsActive &&
                       (p.PageSlug == pageSlug || p.PageSlug == "*"))
            .OrderByDescending(p => p.PageSlug == pageSlug ? 1 : 0) // Exact match first
            .ThenBy(p => p.DisplayOrder)
            .FirstOrDefaultAsync();
    }

    public async Task<AdPlacement> CreateAsync(AdPlacement placement)
    {
        placement.CreatedAt = DateTime.UtcNow;
        placement.UpdatedAt = DateTime.UtcNow;
        await _repo.AddAsync(placement);
        return placement;
    }

    public async Task<AdPlacement> UpdateAsync(AdPlacement placement)
    {
        placement.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(placement);
        return placement;
    }

    public async Task DeleteAsync(int id)
    {
        var placement = await _repo.GetFirstOrDefaultAsync(p => p.Id == id);
        if (placement != null) await _repo.DeleteAsync(placement);
    }

    public async Task<bool> IsZoneExcludedAsync(int zoneId, string pageSlug, bool isLandingPage)
    {
        // Check zone-level exclusion
        var zone = await _repo.Query()
            .Where(p => p.ZoneId == zoneId && p.IsActive)
            .Select(p => new { p.IsExclusion, p.PageSlug, p.Zone!.ExcludeFromLandingPage })
            .FirstOrDefaultAsync(); // This won't work - need zone repo

        // We'll handle this in AdRenderer with zone info
        return false; // Placeholder - actual logic in AdRenderer
    }

    public async Task<int?> GetMaxBannersForPageAsync(int zoneId, string pageSlug)
    {
        var placement = await GetForPageAsync(zoneId, pageSlug);
        return placement?.MaxBannersOverride;
    }

    public async Task<string?> GetCssClassForPageAsync(int zoneId, string pageSlug)
    {
        var placement = await GetForPageAsync(zoneId, pageSlug);
        return placement?.CssClassOverride;
    }

    public async Task<string?> GetWrapperTemplateForPageAsync(int zoneId, string pageSlug)
    {
        var placement = await GetForPageAsync(zoneId, pageSlug);
        return placement?.WrapperTemplateOverride;
    }
}
