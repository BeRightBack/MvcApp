using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Infrastructure;

namespace MvcApp.Web.Controllers
{
    [Authorize]
    public class EventsController : Controller
    {
        private readonly UserDbContext _db;
        private readonly UserManager<UserDetails> _userManager;
        private readonly IGamificationService _gamification;

        public EventsController(UserDbContext db, UserManager<UserDetails> userManager, IGamificationService gamification)
        {
            _db = db;
            _userManager = userManager;
            _gamification = gamification;
        }

        public async Task<IActionResult> Index(string? city, int? categoryId, DateTime? from, DateTime? to, string orderBy = "EventDate", int page = 1)
        {
            var query = _db.Events
                .Include(e => e.Category)
                .Include(e => e.Creator)
                .Include(e => e.RSVPs)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(city))
                query = query.Where(e => e.City.ToLower().Contains(city.ToLower()));

            if (categoryId.HasValue)
                query = query.Where(e => e.CategoryId == categoryId.Value);

            if (from.HasValue)
                query = query.Where(e => e.EventDate >= from.Value);

            if (to.HasValue)
                query = query.Where(e => e.EventDate <= to.Value);

            query = orderBy.ToLower() switch
            {
                "created" => query.OrderByDescending(e => e.CreatedAt),
                _ => query.OrderBy(e => e.EventDate)
            };

            var total = await query.CountAsync();
            var events = await query
                .Skip((page - 1) * 12)
                .Take(12)
                .ToListAsync();

            ViewBag.Categories = await _db.EventCategories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.City = city;
            ViewBag.CategoryId = categoryId;
            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");
            ViewBag.OrderBy = orderBy;
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / 12.0);

            return View(events);
        }

        public async Task<IActionResult> Details(int id)
        {
            var eventItem = await _db.Events
                .Include(e => e.Category)
                .Include(e => e.Creator)
                .Include(e => e.RSVPs).ThenInclude(r => r.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventItem == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            ViewBag.CurrentUserId = currentUser?.Id;
            ViewBag.UserRSVP = eventItem.RSVPs.FirstOrDefault(r => r.UserId == currentUser?.Id);

            return View(eventItem);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _db.EventCategories.OrderBy(c => c.Name).ToListAsync();
            return View(new Event { EventDate = DateTime.Today.AddDays(1) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event model, int? categoryId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            ModelState.Remove("CreatorId");
            ModelState.Remove("Creator");
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _db.EventCategories.OrderBy(c => c.Name).ToListAsync();
                return View(model);
            }

            model.CreatorId = user.Id;
            model.CategoryId = categoryId;
            model.CreatedAt = DateTime.UtcNow;

            _db.Events.Add(model);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var eventItem = await _db.Events.FindAsync(id);
            if (eventItem == null) return NotFound();
            if (eventItem.CreatorId != user.Id && !User.IsInRole("Admin"))
                return Forbid();

            ViewBag.Categories = await _db.EventCategories.OrderBy(c => c.Name).ToListAsync();
            return View(eventItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Event model, int? categoryId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var eventItem = await _db.Events.FindAsync(id);
            if (eventItem == null) return NotFound();
            if (eventItem.CreatorId != user.Id && !User.IsInRole("Admin"))
                return Forbid();

            eventItem.Title = model.Title;
            eventItem.Description = model.Description;
            eventItem.EventDate = model.EventDate;
            eventItem.City = model.City;
            eventItem.Country = model.Country;
            eventItem.ImageUrl = model.ImageUrl;
            eventItem.CategoryId = categoryId;
            eventItem.MaxAttendees = model.MaxAttendees;

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = eventItem.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var eventItem = await _db.Events.FindAsync(id);
            if (eventItem == null) return NotFound();
            if (eventItem.CreatorId != user.Id && !User.IsInRole("Admin"))
                return Forbid();

            _db.Events.Remove(eventItem);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RSVP(int eventId, RSVPStatus status)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var eventItem = await _db.Events.Include(e => e.RSVPs).FirstOrDefaultAsync(e => e.Id == eventId);
            if (eventItem == null) return NotFound();

            var existing = eventItem.RSVPs.FirstOrDefault(r => r.UserId == user.Id);
            if (existing != null)
            {
                if (existing.Status == status)
                {
                    _db.EventRSVPs.Remove(existing);
                }
                else
                {
                    existing.Status = status;
                    existing.RSVPDate = DateTime.UtcNow;
                }
            }
            else
            {
                if (eventItem.MaxAttendees > 0 && eventItem.RSVPs.Count(r => r.Status == RSVPStatus.Going) >= eventItem.MaxAttendees && status == RSVPStatus.Going)
                {
                    TempData["Error"] = "This event is full.";
                    return RedirectToAction(nameof(Details), new { id = eventId });
                }

                _db.EventRSVPs.Add(new EventRSVP
                {
                    EventId = eventId,
                    UserId = user.Id,
                    Status = status,
                    RSVPDate = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();

            if (status == RSVPStatus.Going && existing == null)
                await _gamification.AwardPointsAsync(user.Id, 15, "RSVP'd to an event", "Event", eventId);

            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        public async Task<IActionResult> MyEvents()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Navigation properties are nullable in entity classes but populated by EF Core at runtime
#pragma warning disable CS8602
            var attending = await _db.EventRSVPs
                .Where(r => r.UserId == user.Id && r.Status == RSVPStatus.Going)
                .Include(r => r.Event).ThenInclude(e => e.Category)
                .Include(r => r.Event).ThenInclude(e => e.Creator)
                .Include(r => r.Event).ThenInclude(e => e.RSVPs)
                .Select(r => r.Event)
                .ToListAsync();

            var created = await _db.Events
                .Where(e => e.CreatorId == user.Id)
                .Include(e => e.Category)
                .Include(e => e.RSVPs)
                .ToListAsync();
#pragma warning restore CS8602

            ViewBag.Attending = attending;
            ViewBag.Created = created;

            return View();
        }
    }
}
