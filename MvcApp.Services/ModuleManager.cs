using MvcApp.Core.Abstractions;

namespace MvcApp.Services;

public class ModuleManager(ISettingsService settings) : IModuleManager
{
    private static readonly List<ModuleInfo> _knownModules =
    [
        new() { Name = "Forum", DisplayName = "Forums", Description = "Community discussion forums with categories, threads, and posts" },
        new() { Name = "Blog",  DisplayName = "Blog",   Description = "Blog with categories, tags, comments, and search" },
        new() { Name = "Chat",     DisplayName = "Chat",     Description = "Real-time chat rooms" },
        new() { Name = "Video",    DisplayName = "Video Chat", Description = "Live video chat rooms with spot broadcasting and 1-on-1 video calls" },
        new() { Name = "Messages", DisplayName = "Messages", Description = "Private direct messaging between users" },
        new() { Name = "Store",    DisplayName = "Store",    Description = "Online store with products, cart, checkout, and orders" },
        new() { Name = "Iptv",     DisplayName = "IPTV",     Description = "IPTV subscription service with plans, cart, and payments" },
        new() { Name = "Pages",    DisplayName = "Pages",    Description = "CMS pages with Razor-syntax bodies usable on any template" },
        new() { Name = "Ads",      DisplayName = "Ads",      Description = "Ad banner management with zones, scheduling, targeting, and tracking" },
        new() { Name = "Utility",  DisplayName = "Utility",  Description = "Projects, tasks and subtasks manager" },
        new() { Name = "Events",   DisplayName = "Events",   Description = "Local events with RSVP, categories, and city/date filters" },
        new() { Name = "Gamification", DisplayName = "Gamification", Description = "Points, badges, and leaderboards to reward user engagement" },
    ];

    public async Task<bool> IsModuleEnabledAsync(string moduleName)
    {
        var val = await settings.GetAsync<bool>($"Module.{moduleName}.Enabled");
        return val ?? false;
    }

    public async Task<List<ModuleInfo>> GetAllModulesAsync()
    {
        var result = new List<ModuleInfo>();
        foreach (var m in _knownModules)
        {
            var val = await settings.GetAsync($"Module.{m.Name}.Enabled");
            if (val == null)
                continue;
            result.Add(new ModuleInfo
            {
                Name = m.Name,
                DisplayName = m.DisplayName,
                Description = m.Description,
                IsEnabled = val.Equals("true", StringComparison.OrdinalIgnoreCase)
            });
        }
        return result;
    }

    public async Task SetModuleEnabledAsync(string moduleName, bool enabled)
    {
        await settings.SetAsync($"Module.{moduleName}.Enabled", enabled.ToString().ToLower());
    }
}