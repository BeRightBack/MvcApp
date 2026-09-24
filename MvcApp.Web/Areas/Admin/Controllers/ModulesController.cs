using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvcApp.Core.Abstractions;

namespace MvcApp.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ModulesController(IModuleManager moduleManager, IAuditService auditService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var modules = await moduleManager.GetAllModulesAsync();
        return View(modules);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(string name, bool enabled)
    {
        await moduleManager.SetModuleEnabledAsync(name, enabled);
        await auditService.LogAsync("Update", "Module", name, $"{(enabled ? "Enabled" : "Disabled")} module '{name}'");
        TempData["Success"] = $"Module '{(enabled ? "enabled" : "disabled")}'.";
        return RedirectToAction(nameof(Index));
    }
}