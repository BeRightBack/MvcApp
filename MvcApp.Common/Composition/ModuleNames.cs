namespace MvcApp.Common.Composition;

/// <summary>
/// The modules a site can be composed FROM — one entry per module assembly.
///
/// A profile's <c>composition.modules</c> may only name these. That restriction is real and not
/// bookkeeping: composition decides which assemblies are loaded and which MVC application parts
/// exist, so a name with no assembly behind it can only fail. This list is the authority the guard
/// and the profile tests check against.
///
/// Note what is NOT here: Events and Gamification. They are real features with real runtime flags
/// (<c>Module.Events.Enabled</c>, <c>Module.Gamification.Enabled</c>) but they live inside
/// MvcApp.Web, not in a module assembly — so a template switches them on with a setting, not with
/// composition. Listing them here would mean extracting them into modules first.
/// </summary>
public static class ModuleNames
{
    public static readonly IReadOnlyList<string> All =
    [
        "Forum",
        "Blog",
        "Chat",
        "Video",
        "Messages",
        "Store",
        "Iptv",
        "Pages",
        "Ads",
        "Utility",
    ];

    public static bool IsKnown(string name) =>
        All.Contains(name, StringComparer.OrdinalIgnoreCase);
}
