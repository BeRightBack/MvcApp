using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MvcApp.Core.Abstractions;
using MvcApp.Core.Seeding;

namespace MvcApp.Services;

/// <summary>
/// The seed contract for every template, loaded as DATA rather than compiled into the platform.
///
/// A profile declares three independent things — what content it seeds, what it is composed of, and
/// what it defaults — so switching templates no longer leaves one template's content sitting in the
/// next template's database, and so a template is a manifest rather than a static list inside core.
///
/// Source of truth: <c>Templates/profiles.json</c>, embedded in this assembly. Point
/// <c>Templates:ProfilesPath</c> at another copy of that file to supply your own without rebuilding —
/// which is the first step toward templates that live entirely outside the platform.
///
/// Nothing here applies or removes anything. The packs own the rows, the manifest owns the
/// bookkeeping, and removal stays an explicit admin action.
///
/// Only packs that exist in <c>SeedPackNames.All</c> may be listed: a name that no pack implements is
/// reported by <see cref="GetUnknownPacks"/> and fails the profile tests, rather than silently
/// applying as "nothing".
/// </summary>
public class TemplateProfileService : ITemplateProfileService
{
    /// <summary>Logical name of the embedded manifest, pinned in the project file.</summary>
    public const string EmbeddedProfilesResource = "MvcApp.Services.Templates.profiles.json";

    /// <summary>Configuration key holding a path to an alternative profiles file.</summary>
    public const string ProfilesPathKey = "Templates:ProfilesPath";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private readonly IReadOnlyList<TemplateSeedProfile> _profiles;

    /// <summary>Uses the embedded manifest. Kept so the profile can be inspected without a container.</summary>
    public TemplateProfileService() : this(new ConfigurationBuilder().Build())
    {
    }

    public TemplateProfileService(IConfiguration configuration)
    {
        _profiles = Load(configuration);
    }

    public IReadOnlyList<TemplateSeedProfile> GetProfiles() => _profiles;

    public TemplateSeedProfile? GetProfile(string template) =>
        _profiles.FirstOrDefault(p => p.Template.Equals(template, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<string> GetTemplates() =>
        _profiles.Select(p => p.Template).ToList();

    public IReadOnlyList<string> GetUnknownPacks() =>
    [
        .. _profiles
            .SelectMany(p => p.Content.Packs)
            .Where(pack => !SeedPackNames.All.Contains(pack, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase),
    ];

    private static IReadOnlyList<TemplateSeedProfile> Load(IConfiguration configuration)
    {
        var configuredPath = configuration[ProfilesPathKey];

        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            // Configured but absent is a misconfiguration, not a reason to fall back: quietly using
            // the embedded set would mean the deployment runs templates nobody asked for.
            if (!File.Exists(configuredPath))
            {
                throw new InvalidOperationException(
                    $"{ProfilesPathKey} points at '{configuredPath}', which does not exist.");
            }

            return Parse(File.ReadAllText(configuredPath), configuredPath);
        }

        using var stream = typeof(TemplateProfileService).Assembly
            .GetManifestResourceStream(EmbeddedProfilesResource)
            ?? throw new InvalidOperationException(
                $"The embedded template profiles '{EmbeddedProfilesResource}' are missing from the assembly. "
                + "The project file must keep the EmbeddedResource entry for Templates/profiles.json.");

        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd(), EmbeddedProfilesResource);
    }

    private static IReadOnlyList<TemplateSeedProfile> Parse(string json, string source)
    {
        TemplateProfileDocument? document;

        try
        {
            document = JsonSerializer.Deserialize<TemplateProfileDocument>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Template profiles from {source} are not valid JSON: {ex.Message}", ex);
        }

        var profiles = document?.Profiles ?? [];

        if (profiles.Count == 0)
        {
            throw new InvalidOperationException(
                $"Template profiles from {source} define no templates. An empty set would leave the "
                + "admin template list blank and apply no content for any template.");
        }

        var duplicates = profiles
            .GroupBy(p => p.Template, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException(
                $"Template profiles from {source} define the same template more than once: "
                + string.Join(", ", duplicates) + ".");
        }

        return profiles;
    }

    /// <summary>Root object of the manifest, so the file can gain fields without reshaping.</summary>
    private sealed class TemplateProfileDocument
    {
        public List<TemplateSeedProfile> Profiles { get; set; } = [];
    }
}
