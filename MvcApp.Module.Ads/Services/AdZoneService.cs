using Microsoft.EntityFrameworkCore;
using MvcApp.Core.Abstractions;
using MvcApp.Module.Ads.Entities;

namespace MvcApp.Module.Ads.Services;

public interface IAdZoneService
{
    Task<AdZone?> GetByKeyAsync(string key);
    Task<AdZone?> GetByIdAsync(int id);
    Task<IReadOnlyList<AdZone>> GetAllAsync(bool activeOnly = true);
    Task<AdZone> CreateAsync(AdZone zone);
    Task<AdZone> UpdateAsync(AdZone zone);
    Task DeleteAsync(int id);
    Task<bool> ExistsKeyAsync(string key, int? excludeId = null);
}

public class AdZoneService : IAdZoneService
{
    private readonly IRepository<AdZone> _repo;

    public AdZoneService(IRepository<AdZone> repo) => _repo = repo;

    public async Task<AdZone?> GetByKeyAsync(string key)
        => await _repo.Query().FirstOrDefaultAsync(z => z.Key == key);

    public async Task<AdZone?> GetByIdAsync(int id)
        => await _repo.GetFirstOrDefaultAsync(z => z.Id == id);

    public async Task<IReadOnlyList<AdZone>> GetAllAsync(bool activeOnly = true)
    {
        IQueryable<AdZone> q = _repo.Query();
        if (activeOnly) q = q.Where(z => z.IsActive);
        return await q.OrderBy(z => z.DisplayOrder).ThenBy(z => z.Name).ToListAsync();
    }

    public async Task<AdZone> CreateAsync(AdZone zone)
    {
        zone.CreatedAt = DateTime.UtcNow;
        zone.UpdatedAt = DateTime.UtcNow;
        await _repo.AddAsync(zone);
        return zone;
    }

    public async Task<AdZone> UpdateAsync(AdZone zone)
    {
        zone.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(zone);
        return zone;
    }

    public async Task DeleteAsync(int id)
    {
        var zone = await _repo.GetFirstOrDefaultAsync(z => z.Id == id);
        if (zone != null) await _repo.DeleteAsync(zone);
    }

    public async Task<bool> ExistsKeyAsync(string key, int? excludeId = null)
    {
        var q = _repo.Query().Where(z => z.Key == key);
        if (excludeId.HasValue) q = q.Where(z => z.Id != excludeId.Value);
        return await q.AnyAsync();
    }
}
