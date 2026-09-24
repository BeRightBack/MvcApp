using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using System.Security.Claims;

namespace MvcApp.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Moderator")]
    public class BansController(
        IBanService banService,
        UserManager<UserDetails> userManager,
        IAuditService auditService) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var bans = await banService.GetBansAsync();
            ViewBag.ActiveBanCount = bans.Count(IsActive);
            ViewBag.IsAdmin = User.IsInRole("Admin");
            return View(bans);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ban(string username, string reason, int durationHours)
        {
            var name = username?.Trim();
            var user = string.IsNullOrEmpty(name)
                ? null
                : await userManager.FindByNameAsync(name) ?? await userManager.FindByEmailAsync(name);

            if (user == null)
            {
                TempData["Error"] = $"User '{username}' was not found.";
                return RedirectToAction(nameof(Index));
            }

            if (await userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = "Administrators cannot be banned.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] = "A reason is required.";
                return RedirectToAction(nameof(Index));
            }

            var bannedById = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            TimeSpan? duration = durationHours < 0 ? null : TimeSpan.FromHours(durationHours);
            await banService.BanUserAsync(user.Id, reason.Trim(), bannedById, duration);

            await auditService.LogAsync("Ban", "User", user.Id,
                $"Banned {user.UserName} until {(duration.HasValue ? DateTime.UtcNow.Add(duration.Value).ToString("g") + " UTC" : "indefinite")}: {reason.Trim()}");

            TempData["Success"] = $"Banned {user.UserName}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Revoke(Guid id)
        {
            var revokedById = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            if (await banService.RevokeAsync(id, revokedById))
                TempData["Success"] = "Ban revoked.";
            else
                TempData["Error"] = "Ban not found or already revoked.";
            return RedirectToAction(nameof(Index));
        }

        private static bool IsActive(UserBan ban) =>
            !ban.RevokedAt.HasValue && (!ban.BannedUntil.HasValue || ban.BannedUntil > DateTime.UtcNow);
    }
}
