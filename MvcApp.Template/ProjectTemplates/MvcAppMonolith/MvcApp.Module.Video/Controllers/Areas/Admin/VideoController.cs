using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MvcApp.Core;
using MvcApp.Common.Filters;
using MvcApp.Core.Abstractions;
using MvcApp.Infrastructure;

namespace MvcApp.Module.Video.Controllers.Areas.Admin;

[Area("Admin")]
[Authorize(Roles = "Admin")]
[ModuleEnabledFilter("Video")]
public class VideoController(
    UserManager<UserDetails> userManager,
    IRepository<VideoRoom> roomRepo,
    IRepository<VideoRoomMessage> messageRepo) : Controller
{
    public async Task<IActionResult> Index()
    {
        var rooms = await roomRepo.Query().OrderBy(r => r.Name).ToListAsync();
        var db = HttpContext.RequestServices.GetRequiredService<UserDbContext>();

        ViewBag.MessageCounts = await db.Set<VideoRoomMessage>()
            .GroupBy(m => m.VideoRoomId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count);

        ViewBag.LastActivity = await db.Set<VideoRoomMessage>()
            .GroupBy(m => m.VideoRoomId)
            .Select(g => new { g.Key, Last = g.Max(m => m.MessageSent) })
            .ToDictionaryAsync(g => g.Key, g => g.Last);

        var creatorIds = rooms
            .Where(r => !string.IsNullOrWhiteSpace(r.CreatedById))
            .Select(r => r.CreatedById!)
            .Distinct()
            .ToList();

        var creators = new Dictionary<string, UserDetails>();
        foreach (var creatorId in creatorIds)
        {
            var user = await userManager.FindByIdAsync(creatorId);
            if (user != null) creators[creatorId] = user;
        }
        ViewBag.Creators = creators;

        return View(rooms);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? description, int maxSpots, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(string.Empty, "Room name is required.");
            return View();
        }

        var user = await userManager.GetUserAsync(User);
        await roomRepo.AddAsync(new VideoRoom
        {
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            MaxSpots = Math.Clamp(maxSpots, 1, 10),
            IsActive = isActive,
            CreatedById = user?.Id
        });

        TempData["Success"] = $"Room '{name}' created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var room = await roomRepo.GetByIdAsync(id);
        if (room == null) return NotFound();
        return View(room);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string name, string? description, int maxSpots, bool isActive)
    {
        var room = await roomRepo.GetByIdAsync(id);
        if (room == null) return NotFound();

        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(string.Empty, "Room name is required.");
            return View(room);
        }

        room.Name = name.Trim();
        room.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        room.MaxSpots = Math.Clamp(maxSpots, 1, 10);
        room.IsActive = isActive;
        await roomRepo.UpdateAsync(room);

        TempData["Success"] = "Room updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var room = await roomRepo.GetByIdAsync(id);
        if (room == null) return NotFound();

        var messages = await messageRepo.Query().Where(m => m.VideoRoomId == id).ToListAsync();
        foreach (var message in messages)
        {
            await messageRepo.DeleteAsync(message);
        }
        await roomRepo.DeleteAsync(room);

        TempData["Success"] = "Room deleted.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Messages(int id, int page = 1)
    {
        var room = await roomRepo.GetByIdAsync(id);
        if (room == null) return NotFound();

        const int pageSize = 50;
        var total = await messageRepo.Query().CountAsync(m => m.VideoRoomId == id);
        var messages = await messageRepo.Query()
            .Where(m => m.VideoRoomId == id)
            .OrderByDescending(m => m.MessageSent)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Room = room;
        ViewBag.Page = page;
        ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        var senderIds = messages
            .Where(m => !string.IsNullOrWhiteSpace(m.SenderId))
            .Select(m => m.SenderId!)
            .Distinct()
            .ToList();

        var senders = new Dictionary<string, UserDetails>();
        foreach (var senderId in senderIds)
        {
            var user = await userManager.FindByIdAsync(senderId);
            if (user != null) senders[senderId] = user;
        }
        ViewBag.Senders = senders;

        return View(messages);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMessage(int id)
    {
        var message = await messageRepo.GetByIdAsync(id);
        if (message == null) return NotFound();

        var roomId = message.VideoRoomId;
        await messageRepo.DeleteAsync(message);

        TempData["Success"] = "Message deleted.";
        return RedirectToAction(nameof(Messages), new { id = roomId });
    }
}
