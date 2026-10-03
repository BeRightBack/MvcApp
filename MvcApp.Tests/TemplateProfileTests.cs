using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MvcApp.Core.Abstractions;
using MvcApp.Core.Seeding;
using MvcApp.Infrastructure;
using MvcApp.Infrastructure.Seeding.Packs;
using MvcApp.Module.Ads;
using MvcApp.Module.Video;
using MvcApp.Services;
using Xunit;

namespace MvcApp.Tests;

/// <summary>
/// Guards on the per-template seeding contract. These need no database, so they run on every
/// build — unlike <see cref="SeedPackIntegrationTests"/>, which returns early unless
/// SEED_TEST_CONNECTION is set.
///
/// The bug these exist for: SubscriptionPlan and SubscriptionDetail used to be owned by
/// DatingPack, so the IPTV template — whose own stated purpose is "subscription plans and a
/// support forum" — listed no pack that produced tiers. /iptv, /iptv-store and the plan admin
/// all came up empty on a fresh IPTV database, and nothing failed loudly. A profile asking for
/// content no pack owns is the failure mode, so it is asserted rather than logged.
/// </summary>
public class TemplateProfileTests
{
    private static ITemplateProfileService Profiles => new TemplateProfileService();

    private static IReadOnlyList<ISeedPack> RegisteredPacks()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddInfrastructure(configuration);

        // AdsPack and VideoPack are registered by their own modules rather than by
        // AddInfrastructure, so they only exist when the module is wired. That is deliberate —
        // a deployment without the Ads module must not have an Ads pack to remove — but it means
        // any test that enumerates ISeedPack has to wire the modules too, or it sees a registry
        // the real app never has.
        services.AddAdsModule();
        services.AddVideo();

