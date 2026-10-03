using Microsoft.Extensions.Configuration;
using MvcApp.Services;
using Xunit;

namespace MvcApp.Tests;

/// <summary>
/// Template profiles are data now, so the loader itself is load-bearing: an empty or unreadable set
/// would leave the admin template list blank and apply no content for any template. These pin the
/// three ways that can go wrong, and that a shipped manifest is actually being read.
///
/// The rest of the profile invariants live in <see cref="TemplateProfileTests"/> and now run against
/// the data file rather than a static list.
/// </summary>
public class TemplateProfileDataTests
{
    private static IConfiguration ConfigurationFor(string? profilesPath) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [TemplateProfileService.ProfilesPathKey] = profilesPath,
            })
            .Build();

    [Fact]
    public void The_shipped_manifest_is_loaded_and_defines_the_known_templates()
    {
        // No configuration at all: the embedded manifest is the source of truth.
        var profiles = new TemplateProfileService();

        var templates = profiles.GetTemplates();

        Assert.Contains("Default", templates);
        Assert.Contains("IPTV", templates);
        Assert.Contains("Frenzyzone", templates);

        // The guard in the loader means an empty set cannot reach here, but this pins that the
        // shipped manifest is the full set rather than a stray entry.
        Assert.True(templates.Count >= 12, $"Expected the shipped template set, got: {string.Join(", ", templates)}");
        Assert.Empty(profiles.GetUnknownPacks());
    }

    [Fact]
    public void A_configured_but_missing_profiles_file_fails_loudly()
    {
        const string missing = "/nonexistent/template-profiles.json";

        var ex = Assert.Throws<InvalidOperationException>(
            () => new TemplateProfileService(ConfigurationFor(missing)));

        // Falling back to the embedded set would mean the deployment runs templates nobody asked for.
        Assert.Contains(missing, ex.Message);
    }

    [Fact]
    public void A_malformed_profiles_file_fails_loudly()
    {
        var path = Path.Combine(Path.GetTempPath(), $"profiles-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{ this is not valid json");

        try
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => new TemplateProfileService(ConfigurationFor(path)));

            Assert.Contains(path, ex.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void A_profiles_file_with_no_templates_fails_loudly()
    {
        var path = Path.Combine(Path.GetTempPath(), $"profiles-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, """{ "profiles": [] }""");

        try
        {
            // An empty set passing silently is the failure mode worth catching: every invariant
            // assertion in the profile tests would pass vacuously against nothing.
            Assert.Throws<InvalidOperationException>(
                () => new TemplateProfileService(ConfigurationFor(path)));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void A_profiles_file_replaces_the_embedded_set()
    {
        // The point of the override: a deployment supplies its own templates without a rebuild.
        var path = Path.Combine(Path.GetTempPath(), $"profiles-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, """
            {
              "profiles": [
                {
                  "template": "Solo",
                  "purpose": "One template, supplied by the deployment.",
                  "content": { "packs": ["Blog"] },
                  "composition": { "modules": ["Blog", "Pages"], "offModules": ["Iptv"] }
                }
              ]
            }
            """);

        try
        {
            var profiles = new TemplateProfileService(ConfigurationFor(path));

            Assert.Equal(["Solo"], profiles.GetTemplates());

            var solo = profiles.GetProfile("Solo");
            Assert.NotNull(solo);
            Assert.Equal(["Blog"], solo!.Content.Packs);
            Assert.Equal(["Blog", "Pages"], solo.Composition.Modules);
            Assert.Equal(["Iptv"], solo.Composition.OffModules);
            Assert.Empty(profiles.GetUnknownPacks());
        }
        finally
        {
            File.Delete(path);
        }
    }
}
