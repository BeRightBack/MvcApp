using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Common.Filters;
using MvcApp.Core.Abstractions;

namespace MvcApp.Module.Chat.Controllers;

[Authorize]
[ModuleEnabledFilter("Chat")]
public class ChatController(
    UserManager<UserDetails> userManager,
    IRepository<ChatRoom> chatRoomRepo,
    IRepository<ChatRoomMessage> roomMessageRepo) : Controller
{
    public async Task<IActionResult> Rooms()
    {
        var rooms = await chatRoomRepo.Query().ToListAsync();
        return View(rooms);
    }

    public async Task<IActionResult> Room(int id)
    {
        var currentUser = await userManager.GetUserAsync(User);
        if (currentUser == null) return Challenge();

        var room = await chatRoomRepo.GetFirstOrDefaultAsync(r => r.Id == id);
        if (room == null) return NotFound();

        var messages = await roomMessageRepo.Query()
            .Where(m => m.ChatRoomId == id)
            .OrderBy(m => m.MessageSent)
            .ToListAsync();

        ViewBag.RoomId = room.Id;
        ViewBag.RoomName = room.Name;
        ViewBag.CurrentUserId = currentUser.Id;
        ViewBag.CurrentUsername = currentUser.UserName;
        return View(messages);
    }
}