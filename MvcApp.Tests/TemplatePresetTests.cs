using MvcApp.Core.Abstractions;
using MvcApp.Services;
using Xunit;

namespace MvcApp.Tests;

/// <summary>
/// Per-template presets: a template arrives set up for its purpose and the owner edits from there.
///
/// These use an in-memory settings store on purpose. Applying a template against a real database
/// writes to it, and this suite must never do that.
/// </summary>
public class TemplatePresetTests
{
    private sealed class FakeSettings : ISettingsService
    {
        public Dictionary<string, string> Values { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> Writes { get; } = [];

        public Task<string?> GetAsync(string key) =>
            Task.FromResult(Values.TryGetValue(key, out var value) ? value : null);

        public Task<T?> GetAsync<T>(string key) where T : struct => Task.FromResult<T?>(null);

        public Task SetAsync(string key, string value, string? updatedBy = null)
        {
            Values[key] = value;
            Writes.Add(key);
            return Task.CompletedTask;
        }
    }

    /// <summary>The seeded catalogue: the settings a preset is allowed to name.</summary>
    private static readonly string[] CatalogueKeys =
    [
        "SiteTemplate", "Branding.SiteName",
        "Module.Forum.Enabled", "Module.Blog.Enabled", "Module.Chat.Enabled", "Module.Video.Enabled",
        "Module.Messages.Enabled", "Module.Store.Enabled", "Module.Iptv.Enabled", "Module.Pages.Enabled",
        "Module.Ads.Enabled", "Module.Utility.Enabled",
        "Module.Events.Enabled", "Module.Gamification.Enabled",
    ];

    private static FakeSettings Seeded()
    {
        var settings = new FakeSettings();
        foreach (var key in CatalogueKeys)
        {
            settings.Values[key] = "true";
        }

        settings.Values["Branding.SiteName"] = "their-own-name";

        return settings;
    }

    private static TemplateService Service(FakeSettings settings) =>
        new(settings, new TemplateProfileService());

    [Fact]
    public async Task Applying_a_template_presets_the_modules_its_purpose_calls_for()
    {
        var settings = Seeded();

        await Service(settings).SetActiveTemplateAsync("Minimal");

        Assert.Equal("Minimal", settings.Values["SiteTemplate"]);
        Assert.Equal("true", settings.Values["Module.Blog.Enabled"]);
        Assert.Equal("true", settings.Values["Module.Pages.Enabled"]);

        // 'Minimal' is content-first reading: everything else is off, not merely absent.
        Assert.Equal("false", settings.Values["Module.Iptv.Enabled"]);
        Assert.Equal("false", settings.Values["Module.Store.Enabled"]);
        Assert.Equal("false", settings.Values["Module.Ads.Enabled"]);
    }

    [Fact]
    public async Task Applying_a_template_leaves_everything_it_does_not_preset_alone()
    {
        // Personalisation must survive: the template sets its own presets and touches nothing else.
        var settings = Seeded();

        await Service(settings).SetActiveTemplateAsync("Minimal");

        Assert.Equal("their-own-name", settings.Values["Branding.SiteName"]);
        Assert.DoesNotContain("Branding.SiteName", settings.Writes);
    }

    [Fact]
    public async Task A_preset_naming_a_setting_that_does_not_exist_is_refused()
    {
        // A typo would otherwise write an orphan row while the template silently did nothing.
        var settings = new FakeSettings();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Service(settings).SetActiveTemplateAsync("Minimal"));

        Assert.Contains("do not exist", ex.Message);
    }

    [Fact]
    public async Task A_template_the_manifest_does_not_describe_has_no_presets()
    {
        var settings = Seeded();

        // The UI list and the profile manifest are separate; a template in one and not the other
        // must not fail, it simply carries no presets.
        await Service(settings).ApplyPresetsAsync("NoSuchTemplate");

        Assert.Empty(settings.Writes);
    }

    [Fact]
    public void Every_templates_module_presets_agree_with_its_composition()
    {
        // Presets are data and composition is data, both in the same profile. If they drift, a site
        // ships one set of modules and switches on another.
        //
        // Only the ten real modules are compared. Events and Gamification are features with no
        // assembly: composition cannot include them, so they are deliberately preset by purpose
        // instead — which is exactly why they must not be part of this check.
        var modules = new[]
        {
            "Forum", "Blog", "Chat", "Video", "Messages", "Store", "Iptv", "Pages", "Ads", "Utility",
        };

        var profiles = new TemplateProfileService();

        var misses = profiles.GetTemplates()
            .SelectMany(template =>
            {
                var profile = profiles.GetProfile(template)!;
                var composed = profile.Composition.Modules.ToHashSet(StringComparer.OrdinalIgnoreCase);

                return profile.Defaults.Settings
                    .Where(setting => setting.Key.StartsWith("Module.", StringComparison.Ordinal)
                                      && setting.Key.EndsWith(".Enabled", StringComparison.Ordinal))
                    .Select(setting => (Module: setting.Key[7..^8], setting.Value))
                    .Where(x => modules.Contains(x.Module, StringComparer.OrdinalIgnoreCase))
                    .Where(x => (x.Value == "true") != composed.Contains(x.Module))
                    .Select(x => $"{template}: Module.{x.Module}.Enabled={x.Value} but composed={composed.Contains(x.Module)}");
            })
            .ToList();

        Assert.Empty(misses);
    }

    [Fact]
    public void Every_template_declares_the_full_module_set()
    {
        // Half a preset set is worse than none: a module nobody preset keeps whatever value the
        // previous template left.
        var profiles = new TemplateProfileService();
        var modules = new[]
        {
            "Forum", "Blog", "Chat", "Video", "Messages", "Store", "Iptv", "Pages", "Ads", "Utility",
        };

        foreach (var template in profiles.GetTemplates())
        {
            var settings = profiles.GetProfile(template)!.Defaults.Settings;

            foreach (var module in modules)
            {
                Assert.True(
                    settings.ContainsKey($"Module.{module}.Enabled"),
                    $"{template} does not preset Module.{module}.Enabled");
            }
        }
    }
}
