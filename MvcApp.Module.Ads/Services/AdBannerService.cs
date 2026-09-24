using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MvcApp.Core.Abstractions;
using MvcApp.Module.Ads.Entities;

namespace MvcApp.Module.Ads.Services;

public interface IAdBannerService
{
    Task<AdBanner?> GetByIdAsync(int id);
    Task<IReadOnlyList<AdBanner>> GetByZoneAsync(int zoneId, bool activeOnly = true, string? currentCulture = null, ClaimsPrincipal? user = null);
    Task<IReadOnlyList<AdBanner>> GetAllAsync(int zoneId, bool includeInactive = false);
    Task<AdBanner> CreateAsync(AdBanner banner);
    Task<AdBanner> UpdateAsync(AdBanner banner);
    Task DeleteAsync(int id);
    Task ReorderAsync(int zoneId, int[] bannerIdsInOrder);
}

public class AdBannerService : IAdBannerService
{
    private readonly IRepository<AdBanner> _repo;

    public AdBannerService(IRepository<AdBanner> repo) => _repo = repo;

    public async Task<AdBanner?> GetByIdAsync(int id)
        => await _repo.GetFirstOrDefaultAsync(b => b.Id == id);

    public async Task<IReadOnlyList<AdBanner>> GetByZoneAsync(int zoneId, bool activeOnly = true, string? currentCulture = null, ClaimsPrincipal? user = null)
    {
        var q = _repo.Query().Where(b => b.ZoneId == zoneId);

        if (activeOnly)
        {
            var now = DateTime.UtcNow;
            q = q.Where(b => b.IsActive &&
                           (b.StartDate == null || b.StartDate <= now) &&
                           (b.EndDate == null || b.EndDate >= now));
        }

        // Fetch banners first (client-side evaluation for complex filters)
        var banners = await q.OrderByDescending(b => b.Weight).ThenBy(b => b.Name).ToListAsync();

        if (!activeOnly)
            return banners;

        // Apply culture targeting in memory
        if (!string.IsNullOrWhiteSpace(currentCulture))
        {
            banners = banners.Where(b => string.IsNullOrWhiteSpace(b.TargetCultures) ||
                               b.TargetCultures!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                   .Contains(currentCulture, StringComparer.OrdinalIgnoreCase)).ToList();
        }

        // Apply role targeting in memory
        if (user != null)
        {
            banners = banners.Where(b => string.IsNullOrWhiteSpace(b.TargetRoles) ||
                               b.TargetRoles!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                   .Any(r => user.IsInRole(r))).ToList();
        }
        else
        {
            // Anonymous user - only show banners with no role targeting or targeting "Anonymous"
            banners = banners.Where(b => string.IsNullOrWhiteSpace(b.TargetRoles) ||
                               b.TargetRoles!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                   .Any(r => string.Equals(r, "Anonymous", StringComparison.OrdinalIgnoreCase))).ToList();
        }

        return banners;
    }

    public async Task<IReadOnlyList<AdBanner>> GetAllAsync(int zoneId, bool includeInactive = false)
    {
        var q = _repo.Query().Where(b => b.ZoneId == zoneId);
        if (!includeInactive) q = q.Where(b => b.IsActive);
        return await q.OrderByDescending(b => b.Weight).ThenBy(b => b.Name).ToListAsync();
    }

    public async Task<AdBanner> CreateAsync(AdBanner banner)
    {
        banner.CreatedAt = DateTime.UtcNow;
        banner.UpdatedAt = DateTime.UtcNow;
        await _repo.AddAsync(banner);
        return banner;
    }

    public async Task<AdBanner> UpdateAsync(AdBanner banner)
    {
        banner.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(banner);
        return banner;
    }

    public async Task DeleteAsync(int id)
    {
        var banner = await _repo.GetFirstOrDefaultAsync(b => b.Id == id);
        if (banner != null) await _repo.DeleteAsync(banner);
    }

    public async Task ReorderAsync(int zoneId, int[] bannerIdsInOrder)
    {
        var banners = await _repo.Query().Where(b => b.ZoneId == zoneId && bannerIdsInOrder.Contains(b.Id)).ToListAsync();
        for (int i = 0; i < bannerIdsInOrder.Length; i++)
        {
            var banner = banners.FirstOrDefault(b => b.Id == bannerIdsInOrder[i]);
            if (banner != null)
            {
                banner.Weight = bannerIdsInOrder.Length - i; // Higher weight = first
                await _repo.UpdateAsync(banner);
            }
        }
    }
}
