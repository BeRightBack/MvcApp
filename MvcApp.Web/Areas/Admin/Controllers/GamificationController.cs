using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Common.Filters;
using MvcApp.Core;
using MvcApp.Infrastructure;
using Microsoft.Extensions.Localization;
using MvcApp.Localization;

namespace MvcApp.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
[ModuleEnabledFilter("Gamification")]
public class GamificationController(UserDbContext db, IStringLocalizer<SharedResource> localizer) : Controller
{
    public async Task<IActionResult> Badges()
    {
        var badges = await db.Badges
            .OrderBy(b => b.Category)
            .ThenBy(b => b.SortOrder)
            .ToListAsync();

        ViewBag.TotalUsers = await db.Users.CountAsync();
        ViewBag.TotalAwarded = await db.UserBadges.CountAsync();

        return View(badges);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Award(int badgeId, string userId)
    {
        if (string.IsNullOrEmpty(userId)) return BadRequest();

        var badge = await db.Badges.FindAsync(badgeId);
        if (badge == null) return NotFound();

        var existing = await db.UserBadges
            .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BadgeId == badgeId);
        if (existing != null)
        {
            TempData["Error"] = localizer["User already has this badge."];
            return RedirectToAction(nameof(Badges));
        }

        db.UserBadges.Add(new UserBadge
        {
            UserId = userId,
            BadgeId = badgeId
        });

        await db.SaveChangesAsync();
        TempData["Message"] = $"Badge '{badge.Name}' awarded successfully.";
        return RedirectToAction(nameof(Badges));
    }
}
