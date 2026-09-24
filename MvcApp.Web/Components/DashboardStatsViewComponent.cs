using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Infrastructure;

namespace MvcApp.Web.Components;

public class DashboardStatsViewComponent(
    UserManager<UserDetails> userManager,
    UserDbContext db,
    IModuleManager moduleManager) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var cutOff = DateTime.UtcNow.AddDays(-30);
        var model = new DashboardStatsViewModel
        {
            TotalUsers = await userManager.Users.CountAsync(),
            NewUsers30Days = await userManager.Users.CountAsync(u => u.Created >= cutOff),
            AuditEvents = await db.AuditLogs.CountAsync(),
            EnabledModules = (await moduleManager.GetAllModulesAsync()).Count(m => m.IsEnabled),
            RecentActivity = await db.AuditLogs
                .OrderByDescending(a => a.Timestamp)
                .Take(5)
                .ToListAsync()
        };
        return View(model);
    }
}

public class DashboardStatsViewModel
{
    public int TotalUsers { get; set; }
    public int NewUsers30Days { get; set; }
    public int AuditEvents { get; set; }
    public int EnabledModules { get; set; }
    public List<AuditLog> RecentActivity { get; set; } = [];
}