        using var provider = services.BuildServiceProvider();
        return provider.GetServices<ISeedPack>().ToList();
    }

    [Fact]
    public void Every_pack_a_profile_lists_is_implemented()
    {
        var implemented = RegisteredPacks().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var requested = Profiles.GetProfiles().SelectMany(p => p.Content.Packs).Distinct(StringComparer.OrdinalIgnoreCase);

        var missing = requested.Where(pack => !implemented.Contains(pack)).ToList();

        Assert.True(
            missing.Count == 0,
            "These profiles ask for packs no registered ISeedPack provides, so the content would "
            + $"silently never appear: {string.Join(", ", missing)}");
    }

    [Fact]
    public void GetUnknownPacks_is_empty()
    {
        Assert.Empty(Profiles.GetUnknownPacks());
    }

    [Fact]
    public void Every_implemented_pack_is_declared_in_SeedPackNames()
    {
        var declared = SeedPackNames.All.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var undeclared = RegisteredPacks()
            .Select(p => p.Name)
            .Where(name => !declared.Contains(name))
            .ToList();

        Assert.True(
            undeclared.Count == 0,
            $"A pack exists but is not in SeedPackNames.All, so it cannot be shown or validated: {string.Join(", ", undeclared)}");
    }

    [Fact]
    public void Every_declared_pack_has_exactly_one_implementation()
    {
        var implementations = RegisteredPacks()
            .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var wrong = SeedPackNames.All
            .Where(name => implementations.GetValueOrDefault(name) != 1)
            .ToList();

        Assert.True(
            wrong.Count == 0,
            $"Each name in SeedPackNames.All promises exactly one owner; these do not: {string.Join(", ", wrong)}");
    }

    [Fact]
    public void Pack_names_and_display_names_are_populated()
    {
        var problems = RegisteredPacks()
            .Where(p => string.IsNullOrWhiteSpace(p.Name)
                     || string.IsNullOrWhiteSpace(p.DisplayName)
                     || string.IsNullOrWhiteSpace(p.Description)
                     || p.EntityNames.Count == 0)
            .Select(p => p.Name)
            .ToList();

        Assert.True(
            problems.Count == 0,
            $"The admin seeder page renders all of these, so a blank one renders a blank row: {string.Join(", ", problems)}");
    }

    [Theory]
    [InlineData("IPTV")]
    [InlineData("Dating")]
    [InlineData("Luxury")]
    public void Templates_that_sell_a_membership_list_the_plans_pack(string template)
    {
        var profile = Profiles.GetProfile(template);

        Assert.NotNull(profile);
        Assert.Contains(SeedPackNames.Plans, profile!.Content.Packs);
    }

    [Fact]
    public void The_plans_pack_is_not_claimed_by_the_dating_pack()
    {
        var dating = RegisteredPacks().Single(p => p.Name == SeedPackNames.Dating);
        var plans = RegisteredPacks().Single(p => p.Name == SeedPackNames.Plans);

        Assert.DoesNotContain("SubscriptionPlan", dating.EntityNames);
        Assert.Contains("SubscriptionPlan", plans.EntityNames);
        Assert.Contains("SubscriptionDetail", plans.EntityNames);
    }

    [Fact]
    public void Only_the_plans_pack_owns_subscription_entities()
    {
        var owners = RegisteredPacks()
            .Where(p => p.EntityNames.Contains("SubscriptionPlan")
                     || p.EntityNames.Contains("SubscriptionDetail"))
            .Select(p => p.Name)
            .ToList();

        Assert.Equal([SeedPackNames.Plans], owners);
    }

    [Fact]
    public void Every_profile_names_a_unique_template_with_a_purpose_and_at_least_one_module()
    {
        var profiles = Profiles.GetProfiles();

        var duplicateTemplates = profiles
            .GroupBy(p => p.Template, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.True(duplicateTemplates.Count == 0, $"Duplicate profiles: {string.Join(", ", duplicateTemplates)}");

        var incomplete = profiles
            .Where(p => string.IsNullOrWhiteSpace(p.Template)
                     || string.IsNullOrWhiteSpace(p.Purpose)
                     || p.Composition.Modules.Count == 0)
            .Select(p => p.Template)
            .ToList();

        Assert.True(incomplete.Count == 0, $"Incomplete profiles: {string.Join(", ", incomplete)}");
    }

    [Fact]
    public void A_profile_never_lists_a_module_it_turns_off()
    {
        var conflicts = Profiles.GetProfiles()
            .Where(p => p.Composition.Modules.Intersect(p.Composition.OffModules, StringComparer.OrdinalIgnoreCase).Any())
            .Select(p => p.Template)
            .ToList();

        Assert.True(
            conflicts.Count == 0,
            $"A module cannot be on and off in the same profile: {string.Join(", ", conflicts)}");
    }

    [Fact]
    public void A_profile_does_not_ask_for_the_same_pack_twice()
    {
        var duplicates = Profiles.GetProfiles()
            .Where(p => p.Content.Packs.Count != p.Content.Packs.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            .Select(p => p.Template)
            .ToList();

        Assert.True(duplicates.Count == 0, $"Repeated packs: {string.Join(", ", duplicates)}");
    }

    [Fact]
    public async Task Every_template_the_app_offers_has_a_profile()
    {
        // TemplateService is the list the admin actually sees. A template with no profile would
        // fall through the planner with nothing to apply and nothing to remove.
        var offered = await new TemplateService(new UnusedSettingsService()).GetAvailableTemplatesAsync();
        var profiled = Profiles.GetTemplates().ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unprofiled = offered.Select(t => t.Name).Where(t => !profiled.Contains(t)).ToList();

        Assert.True(unprofiled.Count == 0, $"Templates with no seed profile: {string.Join(", ", unprofiled)}");
    }

    private sealed class UnusedSettingsService : ISettingsService
    {
        public Task<string?> GetAsync(string key) => Task.FromResult<string?>(null);

        public Task<T?> GetAsync<T>(string key) where T : struct => Task.FromResult<T?>(null);

        public Task SetAsync(string key, string value, string? updatedBy = null) => Task.CompletedTask;
    }
}
