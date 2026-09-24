using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Web.Areas.Admin.Models;

namespace MvcApp.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class TemplateNavController : Controller
{
    private readonly ITemplateService _templateService;
    private readonly ISettingsService _settings;
    private readonly IModuleManager _moduleManager;
    private readonly INavService _navService;
    private readonly IAuditService _auditService;
    private readonly IRepository<ContentPage> _pageRepo;

    public TemplateNavController(
        ITemplateService templateService,
        ISettingsService settings,
        IModuleManager moduleManager,
        INavService navService,
        IAuditService auditService,
        IRepository<ContentPage> pageRepo)
    {
        _templateService = templateService;
        _settings = settings;
        _moduleManager = moduleManager;
        _navService = navService;
        _auditService = auditService;
        _pageRepo = pageRepo;
    }

    public async Task<IActionResult> Index(string? template)
    {
        var templates = await _templateService.GetAvailableTemplatesAsync();
        var active = await _templateService.GetActiveTemplateAsync();
        var name = template ?? active;
        var info = templates.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (info == null)
            return NotFound();

        name = info.Name;
        var navbar = await _navService.GetTemplateNavbarAsync(name);
        var footer = await _navService.GetTemplateFooterAsync(name);
        var socialDropdown = await _navService.GetTemplateSocialDropdownAsync(name);
        var profileDropdown = await _navService.GetTemplateProfileDropdownAsync(name);
        var modules = await _moduleManager.GetAllModulesAsync();
        var pages = await _pageRepo.Query().OrderBy(p => p.Title).ToListAsync();

        var vm = new TemplateNavViewModel
        {
            Template = name,
            TemplateDisplayName = info.DisplayName,
            Templates = templates,
            NavbarItems = BuildEditorItems(navbar, name, modules, pages),
            FooterItems = BuildEditorItems(footer, name, modules, pages),
            SocialDropdownItems = BuildEditorItems(socialDropdown, name, modules, pages),
            ProfileDropdownItems = BuildEditorItems(profileDropdown, name, modules, pages)
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(string template, IFormCollection form)
    {
        var templates = await _templateService.GetAvailableTemplatesAsync();
        if (!templates.Any(t => t.Name.Equals(template, StringComparison.OrdinalIgnoreCase)))
            return NotFound();

        var pages = await _pageRepo.Query().OrderBy(p => p.Title).ToListAsync();

        var navbar = BuildNavFromForm(template, form, "navbar", pages);
        var footer = BuildNavFromForm(template, form, "footer", pages);
        var socialDropdown = BuildDropdownFromForm(form, "social", pages);
        var profileDropdown = BuildDropdownFromForm(form, "profile", pages);
        var socialIcon = form["socialIcon"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(socialIcon)) socialIcon = "bx bx-heart";

        await _settings.SetAsync($"SiteNav.{template}.Navbar", JsonSerializer.Serialize(navbar), User.Identity?.Name);
        await _settings.SetAsync($"SiteNav.{template}.Footer", JsonSerializer.Serialize(footer), User.Identity?.Name);
        await _settings.SetAsync($"SiteNav.{template}.SocialDropdown", JsonSerializer.Serialize(socialDropdown), User.Identity?.Name);
        await _settings.SetAsync($"SiteNav.{template}.ProfileDropdown", JsonSerializer.Serialize(profileDropdown), User.Identity?.Name);
        await _settings.SetAsync($"SiteNav.{template}.SocialDropdownIcon", socialIcon, User.Identity?.Name);
        await _auditService.LogAsync("Update", "TemplateNav", template, $"Updated navigation for template '{template}'");
        TempData["Success"] = $"Navigation for '{template}' saved.";
        return RedirectToAction(nameof(Index), new { template });
    }

    private List<TemplateNavEditorItem> BuildEditorItems(List<NavItem> current, string template, List<ModuleInfo> modules, List<ContentPage> pages)
    {
        var items = new List<TemplateNavEditorItem>();
        foreach (var cat in NavCatalog.Items.Where(c => NavCatalog.IsForTemplate(c, template)))
        {
            var inNav = current.Any(i => string.Equals(NavCatalog.KeyOf(i), cat.Key, StringComparison.OrdinalIgnoreCase));
            items.Add(new TemplateNavEditorItem
            {
                Key = cat.Key,
                Group = cat.Group,
                Label = cat.Item.Label,
                Area = cat.Item.Area,
                Controller = cat.Item.Controller,
                Action = cat.Item.Action,
                Module = cat.Item.Module,
                RequiresAuth = cat.Item.RequiresAuth,
                RequiresAdmin = cat.Item.RequiresAdmin,
                RequiresModerator = cat.Item.RequiresModerator,
                IsChecked = inNav,
                ModuleEnabled = cat.Item.Module == null
                    ? null
                    : modules.FirstOrDefault(m => m.Name.Equals(cat.Item.Module, StringComparison.OrdinalIgnoreCase))?.IsEnabled
            });
        }

        var pagesEnabled = modules.FirstOrDefault(m => m.Name.Equals("Pages", StringComparison.OrdinalIgnoreCase))?.IsEnabled;
        foreach (var page in pages.Where(p => p.IsPublished))
        {
            var inNav = current.Any(i =>
                string.Equals(i.Controller, "Pages", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(i.Action, "View", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(i.Slug, page.Slug, StringComparison.OrdinalIgnoreCase));
            items.Add(new TemplateNavEditorItem
            {
                Key = $"pages|{page.Slug}",
                Group = "Pages",
                Label = page.Title,
                Controller = "Pages",
                Action = "View",
                Slug = page.Slug,
                Module = "Pages",
                IsChecked = inNav,
                ModuleEnabled = pagesEnabled
            });
        }

        var index = 0;
        foreach (var i in current)
        {
            if (NavCatalog.Find(i) != null)
                continue;
            if (string.Equals(i.Controller, "Pages", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(i.Action, "View", StringComparison.OrdinalIgnoreCase))
                continue;
            items.Add(new TemplateNavEditorItem
            {
                Index = index++,
                Group = "Custom Links",
                Label = i.Label,
                Area = i.Area,
                Controller = i.Controller,
                Action = i.Action,
                Slug = i.Slug,
                Module = i.Module,
                RequiresAuth = i.RequiresAuth,
                RequiresAdmin = i.RequiresAdmin,
                IsChecked = true,
                IsAdditional = true,
                ModuleEnabled = i.Module == null
                    ? null
                    : modules.FirstOrDefault(m => m.Name.Equals(i.Module, StringComparison.OrdinalIgnoreCase))?.IsEnabled
            });
        }

        return items;
    }

    private List<NavItem> BuildNavFromForm(string template, IFormCollection form, string section, List<ContentPage> pages)
    {
        var nav = new List<NavItem>();
        var selected = form[$"{section}.selected"];

        foreach (var cat in NavCatalog.Items.Where(c => NavCatalog.IsForTemplate(c, template)))
        {
            if (!selected.Contains(cat.Key, StringComparer.OrdinalIgnoreCase))
                continue;
            nav.Add(new NavItem
            {
                Label = cat.Item.Label,
                Area = cat.Item.Area,
                Controller = cat.Item.Controller,
                Action = cat.Item.Action,
                Module = cat.Item.Module,
                RequiresAuth = cat.Item.RequiresAuth,
                RequiresAdmin = cat.Item.RequiresAdmin
            });
        }

        foreach (var key in selected)
        {
            if (key == null || !key.StartsWith("pages|", StringComparison.OrdinalIgnoreCase))
                continue;
            var slug = key["pages|".Length..];
            var page = pages.FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase) && p.IsPublished);
            if (page == null)
                continue;
            nav.Add(new NavItem
            {
                Label = page.Title,
                Controller = "Pages",
                Action = "View",
                Slug = page.Slug,
                Module = "Pages"
            });
        }

        var customPrefix = $"{section}Custom";
        var keep = form[$"{customPrefix}.Keep"].ToArray();
        var labels = form[$"{customPrefix}.Label"].ToArray();
        var areas = form[$"{customPrefix}.Area"].ToArray();
        var controllers = form[$"{customPrefix}.Controller"].ToArray();
        var actions = form[$"{customPrefix}.Action"].ToArray();
        var modules = form[$"{customPrefix}.Module"].ToArray();
        var requiresAuth = form[$"{customPrefix}.RequiresAuth"].ToArray();
        var requiresAdmin = form[$"{customPrefix}.RequiresAdmin"].ToArray();
        var requiresModerator = form[$"{customPrefix}.RequiresModerator"].ToArray();
        var indexes = form[$"{customPrefix}.Index"].ToArray();

        for (var n = 0; n < indexes.Length; n++)
        {
            if (!keep.Contains(indexes[n]))
                continue;
            if (string.IsNullOrWhiteSpace(labels[n]) || string.IsNullOrWhiteSpace(controllers[n]))
                continue;
            nav.Add(new NavItem
            {
                Label = labels[n] ?? string.Empty,
                Area = string.IsNullOrWhiteSpace(areas[n]) ? null : areas[n],
                Controller = controllers[n] ?? string.Empty,
                Action = string.IsNullOrWhiteSpace(actions[n]) ? "Index" : actions[n] ?? "Index",
                Module = string.IsNullOrWhiteSpace(modules[n]) ? null : modules[n],
                RequiresAuth = requiresAuth.Length > n && requiresAuth[n] == "true",
                RequiresAdmin = requiresAdmin.Length > n && requiresAdmin[n] == "true",
                RequiresModerator = requiresModerator.Length > n && requiresModerator[n] == "true"
            });
        }

        var newPrefix = $"{section}New";
        var newLabel = form[$"{newPrefix}Label"].ToString().Trim();
        var newController = form[$"{newPrefix}Controller"].ToString().Trim();
        if (!string.IsNullOrWhiteSpace(newLabel) && !string.IsNullOrWhiteSpace(newController))
        {
            nav.Add(new NavItem
            {
                Label = newLabel,
                Area = string.IsNullOrWhiteSpace(form[$"{newPrefix}Area"].ToString()) ? null : form[$"{newPrefix}Area"].ToString(),
                Controller = newController,
                Action = string.IsNullOrWhiteSpace(form[$"{newPrefix}Action"].ToString()) ? "Index" : form[$"{newPrefix}Action"].ToString(),
                Module = string.IsNullOrWhiteSpace(form[$"{newPrefix}Module"].ToString()) ? null : form[$"{newPrefix}Module"].ToString(),
                RequiresAdmin = form[$"{newPrefix}RequiresAdmin"].ToString() == "true"
            });
        }

        return nav;
    }

    private List<NavItem> BuildDropdownFromForm(IFormCollection form, string section, List<ContentPage> pages)
    {
        var nav = new List<NavItem>();
        var selected = form[$"{section}.selected"];

        foreach (var cat in NavCatalog.Items.Where(c => c.Group == "Shared" || c.Group == "Modules"))
        {
            if (!selected.Contains(cat.Key, StringComparer.OrdinalIgnoreCase))
                continue;
            nav.Add(new NavItem
            {
                Label = cat.Item.Label,
                Area = cat.Item.Area,
                Controller = cat.Item.Controller,
                Action = cat.Item.Action,
                Module = cat.Item.Module,
                RequiresAuth = cat.Item.RequiresAuth,
                RequiresAdmin = cat.Item.RequiresAdmin
            });
        }

        var dividers = form[$"{section}.dividers"];
        var dividerPositions = new HashSet<int>();
        foreach (var pos in dividers)
        {
            if (int.TryParse(pos, out var idx))
                dividerPositions.Add(idx);
        }

        var customPrefix = $"{section}Custom";
        var keep = form[$"{customPrefix}.Keep"].ToArray();
        var labels = form[$"{customPrefix}.Label"].ToArray();
        var areas = form[$"{customPrefix}.Area"].ToArray();
        var controllers = form[$"{customPrefix}.Controller"].ToArray();
        var actions = form[$"{customPrefix}.Action"].ToArray();
        var indexes = form[$"{customPrefix}.Index"].ToArray();

        for (var n = 0; n < indexes.Length; n++)
        {
            if (!keep.Contains(indexes[n]))
                continue;
            if (string.IsNullOrWhiteSpace(labels[n]) || string.IsNullOrWhiteSpace(controllers[n]))
                continue;
            nav.Add(new NavItem
            {
                Label = labels[n] ?? string.Empty,
                Area = string.IsNullOrWhiteSpace(areas[n]) ? null : areas[n],
                Controller = controllers[n] ?? string.Empty,
                Action = string.IsNullOrWhiteSpace(actions[n]) ? "Index" : actions[n] ?? "Index"
            });
        }

        return nav;
    }
}
