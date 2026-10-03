using System.Reflection;

namespace MvcApp.Infrastructure;

/// <summary>
/// Which entities each module owns.
///
/// This exists because the platform's context declares a DbSet for every module's entities, while
/// the entity CONFIGURATION (keys, relationships) lives in the owning module. A site composed
/// without that module therefore has entities in its model with nothing configuring them, and EF
/// refuses to build the model at all:
///
///   The entity type 'BlogPostTag' requires a primary key to be defined.
///
/// — which took every request down with a 500 (verified). Composition has to remove those entities
/// from the model, and this is the mapping that says which ones belong to which module.
///
/// Keyed by the module's assembly suffix: MvcApp.Module.IPTV -> "IPTV". Entity names are unique
/// across modules (a test enforces it), so a name identifies its owner unambiguously.
/// </summary>
public static class ModuleEntityMap
{
    public const string AssemblyPrefix = "MvcApp.Module.";

    private static readonly Dictionary<string, HashSet<string>> _byModule = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Ads"] = ["AdBanner", "AdClick", "AdImpression", "AdPlacement", "AdZone"],
        ["Blog"] = ["BlogCategory", "BlogComment", "BlogPost", "BlogPostTag", "BlogTag"],
        ["Chat"] = ["ChatRoom", "ChatRoomMessage"],
        ["Forum"] = ["ForumCategory", "ForumPost", "ForumThread"],
        ["IPTV"] = ["IptvOrder", "Payment", "ShoppingCartItem", "Subscription", "SubscriptionDetail", "SubscriptionPlan"],
        ["Pages"] = ["ContentPage", "ContentPageSnippet"],
        ["Store"] = ["CartItem", "Order", "OrderItem", "Product", "ProductCategory"],
        ["Utility"] = ["Project", "ToDoSubTask", "ToDoTask"],
        ["Video"] = ["VideoRoom", "VideoRoomMessage"],
    };

    public static IReadOnlyDictionary<string, IReadOnlySet<string>> ByModule { get; } =
        _byModule.ToDictionary(
            entry => entry.Key,
            entry => (IReadOnlySet<string>)entry.Value,
            StringComparer.OrdinalIgnoreCase);

    /// <summary>The module a configuration assembly belongs to, or null if it is not a module assembly.</summary>
    public static string? ModuleKey(string? assemblyName) =>
        assemblyName is not null && assemblyName.StartsWith(AssemblyPrefix, StringComparison.Ordinal)
            ? assemblyName[AssemblyPrefix.Length..]
            : null;

    /// <summary>The modules this deployment is built from, keyed the same way.</summary>
    public static HashSet<string> ComposedModuleKeys(IEnumerable<Assembly> configurationAssemblies) =>
        configurationAssemblies
            .Select(assembly => ModuleKey(assembly.GetName().Name))
            .Where(key => key is not null)
            .Select(key => key!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// True when an entity belongs to a module that is not part of this deployment — the case the
    /// model must drop. An entity no module owns (the platform's own) is never dropped.
    /// </summary>
    public static bool IsIgnored(string entityName, IReadOnlySet<string> composedModules) =>
        _byModule.Any(entry => entry.Value.Contains(entityName) && !composedModules.Contains(entry.Key));
}
