using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Infrastructure;

namespace MvcApp.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AuditLogsController(UserDbContext db) : Controller
    {
        public async Task<IActionResult> Index(string search = "", int page = 1, int pageSize = 30)
        {
            var query = db.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(a =>
                    a.Action.Contains(search) ||
                    a.UserName!.Contains(search) ||
                    a.Entity.Contains(search) ||
                    a.Details!.Contains(search));
            }

            var total = await query.CountAsync();
            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.Search = search;

            return View(logs);
        }
    }
}
