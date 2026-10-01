using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvcApp.Core.Abstractions;
using MvcApp.Infrastructure;
using MvcApp.Infrastructure.Seeding;
using MvcApp.Infrastructure.Seeding.Packs;

namespace MvcApp.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class SeedPacksController(
    UserDbContext db,
    SeedPackService packs,
    SeedPackPlanner planner,
    IEnumerable<ISeedPack> allPacks,
    ITemplateService templateService,
    ITemplateProfileService profiles,
    IAuditService auditService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? template, CancellationToken ct)
    {
        var active = await templateService.GetActiveTemplateAsync();
        var selected = string.IsNullOrWhiteSpace(template) ? active : template;

        ViewBag.ActiveTemplate = active;
        ViewBag.SelectedTemplate = selected;
        ViewBag.Packs = await planner.PlanAsync(selected, ct);
        ViewBag.Templates = await templateService.GetAvailableTemplatesAsync();
        ViewBag.PackDetails = allPacks
            .Select(p => new { p.Name, p.DisplayName, p.Description })
            .ToList();
        ViewBag.Profiles = profiles.GetProfiles()
            .Select(p => new { p.Template, p.Purpose, Packs = string.Join(", ", p.Packs) })
            .ToList();

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(string pack, CancellationToken ct)
    {
        var found = Find(pack);
        if (found is null)
            return Unknown(pack);

        var before = await RowCountAsync(found.Name, ct);
        await found.SeedAsync(packs, db, "admin", ct);
        var added = Math.Max(0, await RowCountAsync(found.Name, ct) - before);

        await auditService.LogAsync("Apply", "SeedPack", found.Name,
            $"Applied seed pack: {added} row(s) added.");

        TempData["Success"] = added > 0
            ? $"Applied '{found.DisplayName}': {added} row(s) added."
            : $"'{found.DisplayName}' was already applied.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(string pack, CancellationToken ct)
    {
        var found = Find(pack);
        if (found is null)
            return Unknown(pack);

        var removal = await packs.RemoveAsync(found.Name, ct);

        await auditService.LogAsync("Remove", "SeedPack", found.Name,
            $"Removed seed pack: {removal.RowsDeleted} row(s) deleted.");

        TempData["Success"] = $"Removed '{found.DisplayName}': {removal.RowsDeleted} row(s) deleted. "
            + "Applying it again later is a clean insert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyProfile(string? template, CancellationToken ct)
    {
        var target = string.IsNullOrWhiteSpace(template)
            ? await templateService.GetActiveTemplateAsync()
            : template;

        var plan = await planner.ApplyForTemplateAsync(target, "admin", ct);
        var unused = plan.Count(i => i.Applied && !i.Needed);

        await auditService.LogAsync("Apply", "SeedProfile", target,
            $"Applied the {target} seed profile: {plan.Count(i => i.Applied)} pack(s) on disk.");

        TempData["Success"] = $"Applied the '{target}' profile: "
            + $"{plan.Count(i => i.Applied)} pack(s) on disk"
            + (unused > 0 ? $", {unused} of them not used by this template (left alone)." : ".");
        return RedirectToAction(nameof(Index));
    }

    private ISeedPack? Find(string pack) =>
        allPacks.FirstOrDefault(p => p.Name.Equals(pack, StringComparison.OrdinalIgnoreCase));

    private IActionResult Unknown(string pack)
    {
        TempData["Error"] = $"Unknown seed pack '{pack}'.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<int> RowCountAsync(string pack, CancellationToken ct) =>
        (await packs.DescribeAsync(pack, ct)).Sum(r => r.Rows);
}
