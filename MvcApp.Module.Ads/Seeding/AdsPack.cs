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
    public string Description =>
        "The seven standard ad zones the layouts render into, plus example banners so a site that "
        + "declares this pack has something to show instead of seven empty zones.";
    public IReadOnlyList<string> EntityNames => ["AdZone", "AdBanner"];

    /// <summary>
    /// Site-relative, deliberately. The creatives live in the site's own wwwroot - the template
    /// project already ships its ad images there - so this path resolves in EVERY environment.
    /// Pointing at the module's /_content/... instead only works from a published output, and an
    /// instance running a plain build output then shows broken images (verified: that is exactly
    /// what happened here).
    /// </summary>
    private const string CreativeBase = "/images/promo";

    public async Task SeedAsync(SeedPackService packs, UserDbContext db, string? appliedBy, CancellationToken ct)
    {
        await SeedZonesAsync(packs, appliedBy, ct);
        await SeedExampleBannersAsync(packs, appliedBy, ct);
    }

    private async Task SeedZonesAsync(SeedPackService packs, string? appliedBy, CancellationToken ct)
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
        }, existing: c => c.Set<AdZone>(),
            appliedBy: appliedBy, ct: ct);
    }

    /// <summary>
    /// Example banners. This is deliberately a separate phase with its own manifest entry: on a
    /// database where the zones were ADOPTED from the migration, the zone half returns early, so
    /// banners hung off the same call would never be seeded on any existing site — only on a fresh
    /// one. Applying them per entity type keeps both halves independently idempotent.
    ///
    /// Nothing here is invented content: they are the stock creatives, referenced through this
    /// module's own static assets so a published site gets working images rather than paths
    /// pointing into somebody else's wwwroot.
    /// </summary>
    private async Task SeedExampleBannersAsync(SeedPackService packs, string? appliedBy, CancellationToken ct)
    {
        await packs.ApplyAsync<AdBanner>(Name, d =>
        {
            if (d.Set<AdBanner>().Any()) return;

            var zones = d.Set<AdZone>().ToDictionary(z => z.Key, StringComparer.OrdinalIgnoreCase);
            var now = DateTime.UtcNow;

            foreach (var example in BuildExamples())
            {
                if (!zones.TryGetValue(example.ZoneKey, out var zone))
                    continue;

                d.Set<AdBanner>().Add(new AdBanner
                {
                    ZoneId = zone.Id,
                    Name = example.Name,
                    Type = example.Type,
                    Content = example.Content,
                    TargetUrl = example.TargetUrl,
                    AltText = example.AltText,
                    Weight = example.Weight,
                    TargetRoles = example.TargetRoles,
                    // Only real creatives are live. The type-coverage and targeting examples are
                    // fixtures: they exist so every render path can be exercised on demand, but a
                    // site that simply declares the Ads pack should show advertisements, not the
                    // word "Text banner". They are one toggle away in the admin.
                    IsActive = example.Type == AdBannerType.Image,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }, existing: c => c.Set<AdBanner>(),
            appliedBy: appliedBy, ct: ct);
    }

    private static IEnumerable<(string ZoneKey, string Name, AdBannerType Type, string Content,
        string? TargetUrl, string? AltText, int Weight, string? TargetRoles)> BuildExamples()
    {
        // Footer and content-bottom carry more than their slot count on purpose: that is what makes
        // them rotate, so a new site can see that behaviour rather than read about it.
        yield return ("footer", "Footer — Frenzyzone (image)", AdBannerType.Image,
            $"{CreativeBase}/footer/frenzyzone-v2-728x90.png", "https://example.com", "Frenzyzone", 120, null);
        yield return ("footer", "Footer — Frenzyzone classic (image)", AdBannerType.Image,
            $"{CreativeBase}/footer/frenzyzone-728x90.png", "https://example.com", "Frenzyzone", 110, null);
        yield return ("footer", "Footer — xxxciety (image)", AdBannerType.Image,
            $"{CreativeBase}/footer/xxxciety-728x90.png", "https://example.com", "xxxciety", 100, null);
        yield return ("footer", "Footer — xxxciety art (image)", AdBannerType.Image,
            $"{CreativeBase}/footer/xxxciety-art-728x90.png", "https://example.com", "xxxciety", 90, null);
        yield return ("footer", "Footer — boutique (image)", AdBannerType.Image,
            $"{CreativeBase}/footer/boutique-728x90.png", "https://example.com", "boutique", 80, null);

        yield return ("content-bottom", "Content bottom — FatTVSet (image)", AdBannerType.Image,
            $"{CreativeBase}/content-bottom/fattvset-970x250.png", "https://example.com", "FatTVSet", 120, null);
        yield return ("content-bottom", "Content bottom — FatTVSet v2 (image)", AdBannerType.Image,
            $"{CreativeBase}/content-bottom/fattvset-v2-970x250.png", "https://example.com", "FatTVSet", 110, null);
        yield return ("content-bottom", "Content bottom — xxxciety (image)", AdBannerType.Image,
            $"{CreativeBase}/content-bottom/xxxciety-970x250.png", "https://example.com", "xxxciety", 100, null);

        yield return ("sidebar-right", "Sidebar right — xxxciety (image)", AdBannerType.Image,
            $"{CreativeBase}/sidebar-right/xxxciety-300x250.png", "https://example.com", "xxxciety", 120, null);
        yield return ("sidebar-right", "Sidebar right — boutique (image)", AdBannerType.Image,
            $"{CreativeBase}/sidebar-right/boutique-300x250.png", "https://example.com", "boutique", 110, null);

        yield return ("top-header", "Top header — Frenzyzone (image)", AdBannerType.Image,
            $"{CreativeBase}/top-header/frenzyzone-970x250.png", "https://example.com", "Frenzyzone", 120, null);
        yield return ("content-top", "Content top — Frenzyzone (image)", AdBannerType.Image,
            $"{CreativeBase}/content-top/frenzyzone-728x90.png", "https://example.com", "Frenzyzone", 120, null);
        yield return ("sidebar-left", "Sidebar left — xxxciety (image)", AdBannerType.Image,
            $"{CreativeBase}/sidebar-left/xxxciety-300x250.png", "https://example.com", "xxxciety", 120, null);
        yield return ("in-article", "In-article — boutique (image)", AdBannerType.Image,
            $"{CreativeBase}/in-article/boutique-728x90.png", "https://example.com", "boutique", 120, null);

        // One of each remaining render path, so all four banner types are exercised on a new site.
        yield return ("sidebar-left", "Sidebar left — HTML example", AdBannerType.Html,
            "<div style=\"padding:12px;border:1px solid #dee2e6;background:#f8f9fa;border-radius:6px\">"
            + "<strong>HTML banner</strong><br /><span style=\"font-size:12px\">Type=Html</span></div>",
            "https://example.com", null, 60, null);
        yield return ("top-header", "Top header — text example", AdBannerType.Text,
            "Text banner — the top-header zone is not shown on the home page",
            "https://example.com", null, 60, null);
        yield return ("in-article", "In-article — script example", AdBannerType.Script,
            "<script>document.write('<span style=\"font-size:12px;color:#6c757d\">Script banner"
            + "</span>')</script>",
            null, null, 60, null);

        // Targeting sample: visible to an administrator, invisible to everyone else.
        yield return ("sidebar-left", "Sidebar left — admin only (role targeting)", AdBannerType.Text,
            "Admin-only banner (TargetRoles=Admin)",
            null, null, 40, "Admin");
    }

    internal static IReadOnlyList<AdZone> Build() =>
    [
        // top-header and content-top are NOT landing-page excluded: the most valuable inventory is
        // the top of the landing page, and refusing it there (which the original rows did) meant a
        // freshly seeded site appeared to have no banner positions at all - they were all below the
        // fold or excluded. in-article stays excluded from the landing page, which is correct: there
        // is no article on it.
        new() { Key = "top-header", Name = "Top Header", Description = "Full-width banner at the very top of the page", MaxBanners = 1, DisplayOrder = 10, DefaultCssClass = "promo-slot ad-top-header mb-3", IsActive = true },
        new() { Key = "sidebar-left", Name = "Left Sidebar", Description = "Vertical banner in the left sidebar", MaxBanners = 2, DisplayOrder = 20, DefaultCssClass = "promo-slot ad-sidebar-left mb-3", IsActive = true },
        new() { Key = "sidebar-right", Name = "Right Sidebar", Description = "Vertical banner in the right sidebar", MaxBanners = 2, DisplayOrder = 30, DefaultCssClass = "promo-slot ad-sidebar-right mb-3", IsActive = true },
        new() { Key = "content-top", Name = "Content Top", Description = "Banner at the top of the main content area", MaxBanners = 1, DisplayOrder = 40, DefaultCssClass = "promo-slot ad-content-top mb-4", IsActive = true },
        new() { Key = "content-bottom", Name = "Content Bottom", Description = "Banner at the bottom of the main content area", MaxBanners = 1, DisplayOrder = 50, DefaultCssClass = "promo-slot ad-content-bottom mt-4", IsActive = true },
        new() { Key = "footer", Name = "Footer", Description = "Banner in the footer area", MaxBanners = 3, DisplayOrder = 60, DefaultCssClass = "promo-slot ad-footer mt-4", IsActive = true },
        new() { Key = "in-article", Name = "In-Article", Description = "Banner inserted between paragraphs in article content", MaxBanners = 1, DisplayOrder = 70, DefaultCssClass = "promo-slot ad-in-article my-4", ExcludeFromLandingPage = true, IsActive = true },
    ];
}
