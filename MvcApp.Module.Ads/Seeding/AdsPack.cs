using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Infrastructure;
using MvcApp.Infrastructure.Seeding;
using MvcApp.Infrastructure.Seeding.Packs;
using MvcApp.Module.Ads.Entities;
using MvcApp.Core.Seeding;

namespace MvcApp.Module.Ads.Seeding;

/// <summary>
/// The seven ad zones were originally written into the AddAdsEntities migration, which makes
/// them impossible for a pack to remove. This pack claims the migration rows first via
/// adoption, and only seeds them on a genuinely fresh database, so an Ads pack on a business
/// portal is removable as one unit.
/// </summary>
public sealed class AdsPack : ISeedPack
{
    public string Name => SeedPackNames.Ads;
    public string DisplayName => "Ads";
    public string Description => "The seven standard ad zones the layouts render into.";
    public IReadOnlyList<string> EntityNames => ["AdZone"];

    public async Task SeedAsync(SeedPackService packs, UserDbContext db, string? appliedBy, CancellationToken ct)
    {
        if (await packs.IsAppliedAsync(Name, ct))
            return;

        var claimed = await packs.AdoptAsync<AdZone>(Name, d => d.Set<AdZone>(), adoptedBy: appliedBy, ct: ct);
        if (claimed > 0)
            return;

        await packs.ApplyAsync<AdZone>(Name, d =>
        {
            if (d.Set<AdZone>().Any()) return;
            d.Set<AdZone>().AddRange(Build());
        }, appliedBy: appliedBy, ct: ct);
    }

    internal static IReadOnlyList<AdZone> Build() =>
    [
        new() { Key = "top-header", Name = "Top Header", Description = "Full-width banner at the very top of the page", MaxBanners = 1, DisplayOrder = 10, DefaultCssClass = "ad-zone ad-top-header mb-3", ExcludeFromLandingPage = true, IsActive = true },
        new() { Key = "sidebar-left", Name = "Left Sidebar", Description = "Vertical banner in the left sidebar", MaxBanners = 2, DisplayOrder = 20, DefaultCssClass = "ad-zone ad-sidebar-left mb-3", IsActive = true },
        new() { Key = "sidebar-right", Name = "Right Sidebar", Description = "Vertical banner in the right sidebar", MaxBanners = 2, DisplayOrder = 30, DefaultCssClass = "ad-zone ad-sidebar-right mb-3", IsActive = true },
        new() { Key = "content-top", Name = "Content Top", Description = "Banner at the top of the main content area", MaxBanners = 1, DisplayOrder = 40, DefaultCssClass = "ad-zone ad-content-top mb-4", ExcludeFromLandingPage = true, IsActive = true },
        new() { Key = "content-bottom", Name = "Content Bottom", Description = "Banner at the bottom of the main content area", MaxBanners = 1, DisplayOrder = 50, DefaultCssClass = "ad-zone ad-content-bottom mt-4", IsActive = true },
        new() { Key = "footer", Name = "Footer", Description = "Banner in the footer area", MaxBanners = 3, DisplayOrder = 60, DefaultCssClass = "ad-zone ad-footer mt-4", IsActive = true },
        new() { Key = "in-article", Name = "In-Article", Description = "Banner inserted between paragraphs in article content", MaxBanners = 1, DisplayOrder = 70, DefaultCssClass = "ad-zone ad-in-article my-4", ExcludeFromLandingPage = true, IsActive = true },
    ];
}
