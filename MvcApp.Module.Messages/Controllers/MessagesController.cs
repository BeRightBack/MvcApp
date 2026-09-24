using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Common.Filters;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Module.Messages.ViewModels;

namespace MvcApp.Module.Messages.Controllers;

[Authorize]
[ModuleEnabledFilter("Messages")]
public class MessagesController(
    UserManager<UserDetails> userManager,
    IMessageRepository messageRepo,
    ILikesRepository likesRepo,
    IUserBlockRepository blockRepo) : Controller
{
    public async Task<IActionResult> Index()
    {
        var currentUser = await userManager.GetUserAsync(User);
        if (currentUser == null) return Challenge();

        var matches = await likesRepo.GetMatchesAsync(currentUser.Id);
        var matchIds = matches.Select(m => m.Id).ToHashSet();

        var conversations = (await messageRepo.GetConversationSummariesAsync(currentUser.Id))
            .Where(c => matchIds.Contains(c.MemberId))
            .ToList();

        var conversationIds = conversations.Select(c => c.MemberId).ToHashSet();
        var suggestions = matches
            .Where(m => !conversationIds.Contains(m.Id))
            .OrderByDescending(m => m.LastActive)
            .Take(8)
            .ToList();

        var viewModel = new MessagesIndexViewModel
        {
            Suggestions = suggestions,
            Conversations = conversations
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Chat(string id)
    {
        var currentUser = await userManager.GetUserAsync(User);
        if (currentUser == null) return Challenge();

        var recipient = await userManager.FindByIdAsync(id);
        if (recipient == null) return NotFound();

        if (await blockRepo.IsBlockedAsync(currentUser.Id, recipient.Id))
        {
            TempData["MessagesError"] = "You cannot view this conversation.";
            return RedirectToAction(nameof(Index));
        }

        var matches = await likesRepo.GetMatchesAsync(currentUser.Id);
        if (matches.All(m => m.Id != recipient.Id))
        {
            TempData["MessagesError"] = "You can only message your matches.";
            return RedirectToAction(nameof(Index));
        }

        var messages = await messageRepo.GetMessageThreadAsync(currentUser.UserName!, recipient.UserName!);
        ViewBag.RecipientId = recipient.Id;
        ViewBag.RecipientUsername = recipient.UserName;
        ViewBag.CurrentUserId = currentUser.Id;
        ViewBag.CurrentUsername = currentUser.UserName;
        return View(messages);
    }
}
