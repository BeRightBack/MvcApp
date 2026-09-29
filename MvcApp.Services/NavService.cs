using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Localization;

namespace MvcApp.Services;

public class NavService(
    ISettingsService settings,
    IModuleManager moduleManager,
    IHttpContextAccessor httpContextAccessor,
    IStringLocalizer<SharedResource> localizer) : INavService
{
    public async Task<List<NavItem>> GetNavItemsAsync()
    {
        var template = await settings.GetAsync("SiteTemplate") ?? "Default";
        var items = await GetTemplateNavbarAsync(template);
        return await FilterAsync(items);
    }

    public async Task<List<NavItem>> GetFooterItemsAsync()
    {
        var template = await settings.GetAsync("SiteTemplate") ?? "Default";
        var items = await GetTemplateFooterAsync(template);
        return await FilterAsync(items);
    }

    public async Task<List<NavItem>> GetSocialDropdownAsync()
    {
        var template = await settings.GetAsync("SiteTemplate") ?? "Default";
        var items = await GetTemplateSocialDropdownAsync(template);
        return await FilterAsync(items);
    }

    public async Task<List<NavItem>> GetProfileDropdownAsync()
    {
        var template = await settings.GetAsync("SiteTemplate") ?? "Default";
        var items = await GetTemplateProfileDropdownAsync(template);
        return await FilterAsync(items);
    }

    public async Task<List<NavItem>> GetTemplateNavbarAsync(string template)
    {
        var raw = await settings.GetAsync($"SiteNav.{template}.Navbar");
        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<NavItem>>(raw);
                if (parsed != null) return parsed;
            }
            catch (JsonException)
            {
                // Fall back to defaults for malformed settings.
            }
        }

        // Legacy: SiteNav.{template} held the navbar list before the split.
        var legacy = await settings.GetAsync($"SiteNav.{template}");
        if (!string.IsNullOrWhiteSpace(legacy))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<NavItem>>(legacy);
                if (parsed != null) return parsed;
            }
            catch (JsonException)
            {
                // Fall through to built-in defaults.
            }
        }

        var builtIn = NavDefaults.GetNavbar(template);
        if (builtIn.Count > 0)
            return builtIn;

        // Templates without a curated nav adapt to the modules in use, so the
        // navbar always reflects the enabled modules plus shared host links.
        return NavDefaults.GetGenericNavbar();
    }

    public async Task<List<NavItem>> GetTemplateFooterAsync(string template)
    {
        var raw = await settings.GetAsync($"SiteNav.{template}.Footer");
        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<NavItem>>(raw);
                if (parsed != null) return parsed;
            }
            catch (JsonException)
            {
                // Fall back to defaults for malformed settings.
            }
        }

        var builtIn = NavDefaults.GetFooter(template);
        if (builtIn.Count > 0)
            return builtIn;

        return NavDefaults.GetGenericFooter();
    }

    public async Task<List<NavItem>> GetTemplateSocialDropdownAsync(string template)
    {
        var raw = await settings.GetAsync($"SiteNav.{template}.SocialDropdown");
        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<NavItem>>(raw);
                if (parsed != null) return parsed;
            }
            catch (JsonException)
            {
                // Fall back to defaults for malformed settings.
            }
        }

        return NavDefaults.GetSocialDropdown(template);
    }

    public async Task<List<NavItem>> GetTemplateProfileDropdownAsync(string template)
    {
        var raw = await settings.GetAsync($"SiteNav.{template}.ProfileDropdown");
        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<NavItem>>(raw);
                if (parsed != null) return parsed;
            }
            catch (JsonException)
            {
                // Fall back to defaults for malformed settings.
            }
        }

        return NavDefaults.GetProfileDropdown(template);
    }

    private async Task<List<NavItem>> FilterAsync(IReadOnlyList<NavItem> items)
    {
        var user = httpContextAccessor.HttpContext?.User;
        var result = new List<NavItem>();
        foreach (var item in items)
        {
            if (item.Module != null && !await moduleManager.IsModuleEnabledAsync(item.Module))
                continue;
            if (item.RequiresAdmin && (user == null || !user.IsInRole("Admin")))
                continue;
            if (item.RequiresModerator && (user == null || (!user.IsInRole("Admin") && !user.IsInRole("Moderator"))))
                continue;
            if (item.RequiresAuth && (user?.Identity == null || !user.Identity.IsAuthenticated))
                continue;
            // Nav labels are plain strings - from NavDefaults in code, or from the SiteNav JSON an
            // admin saves - so they are resolved here, at the single funnel every navbar, footer
            // and dropdown list passes through, rather than at each call site. A label with no
            // translation returns itself unchanged and is picked up by the background translator.
            if (!string.IsNullOrWhiteSpace(item.Label))
            {
                item.Label = localizer[item.Label].Value;
            }
            result.Add(item);
        }
        return result;
    }
}
