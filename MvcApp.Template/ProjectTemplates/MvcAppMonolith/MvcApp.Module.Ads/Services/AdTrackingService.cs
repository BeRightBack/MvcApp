using Microsoft.EntityFrameworkCore;
using MvcApp.Core.Abstractions;
using MvcApp.Module.Ads.Entities;

namespace MvcApp.Module.Ads.Services;

public interface IAdTrackingService
{
    Task RecordImpressionAsync(int bannerId, string? pageSlug, string? zoneKey, string? ipHash, string? userAgent, string? culture, string? userId);
    Task RecordClickAsync(int bannerId, string? pageSlug, string? zoneKey, string? ipHash, string? referrer, string? userId);
    Task<int> GetImpressionCountAsync(int bannerId, DateTime? since = null);
    Task<int> GetClickCountAsync(int bannerId, DateTime? since = null);
    Task<Dictionary<int, (int Impressions, int Clicks)>> GetStatsForZoneAsync(int zoneId, DateTime? since = null);
}

public class AdTrackingService : IAdTrackingService
{
    private readonly IRepository<AdImpression> _impressionRepo;
    private readonly IRepository<AdClick> _clickRepo;

    public AdTrackingService(IRepository<AdImpression> impressionRepo, IRepository<AdClick> clickRepo)
    {
        _impressionRepo = impressionRepo;
        _clickRepo = clickRepo;
    }

    public async Task RecordImpressionAsync(int bannerId, string? pageSlug, string? zoneKey, string? ipHash, string? userAgent, string? culture, string? userId)
    {
        var impression = new AdImpression
        {
            BannerId = bannerId,
            PageSlug = pageSlug,
            ZoneKey = zoneKey,
            IpHash = ipHash,
            UserAgent = userAgent?[..Math.Min(userAgent.Length, 500)],
            Culture = culture,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        await _impressionRepo.AddAsync(impression);
    }

    public async Task RecordClickAsync(int bannerId, string? pageSlug, string? zoneKey, string? ipHash, string? referrer, string? userId)
    {
        var click = new AdClick
        {
            BannerId = bannerId,
            PageSlug = pageSlug,
            ZoneKey = zoneKey,
            IpHash = ipHash,
            Referrer = referrer,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        await _clickRepo.AddAsync(click);
    }

    public async Task<int> GetImpressionCountAsync(int bannerId, DateTime? since = null)
    {
        var q = _impressionRepo.Query().Where(i => i.BannerId == bannerId);
        if (since.HasValue) q = q.Where(i => i.CreatedAt >= since.Value);
        return await q.CountAsync();
    }

    public async Task<int> GetClickCountAsync(int bannerId, DateTime? since = null)
    {
        var q = _clickRepo.Query().Where(c => c.BannerId == bannerId);
        if (since.HasValue) q = q.Where(c => c.CreatedAt >= since.Value);
        return await q.CountAsync();
    }

    public async Task<Dictionary<int, (int Impressions, int Clicks)>> GetStatsForZoneAsync(int zoneId, DateTime? since = null)
    {
        // This would need a join with banners - simplified for now
        return new Dictionary<int, (int, int)>();
    }
}
