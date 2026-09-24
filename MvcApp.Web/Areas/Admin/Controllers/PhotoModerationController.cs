using MvcApp.Infrastructure;
using MvcApp.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MvcApp.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PhotoModerationController(UserDbContext db) : Controller
    {
        public async Task<IActionResult> Index(string filter = "pending", int page = 1, int pageSize = 30)
        {
            IQueryable<Photo> query = db.Photos
                .Include(p => p.UserDetails)
                .AsQueryable();

            query = filter.ToLower() switch
            {
                "approved" => query.Where(p => p.IsApproved),
                "rejected" => query.Where(p => !p.IsApproved),
                _ => query.Where(p => !p.IsApproved)
            };

            var total = await query.CountAsync();
            var photos = await query
                .OrderByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.Filter = filter;
            ViewBag.TotalPending = await db.Photos.CountAsync(p => !p.IsApproved);

            return View(photos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var photo = await db.Photos.FindAsync(id);
            if (photo == null) return NotFound();

            photo.IsApproved = true;
            await db.SaveChangesAsync();

            TempData["Message"] = $"Photo #{id} approved.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var photo = await db.Photos.FindAsync(id);
            if (photo == null) return NotFound();

            photo.IsApproved = false;
            await db.SaveChangesAsync();

            TempData["Message"] = $"Photo #{id} rejected.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var photo = await db.Photos.FindAsync(id);
            if (photo == null) return NotFound();

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Photos", photo.UserDetails?.UserName ?? photo.UserDetailsId, photo.Filename);
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            db.Photos.Remove(photo);
            await db.SaveChangesAsync();

            TempData["Message"] = $"Photo #{id} deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
