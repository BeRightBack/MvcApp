using Microsoft.Extensions.Configuration;
using MvcApp.Core.Abstractions;
using MvcApp.Services;
using MvcApp.Common.Composition;
using Xunit;

namespace MvcApp.Tests;

/// <summary>
/// Composition decides which modules a deployment is built from — the seam that lets a published site
/// ship only what it is made of. Two failure modes matter more than the happy path: composing
/// everything when a template was named (shipping features the site is not made of), and composing
/// nothing when a name is misspelled (a site silently missing a feature it asked for). Neither is
/// allowed to pass quietly.
/// </summary>
public class TemplateCompositionTests
{
    private static readonly ITemplateProfileService Profiles = new TemplateProfileService();

    private static TemplateComposition Resolve(params (string Key, string Value)[] settings) =>
        TemplateComposition.From(
            new ConfigurationBuilder()
                .AddInMemoryCollection(settings.ToDictionary(s => s.Key, s => (string?)s.Value))
                .Build(),
            Profiles);

    [Fact]
    public void With_nothing_configured_every_module_is_composed()
    {
        // The mother application's behaviour, and exactly what the app did before composition existed.
        var composition = Resolve();

        Assert.False(composition.IsTrimmed);

        foreach (var module in new[] { "Forum", "Blog", "Chat", "Video", "Messages", "Store", "Iptv", "Pages", "Ads", "Utility" })
        {
            Assert.True(composition.Includes(module), $"{module} should be composed when nothing is configured");
        }
    }

    [Theory]
    [InlineData("Minimal", "Blog", true)]
    [InlineData("Minimal", "Pages", true)]
    [InlineData("Minimal", "Iptv", false)]
    [InlineData("Minimal", "Store", false)]
    [InlineData("IPTV", "Iptv", true)]
    [InlineData("IPTV", "Forum", true)]
    [InlineData("IPTV", "Store", false)]
    [InlineData("Dating", "Chat", true)]
    [InlineData("Dating", "Iptv", false)]
    public void A_named_template_composes_exactly_its_declared_modules(string template, string module, bool expected)
    {
        var composition = Resolve((TemplateComposition.TemplateNameKey, template));

        Assert.Equal(expected, composition.Includes(module));
    }

    [Fact]
    public void A_template_name_no_profile_defines_fails_loudly()
    {
        // Composing everything instead would ship every feature to a site that asked for one.
        var ex = Assert.Throws<InvalidOperationException>(
            () => Resolve((TemplateComposition.TemplateNameKey, "NotATemplate")));

        Assert.Contains("NotATemplate", ex.Message);
        Assert.Contains("Known templates", ex.Message);
    }

    [Fact]
    public void An_explicit_module_list_wins_over_a_template_name()
    {
        // The escape hatch for a composition no profile describes.
        var composition = Resolve(
            (TemplateComposition.TemplateNameKey, "Dating"),
            (TemplateComposition.ModulesKey + ":0", "Blog"),
            (TemplateComposition.ModulesKey + ":1", "Pages"));

        Assert.True(composition.IsTrimmed);
        Assert.True(composition.Includes("Blog"));
        Assert.True(composition.Includes("Pages"));

        // Dating's own modules are NOT composed: the explicit list replaced them.
        Assert.False(composition.Includes("Chat"));
        Assert.False(composition.Includes("Iptv"));
    }

    [Fact]
    public void A_declared_module_name_that_matches_nothing_fails_loudly()
    {
        // A typo must not compose nothing, silently, leaving the site missing a feature. Resolution
        // now refuses it outright, before any request is served — so it is caught even earlier than
        // the catalogue-drift guard.
        var ex = Assert.Throws<InvalidOperationException>(
            () => Resolve(
                (TemplateComposition.ModulesKey + ":0", "Blog"),
                (TemplateComposition.ModulesKey + ":1", "Bloog")));

        Assert.Contains("Bloog", ex.Message);
    }

    [Fact]
    public void An_untrimmed_composition_never_reports_unmatched_names()
    {
        // Nothing was declared, so there is nothing to be unmatched — this must not throw.
        Resolve().EnsureEveryDeclaredNameMatched(["Forum"]);
    }

    [Fact]
    public void Every_template_can_actually_be_composed()
    {
        // The regression test for the defect this guard was written for: Dating, Luxury and Gaming
        // declared 'Gamification'/'Events', which are features inside MvcApp.Web and not modules, so
        // resolving their composition threw at startup and the app would not run at all.
        var profiles = new TemplateProfileService();

        foreach (var template in profiles.GetTemplates())
        {
            var composition = Resolve((TemplateComposition.TemplateNameKey, template));

            Assert.True(composition.IsTrimmed, $"{template} should trim");
        }
    }

    [Fact]
    public void Every_profiles_declared_module_has_an_assembly_to_compose()
    {
        var profiles = new TemplateProfileService();

        var notModules = profiles.GetTemplates()
            .SelectMany(template => profiles.GetProfile(template)!.Composition.Modules
                .Where(module => !ModuleNames.IsKnown(module))
                .Select(module => $"{template} -> {module}"))
            .ToList();

        Assert.Empty(notModules);
    }

    [Fact]
    public void A_declared_name_that_is_not_a_module_fails_at_resolution()
    {
        // 'Gamification' is the exact name that took the app down: a real feature, not a module.
        var ex = Assert.Throws<InvalidOperationException>(
            () => Resolve((TemplateComposition.ModulesKey + ":0", "Gamification")));

        Assert.Contains("not modules", ex.Message);
        Assert.Contains("Known modules", ex.Message);
    }
}
