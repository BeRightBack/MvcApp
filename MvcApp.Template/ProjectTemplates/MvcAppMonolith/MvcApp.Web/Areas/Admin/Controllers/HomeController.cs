using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Infrastructure;
using MvcApp.Localization;

namespace MvcApp.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly UserDbContext _db;
        private readonly LocalizationDbContext _locDb;
        private readonly UserManager<UserDetails> _userManager;

        public HomeController(UserDbContext db, LocalizationDbContext locDb, UserManager<UserDetails> userManager)
        {
            _db = db;
            _locDb = locDb;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var totalRoles = await _db.Roles.CountAsync();
            var totalLanguages = await _locDb.Languages.CountAsync();
            var totalTranslations = await _locDb.StringResources.CountAsync();
            var totalVisitors = await _db.VisitorLogs.CountAsync();
            var uniqueVisitors = await _db.VisitorLogs.Select(v => v.IpAddress).Distinct().CountAsync();
            var recentUsers = await _userManager.Users.OrderByDescending(u => u.Created).Take(5).ToListAsync();

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalRoles = totalRoles;
            ViewBag.TotalLanguages = totalLanguages;
            ViewBag.TotalTranslations = totalTranslations;
            ViewBag.TotalVisitors = totalVisitors;
            ViewBag.UniqueVisitors = uniqueVisitors;
            ViewBag.RecentUsers = recentUsers;

            return View();
        }
    }
}
