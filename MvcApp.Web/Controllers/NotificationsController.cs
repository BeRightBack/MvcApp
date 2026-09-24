using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MvcApp.Common.Hubs;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Web.Models.NotificationsViewModels;

namespace MvcApp.Web.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<UserDetails> _userManager;
        private readonly IHubContext<NotificationHub> _notificationHub;

        public NotificationsController(
            IUnitOfWork unitOfWork,
            UserManager<UserDetails> userManager,
            IHubContext<NotificationHub> notificationHub)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _notificationHub = notificationHub;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var notifications = await _unitOfWork.NotificationRepository.GetForUserAsync(currentUser.Id);
            return View(notifications);
        }

        public async Task<IActionResult> Settings()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var settings = await _unitOfWork.NotificationSettingsRepository.GetAsync(currentUser.Id);

            var viewModel = new NotificationSettingsViewModel
            {
                EmailOnLike = settings?.EmailOnLike ?? true,
                EmailOnMatch = settings?.EmailOnMatch ?? true
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(NotificationSettingsViewModel viewModel)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            await _unitOfWork.NotificationSettingsRepository.UpdateAsync(
                currentUser.Id,
                viewModel.EmailOnLike,
                viewModel.EmailOnMatch);
            await _unitOfWork.CompleteAsync();

            TempData["NotificationSettingsSaved"] = true;
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead(string? returnUrl)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            await _unitOfWork.NotificationRepository.MarkAllReadAsync(currentUser.Id);
            await _unitOfWork.CompleteAsync();

            await _notificationHub.Clients.Group(currentUser.Id).SendAsync("UpdateNotificationCount", 0);

            return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl)
                ? Url.Action(nameof(Index))!
                : returnUrl);
        }
    }
}
