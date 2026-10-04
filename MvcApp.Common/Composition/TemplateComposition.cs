using Microsoft.Extensions.Configuration;
using MvcApp.Core.Abstractions;

namespace MvcApp.Common.Composition;

/// <summary>
/// Which modules this deployment is built from.
///
/// Composition is a BUILD-TIME fact and is therefore read from configuration, never from the
/// database: a site is composed of its modules before it can ask the database anything, and the
/// seeding that writes <c>SiteTemplate</c> happens long after the container is built.
///
/// This is the seam that makes templates independent. The mother application (the website maker)
/// composes everything because it must offer every template; an offspring deploys with
/// <c>Template:Name</c> set and ships only the modules that template declares — no IPTV code in a
/// dating site, and no need for a flag at runtime to say so.
///
/// With nothing configured, every module is composed, which is exactly the behaviour before this
/// existed. Trimming is opt-in per deployment.
/// </summary>
public sealed class TemplateComposition
{
    /// <summary>The template this deployment is. Composition follows that profile's module set.</summary>
    public const string TemplateNameKey = "Template:Name";

    /// <summary>An explicit module list, for a composition that no profile describes.</summary>
    public const string ModulesKey = "Template:Modules";

    private readonly HashSet<string>? _declared;

    private TemplateComposition(HashSet<string>? declared, string source)
    {
        _declared = declared;
        Source = source;
    }

    /// <summary>Where the composition came from, for the startup log.</summary>
    public string Source { get; }

    /// <summary>True when the deployment asked for a specific module set.</summary>
    public bool IsTrimmed => _declared is not null;

    /// <summary>Names the deployment asked for. Empty when untrimmed.</summary>
    public IReadOnlyCollection<string> Declared => _declared ?? [];

    public bool Includes(string module) => _declared is null || _declared.Contains(module);

    /// <summary>
    /// Resolves the composition. Called before the container exists, so it builds its own profile
    /// reader rather than resolving one — the profile service is stateless and reads a manifest.
    /// </summary>
    public static TemplateComposition From(IConfiguration configuration, ITemplateProfileService profiles)
    {
        // An explicit list wins: it exists for a composition no profile describes.
        var explicitModules = ReadList(configuration.GetSection(ModulesKey));
        if (explicitModules.Count > 0)
        {
            EnsureKnown(explicitModules, ModulesKey);

            return new TemplateComposition(
                new HashSet<string>(explicitModules, StringComparer.OrdinalIgnoreCase),
                ModulesKey);
        }

        var templateName = configuration[TemplateNameKey];
        if (string.IsNullOrWhiteSpace(templateName))
        {
            return new TemplateComposition(null, "all modules (no template configured)");
        }

        var profile = profiles.GetProfile(templateName)
            ?? throw new InvalidOperationException(
                $"{TemplateNameKey} is '{templateName}', which no profile defines. "
                + $"Known templates: {string.Join(", ", profiles.GetTemplates())}. "
                + "Composing every module instead would ship features this site is not made of.");

        EnsureKnown(profile.Composition.Modules, $"{TemplateNameKey} = {profile.Template}");

        return new TemplateComposition(
            new HashSet<string>(profile.Composition.Modules, StringComparer.OrdinalIgnoreCase),
            $"{TemplateNameKey} = {profile.Template}");
    }

    /// <summary>
    /// Composition can only name something with an assembly behind it: it decides which assemblies
    /// are loaded and which MVC application parts exist, so a name with nothing behind it can only
    /// fail. This is checked where the composition is resolved rather than at request time.
    ///
    /// It exists because 'Gamification' was listed for Dating: a real feature with a real runtime
    /// flag, but living inside MvcApp.Web rather than in a module assembly. The app refused to start
    /// (verified). Features like it are switched by their setting, not by composition.
    /// </summary>
    private static void EnsureKnown(IEnumerable<string> modules, string source)
    {
        var unknown = modules.Where(module => !ModuleNames.IsKnown(module)).ToList();
        if (unknown.Count > 0)
        {
            throw new InvalidOperationException(
                $"{source} names module(s) that are not modules: {string.Join(", ", unknown)}. "
                + $"A composed module must have an assembly. Known modules: {string.Join(", ", ModuleNames.All)}. "
                + "Features inside MvcApp.Web (Events, Gamification) are switched by their setting, not by composition.");
        }
    }

    /// <summary>
    /// Guards against a typo in the declared set: a name that matched no module would otherwise
    /// compose nothing, silently, leaving a site missing a feature it asked for.
    /// </summary>
    public void EnsureEveryDeclaredNameMatched(IReadOnlyCollection<string> matched)
    {
        if (_declared is null)
        {
            return;
        }

        var unmatched = _declared.Where(name => !matched.Contains(name)).ToList();
        if (unmatched.Count > 0)
        {
            throw new InvalidOperationException(
                $"{Source} names module(s) that do not exist: {string.Join(", ", unmatched)}. "
                + $"Known modules: {string.Join(", ", matched)}.");
        }
    }

    private static List<string> ReadList(IConfigurationSection section) =>
        section.GetChildren()
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!)
            .ToList();
}
