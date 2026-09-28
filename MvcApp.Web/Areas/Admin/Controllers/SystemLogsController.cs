using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvcApp.Core;
using MvcApp.Core.Abstractions;

namespace MvcApp.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SystemLogsController(ISystemLogService systemLogs) : Controller
    {
        private static readonly int[] AllowedRetentionDays = { 7, 30, 90, 180, 365 };

        public async Task<IActionResult> Index(
            string level = "",
            string search = "",
            DateTime? from = null,
            DateTime? to = null,
            int page = 1,
            int pageSize = 50)
        {
            var result = await systemLogs.QueryAsync(new SystemLogQuery
            {
                Level = level,
                Search = search,
                From = from,
                To = to,
                Page = page,
                PageSize = pageSize
            });

            ViewBag.Level = level;
            ViewBag.Search = search;
            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");
            ViewBag.PageSize = result.PageSize;

            return View(result);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var entry = await systemLogs.GetAsync(id);
            if (entry == null) return NotFound();
            return View(entry);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Purge(int days)
        {
            if (!AllowedRetentionDays.Contains(days))
            {
                return BadRequest("Unsupported retention window.");
            }

            var deleted = await systemLogs.DeleteOlderThanAsync(days);
            TempData["Message"] = $"Removed {deleted} log row(s) older than {days} days.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PurgeAll()
        {
            var deleted = await systemLogs.DeleteAllAsync();
            TempData["Message"] = $"Removed all {deleted} log row(s).";
            return RedirectToAction(nameof(Index));
        }
    }
}
