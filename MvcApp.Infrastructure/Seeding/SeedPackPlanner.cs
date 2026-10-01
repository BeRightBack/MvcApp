using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MvcApp.Core.Abstractions;
using MvcApp.Infrastructure.Seeding.Packs;
using MvcApp.Core.Seeding;

namespace MvcApp.Infrastructure.Seeding;

public sealed record SeedPlanItem(string Pack, string DisplayName, bool Needed, bool Applied, int Rows);

/// <summary>
/// Decides which packs a template needs and applies only those. This is the whole point of
/// the refactor: before it, Program.cs applied every registered pack on every boot, so a
/// business portal carried 41 interest tags and 3 VIP plans it had no use for.
///
/// A pack that the template does not need is never silently deleted. Removal stays an
/// explicit admin action, and re-applying a removed pack is a clean insert.
/// </summary>
public sealed class SeedPackPlanner(
    UserDbContext db,
    SeedPackService packs,
    IEnumerable<ISeedPack> allPacks,
    ITemplateProfileService profiles,
    ILogger<SeedPackPlanner> logger)
{
    /// <summary>What the template wants, what is actually on disk, and how many rows each pack owns.</summary>
    public async Task<IReadOnlyList<SeedPlanItem>> PlanAsync(
        string template,
        CancellationToken ct = default)
    {
        var profile = profiles.GetProfile(template);
        var needed = profile?.Packs.ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var byName = allPacks.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        var rowsByPack = await RowsByPackAsync(ct);

        var plan = new List<SeedPlanItem>();

        foreach (var name in SeedPackNames.All)
        {
            if (!byName.TryGetValue(name, out var pack))
                continue;

            plan.Add(new SeedPlanItem(
                name,
                pack.DisplayName,
                needed.Contains(name),
                rowsByPack.ContainsKey(name),
                rowsByPack.GetValueOrDefault(name)));
        }

        return plan;
    }

    /// <summary>
    /// Applies the packs the template's profile asks for. Packs belonging to other templates
    /// are left untouched: switching templates must never destroy content, it only stops
    /// adding content that does not belong.
    /// </summary>
    public async Task<IReadOnlyList<SeedPlanItem>> ApplyForTemplateAsync(
        string template,
        string? appliedBy = null,
        CancellationToken ct = default)
    {
        var profile = profiles.GetProfile(template);

        if (profile is null)
        {
            logger.LogWarning(
                "No seed profile is registered for template {Template}; no packs were applied.",
                template);
            return await PlanAsync(template, ct);
        }

        var needed = profile.Packs.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var byName = allPacks.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var name in profile.Packs)
        {
            if (!byName.TryGetValue(name, out var pack))
            {
                logger.LogWarning(
                    "Seed profile for {Template} lists pack {Pack}, which is not registered.",
                    template, name);
                continue;
            }

            try
            {
                await pack.SeedAsync(packs, db, appliedBy, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Seed pack {Pack} failed; continuing with the remaining packs", pack.Name);
            }
        }

        var stale = (await packs.GetAppliedPacksAsync(ct))
            .Where(p => !needed.Contains(p))
            .ToList();

        if (stale.Count > 0)
        {
            logger.LogInformation(
                "Template {Template} does not use seed pack(s) {Packs}. Their rows are still "
                + "present and are not deleted automatically; remove them from Admin > Seed Packs "
                + "if this site should not keep them.",
                template, string.Join(", ", stale));
        }

        return await PlanAsync(template, ct);
    }

    private async Task<Dictionary<string, int>> RowsByPackAsync(CancellationToken ct)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var groups = await db.SeedManifest
            .GroupBy(m => m.PackName)
            .Select(g => new { Pack = g.Key, Rows = g.Count() })
            .ToListAsync(ct);

        foreach (var group in groups)
            result[group.Pack] = group.Rows;

        return result;
    }
}
