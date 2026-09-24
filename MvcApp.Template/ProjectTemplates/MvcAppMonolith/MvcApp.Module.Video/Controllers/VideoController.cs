using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Common.Filters;
using MvcApp.Core;
using MvcApp.Core.Abstractions;

namespace MvcApp.Module.Video.Controllers;

[Authorize]
[ModuleEnabledFilter("Video")]
public class VideoController(
    UserManager<UserDetails> userManager,
    IRepository<VideoRoom> roomRepo,
    IRepository<VideoRoomMessage> messageRepo,
    IUnitOfWork unitOfWork) : Controller
{
    public async Task<IActionResult> Index()
    {
        var rooms = await roomRepo.Query()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Id)
            .ToListAsync();

        var currentUser = await userManager.GetUserAsync(User);
        ViewBag.CurrentUserId = currentUser?.Id;
        return View(rooms);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRoom(string name, string description, int maxSpots)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var trimmed = name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            TempData["VideoFlash"] = "A room name is required.";
            return RedirectToAction(nameof(Index));
        }

        var room = new VideoRoom
        {
            Name = trimmed,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            MaxSpots = Math.Clamp(maxSpots, 1, 10),
            CreatedById = user.Id
        };

        await roomRepo.AddAsync(room);
        await unitOfWork.CompleteAsync();

        TempData["VideoFlash"] = $"Video room \"{room.Name}\" created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRoom(int id)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var room = await roomRepo.GetByIdAsync(id);
        if (room == null) return NotFound();

        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && room.CreatedById != user.Id)
            return Forbid();

        await roomRepo.DeleteAsync(id);
        await unitOfWork.CompleteAsync();

        TempData["VideoFlash"] = $"Video room \"{room.Name}\" deleted.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Room(int id)
    {
        var currentUser = await userManager.GetUserAsync(User);
        if (currentUser == null) return Challenge();

        var room = await roomRepo.GetFirstOrDefaultAsync(r => r.Id == id && r.IsActive);
        if (room == null) return NotFound();

        var messages = await messageRepo.Query()
            .Where(m => m.VideoRoomId == id)
            .OrderBy(m => m.MessageSent)
            .ToListAsync();

        ViewBag.RoomId = room.Id;
        ViewBag.RoomName = room.Name;
        ViewBag.MaxSpots = room.MaxSpots;
        ViewBag.CurrentUserId = currentUser.Id;
        ViewBag.CurrentUsername = currentUser.UserName;
        return View(messages);
    }
}
