namespace MvcApp.Core;

/// <summary>
/// Catalog of every nav item a template navbar/footer can contain. The
/// per-template nav editor (Admin/TemplateNav) builds SiteNav.{template} from
/// these items, so a template can include or remove modules and shared links
/// at will. Module items only render when the module is enabled. Items with a
/// <see cref="Templates"/> value are only offered when editing those templates.
/// </summary>
public class NavCatalogItem
{
    public string Key { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string? Templates { get; set; }
    public NavItem Item { get; set; } = new();
}

public static class NavCatalog
{
    public static readonly List<NavCatalogItem> Items = Build();

    public static string KeyOf(NavItem item) =>
        $"{item.Area?.ToLowerInvariant() ?? ""}|{item.Controller.ToLowerInvariant()}|{item.Action.ToLowerInvariant()}";

    public static NavCatalogItem? Find(NavItem item) =>
        Items.FirstOrDefault(c => string.Equals(c.Key, KeyOf(item), StringComparison.OrdinalIgnoreCase));

    public static bool IsForTemplate(NavCatalogItem item, string template) =>
        string.IsNullOrWhiteSpace(item.Templates)
        || item.Templates.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                         .Any(t => t.Equals(template, StringComparison.OrdinalIgnoreCase));

    private static List<NavCatalogItem> Build() =>
    [
        // Module pages: available to any template when the module is enabled.
        new() { Key = "|blog|index",           Group = "Modules",    Item = new() { Label = "Blog",          Controller = "Blog",     Action = "Index", Module = "Blog" } },
        new() { Key = "|forum|index",          Group = "Modules",    Item = new() { Label = "Forums",        Controller = "Forum",    Action = "Index", Module = "Forum" } },
        new() { Key = "|chat|rooms",           Group = "Modules",    Item = new() { Label = "Chat Rooms",    Controller = "Chat",     Action = "Rooms", Module = "Chat" } },
        new() { Key = "|video|index",          Group = "Modules",    Item = new() { Label = "Video Chat",    Controller = "Video",    Action = "Index", Module = "Video" } },
        new() { Key = "|messages|index",       Group = "Modules",    Item = new() { Label = "Messages",      Controller = "Messages", Action = "Index", Module = "Messages" } },
        new() { Key = "|store|index",          Group = "Modules",    Item = new() { Label = "Store",         Controller = "Store",    Action = "Index", Module = "Store" } },
        new() { Key = "|iptvhome|index",       Group = "Modules",    Item = new() { Label = "IPTV",          Controller = "IptvHome", Action = "Index", Module = "Iptv" } },
        new() { Key = "|iptvstore|index",      Group = "Modules",    Item = new() { Label = "IPTV Store",   Controller = "IptvStore", Action = "Index", Module = "Iptv" } },
        new() { Key = "|pages|index",          Group = "Modules",    Item = new() { Label = "Pages",         Controller = "Pages",    Action = "Index", Module = "Pages" } },
        new() { Key = "|utility|index",        Group = "Modules",    Item = new() { Label = "Utility",       Controller = "Utility",  Action = "Index", Module = "Utility", RequiresAuth = true } },
        // Pages built specifically for a template (only offered for that template).
        new() { Key = "|iptvhome|setup",       Group = "IPTV Pages", Templates = "Iptv", Item = new() { Label = "Setup",         Controller = "IptvHome",  Action = "Setup", Module = "Iptv" } },
        new() { Key = "|iptvhome|channels",    Group = "IPTV Pages", Templates = "Iptv", Item = new() { Label = "Channel List",  Controller = "IptvHome",  Action = "Channels", Module = "Iptv" } },
        new() { Key = "|iptvadmin|index",      Group = "IPTV Pages", Templates = "Iptv", Item = new() { Label = "IPTV Dashboard", Controller = "IptvAdmin", Action = "Index", Module = "Iptv", RequiresAdmin = true } },
        // Shared host pages.
        new() { Key = "|home|faq",             Group = "Shared",     Item = new() { Label = "FAQ",           Controller = "Home",     Action = "Faq" } },
        new() { Key = "|home|contact",         Group = "Shared",     Item = new() { Label = "Contact",       Controller = "Home",     Action = "Contact" } },
        new() { Key = "|discover|index",       Group = "Shared",     Item = new() { Label = "Discover",      Controller = "Discover", Action = "Index", RequiresAuth = true } },
        new() { Key = "|likes|index",          Group = "Shared",     Item = new() { Label = "Likes",         Controller = "Likes",    Action = "Index", RequiresAuth = true } },
        new() { Key = "|matches|index",        Group = "Shared",     Item = new() { Label = "Matches",       Controller = "Matches",  Action = "Index", RequiresAuth = true } },
        new() { Key = "|notifications|index",  Group = "Shared",     Item = new() { Label = "Notifications", Controller = "Notifications", Action = "Index", RequiresAuth = true } },
        new() { Key = "|gamification|index",   Group = "Shared",     Item = new() { Label = "Points & Badges", Controller = "Gamification", Action = "Index", RequiresAuth = true } },
        new() { Key = "|gamification|leaderboard", Group = "Shared", Item = new() { Label = "Leaderboard", Controller = "Gamification", Action = "Leaderboard", RequiresAuth = true } },
        new() { Key = "admin|home|index",      Group = "Shared",     Item = new() { Label = "Dashboard",     Area = "Admin",         Controller = "Home", Action = "Index", RequiresAdmin = true } },
        new() { Key = "admin|bans|index",      Group = "Shared",     Item = new() { Label = "Moderation",   Area = "Admin",         Controller = "Bans", Action = "Index", RequiresModerator = true } },
        new() { Key = "|home|index",           Group = "Shared",     Item = new() { Label = "Home",         Controller = "Home",     Action = "Index" } }
    ];
}
