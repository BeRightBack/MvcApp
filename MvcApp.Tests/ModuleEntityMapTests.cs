using MvcApp.Infrastructure;
using Xunit;

namespace MvcApp.Tests;

/// <summary>
/// The model filter that makes composition work end to end. Without it a site composed without a
/// module keeps that module's entities with nothing configuring them, and EF refuses to build the
/// model — every request 500. These pin the decision itself, since the failure it prevents is a
/// whole-site outage rather than a missing page.
/// </summary>
public class ModuleEntityMapTests
{
    [Fact]
    public void An_entity_of_a_composed_module_is_mapped()
    {
        IReadOnlySet<string> composed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Blog", "Pages" };

        Assert.False(ModuleEntityMap.IsIgnored("BlogPostTag", composed));
        Assert.False(ModuleEntityMap.IsIgnored("ContentPage", composed));
    }

    [Fact]
    public void An_entity_of_a_module_that_is_not_composed_is_dropped()
    {
        // The exact case that took the site down: BlogPostTag with no configuration, because Blog
        // was not part of the deployment.
        IReadOnlySet<string> composed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Pages" };

        Assert.True(ModuleEntityMap.IsIgnored("BlogPostTag", composed));
        Assert.True(ModuleEntityMap.IsIgnored("CartItem", composed));
    }

    [Fact]
    public void The_platforms_own_entities_are_never_dropped()
    {
        // Identity and the rest belong to no module, so no composition may remove them.
        IReadOnlySet<string> nothing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Assert.False(ModuleEntityMap.IsIgnored("UserDetails", nothing));
        Assert.False(ModuleEntityMap.IsIgnored("AuditLog", nothing));
        Assert.False(ModuleEntityMap.IsIgnored("SystemSetting", nothing));
    }

    [Fact]
    public void No_entity_is_claimed_by_two_modules()
    {
        // Ownership is resolved by entity name, so a name in two modules would be ambiguous and the
        // wrong one could be dropped.
        var duplicated = ModuleEntityMap.ByModule
            .SelectMany(entry => entry.Value.Select(entity => (Module: entry.Key, Entity: entity)))
            .GroupBy(x => x.Entity, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}: {string.Join(", ", group.Select(x => x.Module))}")
            .ToList();

        Assert.Empty(duplicated);
    }

    [Fact]
    public void A_module_assembly_maps_to_its_module_key()
    {
        Assert.Equal("IPTV", ModuleEntityMap.ModuleKey("MvcApp.Module.IPTV"));
        Assert.Equal("Blog", ModuleEntityMap.ModuleKey("MvcApp.Module.Blog"));

        // Anything that is not a module assembly belongs to no module and owns no module entities.
        Assert.Null(ModuleEntityMap.ModuleKey("MvcApp.Core"));
        Assert.Null(ModuleEntityMap.ModuleKey("MvcApp.Infrastructure"));
        Assert.Null(ModuleEntityMap.ModuleKey(null));
    }

    [Fact]
    public void Ignoring_nothing_is_the_default_when_every_module_is_composed()
    {
        // Untrimmed: the mother application must keep the exact model it had before composition.
        var all = ModuleEntityMap.ByModule.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var entity in ModuleEntityMap.ByModule.Values.SelectMany(v => v))
        {
            Assert.False(ModuleEntityMap.IsIgnored(entity, all));
        }
    }
}
