namespace MvcApp.Core;

public static class NavDefaults
{
    private static readonly Dictionary<string, List<NavItem>> _byTemplate = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Default"] =
        [
            new() { Label = "Blog",          Controller = "Blog",     Action = "Index", Module = "Blog" },
            new() { Label = "Forums",        Controller = "Forum",    Action = "Index", Module = "Forum" },
            new() { Label = "Chat Rooms",    Controller = "Chat",     Action = "Rooms", Module = "Chat" },
            new() { Label = "Messages",      Controller = "Messages", Action = "Index", Module = "Messages" },
            new() { Label = "Store",         Controller = "Store",    Action = "Index", Module = "Store" },
            new() { Label = "IPTV Store",   Controller = "IptvStore", Action = "Index", Module = "Iptv" },
            new() { Label = "Pages",         Controller = "Pages",    Action = "Index", Module = "Pages" },
            new() { Label = "Utility",       Controller = "Utility",  Action = "Index", Module = "Utility", RequiresAuth = true },
            new() { Label = "Discover",      Controller = "Discover", Action = "Index", RequiresAuth = true },
            new() { Label = "Likes",         Controller = "Likes",    Action = "Index", RequiresAuth = true },
            new() { Label = "Matches",       Controller = "Matches",  Action = "Index", RequiresAuth = true },
            new() { Label = "FAQ",           Controller = "Home",     Action = "Faq" },
            new() { Label = "Contact",       Controller = "Home",     Action = "Contact" }
        ],
        ["Dating"] =
        [
            new() { Label = "Chat Rooms",    Controller = "Chat",     Action = "Rooms", Module = "Chat" },
            new() { Label = "Messages",      Controller = "Messages", Action = "Index", Module = "Messages" },
            new() { Label = "Video Chat",    Controller = "Video",    Action = "Index", Module = "Video" },
            new() { Label = "Discover",      Controller = "Discover", Action = "Index", RequiresAuth = true },
            new() { Label = "Likes",         Controller = "Likes",    Action = "Index", RequiresAuth = true },
            new() { Label = "Points & Badges", Controller = "Gamification", Action = "Index", RequiresAuth = true },
            new() { Label = "FAQ",           Controller = "Home",     Action = "Faq" },
            new() { Label = "Contact",       Controller = "Home",     Action = "Contact" },
            new() { Label = "Blog",          Controller = "Blog",     Action = "Index", Module = "Blog" },
            new() { Label = "Forums",        Controller = "Forum",    Action = "Index", Module = "Forum" }
        ],
        ["Store"] =
        [
            new() { Label = "Store",         Controller = "Store",    Action = "Index", Module = "Store" },
            new() { Label = "FAQ",           Controller = "Home",     Action = "Faq" },
            new() { Label = "Contact",       Controller = "Home",     Action = "Contact" },
            new() { Label = "Blog",          Controller = "Blog",     Action = "Index", Module = "Blog" },
            new() { Label = "Forums",        Controller = "Forum",    Action = "Index", Module = "Forum" }
        ],
        ["Business"] =
        [
            new() { Label = "Pages",         Controller = "Pages",    Action = "Index", Module = "Pages" },
            new() { Label = "FAQ",           Controller = "Home",     Action = "Faq" },
            new() { Label = "Contact",       Controller = "Home",     Action = "Contact" },
            new() { Label = "Blog",          Controller = "Blog",     Action = "Index", Module = "Blog" },
            new() { Label = "Forums",        Controller = "Forum",    Action = "Index", Module = "Forum" },
            new() { Label = "Utility",       Controller = "Utility",  Action = "Index", Module = "Utility", RequiresAuth = true }
        ],
        ["Magazine"] =
        [
            new() { Label = "Blog",          Controller = "Blog",     Action = "Index", Module = "Blog" },
            new() { Label = "Forums",        Controller = "Forum",    Action = "Index", Module = "Forum" },
            new() { Label = "Pages",         Controller = "Pages",    Action = "Index", Module = "Pages" },
            new() { Label = "FAQ",           Controller = "Home",     Action = "Faq" },
            new() { Label = "Contact",       Controller = "Home",     Action = "Contact" },
            new() { Label = "Chat Rooms",    Controller = "Chat",     Action = "Rooms", Module = "Chat" }
        ],
        ["Professional"] =
        [
            new() { Label = "Blog",          Controller = "Blog",     Action = "Index", Module = "Blog" },
            new() { Label = "Pages",         Controller = "Pages",    Action = "Index", Module = "Pages" },
            new() { Label = "Forums",        Controller = "Forum",    Action = "Index", Module = "Forum" },
            new() { Label = "FAQ",           Controller = "Home",     Action = "Faq" },
            new() { Label = "Contact",       Controller = "Home",     Action = "Contact" },
            new() { Label = "Utility",       Controller = "Utility",  Action = "Index", Module = "Utility", RequiresAuth = true }
        ],
        ["Gaming"] =
        [
            new() { Label = "Forums",        Controller = "Forum",    Action = "Index", Module = "Forum" },
            new() { Label = "Chat Rooms",    Controller = "Chat",     Action = "Rooms", Module = "Chat" },
            new() { Label = "Blog",          Controller = "Blog",     Action = "Index", Module = "Blog" },
            new() { Label = "Store",         Controller = "Store",    Action = "Index", Module = "Store" },
            new() { Label = "FAQ",           Controller = "Home",     Action = "Faq" },
            new() { Label = "Contact",       Controller = "Home",     Action = "Contact" },
            new() { Label = "Utility",       Controller = "Utility",  Action = "Index", Module = "Utility", RequiresAuth = true }
        ],
        ["Minimal"] =
        [
            new() { Label = "Blog",          Controller = "Blog",     Action = "Index", Module = "Blog" },
            new() { Label = "Pages",         Controller = "Pages",    Action = "Index", Module = "Pages" },
            new() { Label = "FAQ",           Controller = "Home",     Action = "Faq" },
            new() { Label = "Contact",       Controller = "Home",     Action = "Contact" }
        ],
        ["Luxury"] =
        [
            new() { Label = "Chat Rooms",    Controller = "Chat",     Action = "Rooms", Module = "Chat" },
            new() { Label = "Messages",      Controller = "Messages", Action = "Index", Module = "Messages" },
            new() { Label = "Video Chat",    Controller = "Video",    Action = "Index", Module = "Video" },
            new() { Label = "Discover",      Controller = "Discover", Action = "Index", RequiresAuth = true },
            new() { Label = "Likes",         Controller = "Likes",    Action = "Index", RequiresAuth = true },
            new() { Label = "Matches",       Controller = "Matches",  Action = "Index", RequiresAuth = true },
            new() { Label = "Store",         Controller = "Store",    Action = "Index", Module = "Store" },
            new() { Label = "Pages",         Controller = "Pages",    Action = "Index", Module = "Pages" },
            new() { Label = "FAQ",           Controller = "Home",     Action = "Faq" },
            new() { Label = "Contact",       Controller = "Home",     Action = "Contact" },
            new() { Label = "Blog",          Controller = "Blog",     Action = "Index", Module = "Blog" },
            new() { Label = "Forums",        Controller = "Forum",    Action = "Index", Module = "Forum" }
        ],
        ["Social"] =
        [
            new() { Label = "Chat Rooms",    Controller = "Chat",     Action = "Rooms", Module = "Chat" },
            new() { Label = "Messages",      Controller = "Messages", Action = "Index", Module = "Messages" },
            new() { Label = "Video Chat",    Controller = "Video",    Action = "Index", Module = "Video" },
            new() { Label = "Discover",      Controller = "Discover", Action = "Index", RequiresAuth = true },
            new() { Label = "Likes",         Controller = "Likes",    Action = "Index", RequiresAuth = true },
            new() { Label = "Matches",       Controller = "Matches",  Action = "Index", RequiresAuth = true },
            new() { Label = "Points & Badges", Controller = "Gamification", Action = "Index", RequiresAuth = true },
            new() { Label = "FAQ",           Controller = "Home",     Action = "Faq" },
            new() { Label = "Contact",       Controller = "Home",     Action = "Contact" },
            new() { Label = "Blog",          Controller = "Blog",     Action = "Index", Module = "Blog" },
            new() { Label = "Forums",        Controller = "Forum",    Action = "Index", Module = "Forum" }
        ],
        ["Iptv"] =
        [
            new() { Label = "Blog",          Controller = "Blog",     Action = "Index", Module = "Blog" },
            new() { Label = "Forums",        Controller = "Forum",    Action = "Index", Module = "Forum" },
            new() { Label = "IPTV Store",   Controller = "IptvStore", Action = "Index", Module = "Iptv" },
            new() { Label = "FAQ",           Controller = "Home",     Action = "Faq" },
            new() { Label = "Contact",       Controller = "Home",     Action = "Contact" }
        ]
    };

    public static readonly IReadOnlyDictionary<string, NavItem> ModuleHome =
        new Dictionary<string, NavItem>(StringComparer.OrdinalIgnoreCase)
        {
            ["Blog"] = new() { Label = "Blog",          Controller = "Blog",     Action = "Index", Module = "Blog" },
            ["Forum"] = new() { Label = "Forums",       Controller = "Forum",    Action = "Index", Module = "Forum" },
            ["Chat"] = new() { Label = "Chat Rooms",    Controller = "Chat",     Action = "Rooms", Module = "Chat" },
            ["Video"] = new() { Label = "Video Chat",   Controller = "Video",    Action = "Index", Module = "Video" },
            ["Messages"] = new() { Label = "Messages",  Controller = "Messages", Action = "Index", Module = "Messages" },
            ["Store"] = new() { Label = "Store",        Controller = "Store",    Action = "Index", Module = "Store" },
            ["Iptv"] = new() { Label = "IPTV Store", Controller = "IptvStore", Action = "Index", Module = "Iptv" },
            ["Pages"] = new() { Label = "Pages",        Controller = "Pages",    Action = "Index", Module = "Pages" },
            ["Utility"] = new() { Label = "Utility", Controller = "Utility", Action = "Index", Module = "Utility", RequiresAuth = true }
        };

    public static List<NavItem> GetNavbar(string template)
    {
        if (!_byTemplate.TryGetValue(template, out var items))
            return new List<NavItem>();

        return items.Select(Clone).ToList();
    }

    /// <summary>
    /// Footer defaults are intentionally minimal and template-independent:
    /// Home and the admin Dashboard are already hardcoded in the shared layout,
    /// so this only supplies FAQ and Contact.
    /// </summary>
    public static List<NavItem> GetFooter(string template)
    {
        _ = template;
        var footer = GetGenericFooter();
        footer.Add(new NavItem { Label = "Dashboard", Area = "Admin", Controller = "Home", Action = "Index", RequiresAdmin = true });
        footer.Add(new NavItem { Label = "Moderation", Area = "Admin", Controller = "Bans", Action = "Index", RequiresModerator = true });
        return footer;
    }

    private static readonly Dictionary<string, List<NavItem>> _socialDropdownDefaults = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Dating"] =
        [
            new() { Label = "Discover",  Controller = "Discover", Action = "Index", RequiresAuth = true },
            new() { Label = "Likes",     Controller = "Likes",    Action = "Index", RequiresAuth = true },
            new() { Label = "Matches",   Controller = "Matches",  Action = "Index", RequiresAuth = true },
            new() { Label = "Messages",  Controller = "Messages", Action = "Index", Module = "Messages" },
            new() { Type = NavItemType.Divider },
            new() { Label = "Go VIP",    Controller = "Vip",      Action = "Index", RequiresAuth = true, Icon = "bx bx-crown" },
            new() { Label = "Gallery",   Controller = "Gallery",  Action = "Index" },
            new() { Label = "Videos",    Controller = "VideoUpload", Action = "Index" },
            new() { Label = "Events",    Controller = "Events",   Action = "Index" }
        ],
        ["Social"] =
        [
            new() { Label = "Discover",  Controller = "Discover", Action = "Index", RequiresAuth = true },
            new() { Label = "Likes",     Controller = "Likes",    Action = "Index", RequiresAuth = true },
            new() { Label = "Matches",   Controller = "Matches",  Action = "Index", RequiresAuth = true },
            new() { Type = NavItemType.Divider },
            new() { Label = "Messages",  Controller = "Messages", Action = "Index", Module = "Messages" },
            new() { Label = "Gallery",   Controller = "Gallery",  Action = "Index" },
            new() { Label = "Events",    Controller = "Events",   Action = "Index" }
        ],
        ["Luxury"] =
        [
            new() { Label = "Discover",  Controller = "Discover", Action = "Index", RequiresAuth = true },
            new() { Label = "Likes",     Controller = "Likes",    Action = "Index", RequiresAuth = true },
            new() { Label = "Matches",   Controller = "Matches",  Action = "Index", RequiresAuth = true },
            new() { Type = NavItemType.Divider },
            new() { Label = "Messages",  Controller = "Messages", Action = "Index", Module = "Messages" },
            new() { Label = "Gallery",   Controller = "Gallery",  Action = "Index" }
        ],
        ["Default"] =
        [
            new() { Label = "Discover",  Controller = "Discover", Action = "Index", RequiresAuth = true },
            new() { Label = "Likes",     Controller = "Likes",    Action = "Index", RequiresAuth = true },
            new() { Label = "Matches",   Controller = "Matches",  Action = "Index", RequiresAuth = true },
            new() { Type = NavItemType.Divider },
            new() { Label = "Messages",  Controller = "Messages", Action = "Index", Module = "Messages" }
        ]
    };

    public static List<NavItem> GetSocialDropdown(string template)
    {
        if (_socialDropdownDefaults.TryGetValue(template, out var items))
            return items.Select(Clone).ToList();
        return [];
    }

    private static readonly List<NavItem> _profileDropdownDefaults =
    [
        new() { Label = "Profile",          Controller = "Account",  Action = "Manage", RequiresAuth = true },
        new() { Type = NavItemType.Divider },
        new() { Label = "Go VIP",           Controller = "Vip",      Action = "Index", RequiresAuth = true, Icon = "bx bx-crown" },
        new() { Label = "Points & Badges",  Controller = "Gamification", Action = "Index", RequiresAuth = true },
        new() { Label = "Leaderboard",      Controller = "Gamification", Action = "Leaderboard", RequiresAuth = true },
        new() { Label = "Get Verified",     Controller = "Verification", Action = "Index", RequiresAuth = true },
        new() { Label = "My Reports",       Controller = "Report",   Action = "MyReports", RequiresAuth = true },
        new() { Type = NavItemType.Divider },
        new() { Label = "Admin Panel",      Area = "Admin", Controller = "Home", Action = "Index", RequiresAdmin = true, Icon = "bx bx-cog" },
        new() { Label = "Sign Out",         Controller = "Account",  Action = "Logout" }
    ];

    public static List<NavItem> GetProfileDropdown(string template)
    {
        _ = template;
        return _profileDropdownDefaults.Select(Clone).ToList();
    }

    /// <summary>
    /// Generic navbar used when a template has no curated nav definition: every
    /// enabled module is listed (disabled modules are dropped by
    /// NavService.FilterAsync via the Module field), followed by shared host
    /// links. Per-template curation is done through the SiteNav.{template}
    /// settings, which let a template view include or remove modules at will.
    /// </summary>
    public static List<NavItem> GetGenericNavbar()
    {
        var items = new List<NavItem>();
        foreach (var pair in ModuleHome)
            items.Add(Clone(pair.Value));
        items.Add(new NavItem { Label = "FAQ", Controller = "Home", Action = "Faq" });
        items.Add(new NavItem { Label = "Contact", Controller = "Home", Action = "Contact" });
        return items;
    }

    public static List<NavItem> GetGenericFooter()
    {
        return
        [
            new NavItem { Label = "FAQ", Controller = "Home", Action = "Faq" },
            new NavItem { Label = "Contact", Controller = "Home", Action = "Contact" }
        ];
    }

    private static NavItem Clone(NavItem i) => new()
    {
        Label = i.Label,
        Area = i.Area,
        Controller = i.Controller,
        Action = i.Action,
        Slug = i.Slug,
        Module = i.Module,
        RequiresAuth = i.RequiresAuth,
        RequiresAdmin = i.RequiresAdmin,
        RequiresModerator = i.RequiresModerator,
        Type = i.Type,
        Icon = i.Icon
    };
}