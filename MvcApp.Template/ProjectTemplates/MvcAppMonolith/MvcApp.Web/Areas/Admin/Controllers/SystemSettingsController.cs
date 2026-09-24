using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Infrastructure;
using System.Security.Claims;

namespace MvcApp.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SystemSettingsController(UserDbContext db, IAuditService auditService) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var settings = await db.SystemSettings.OrderBy(s => s.Group).ThenBy(s => s.Key).ToListAsync();
            var groups = settings.GroupBy(s => s.Group ?? "General").ToDictionary(g => g.Key, g => g.ToList());
            return View(groups);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var setting = await db.SystemSettings.FindAsync(id);
            if (setting == null) return NotFound();
            return View(setting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Key,Value,Description,Group")] SystemSetting setting)
        {
            if (id != setting.Id) return NotFound();
            if (ModelState.IsValid)
            {
                var existing = await db.SystemSettings.FindAsync(id);
                if (existing == null) return NotFound();
                existing.Value = setting.Value;
                existing.Description = setting.Description;
                existing.Group = setting.Group;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await db.SaveChangesAsync();
                await auditService.LogAsync("Edit", "SystemSetting", setting.Key, $"Updated setting '{setting.Key}'");
                return RedirectToAction(nameof(Index));
            }
            return View(setting);
        }

        public IActionResult Create() => View(new SystemSetting());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Key,Value,Description,Group")] SystemSetting setting)
        {
            if (ModelState.IsValid)
            {
                setting.UpdatedAt = DateTime.UtcNow;
                setting.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
                db.SystemSettings.Add(setting);
                await db.SaveChangesAsync();
                await auditService.LogAsync("Create", "SystemSetting", setting.Key, $"Created setting '{setting.Key}' = '{setting.Value}'");
                return RedirectToAction(nameof(Index));
            }
            return View(setting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var setting = await db.SystemSettings.FindAsync(id);
            if (setting != null)
            {
                db.SystemSettings.Remove(setting);
                await db.SaveChangesAsync();
                await auditService.LogAsync("Delete", "SystemSetting", setting.Key, $"Deleted setting '{setting.Key}'");
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
