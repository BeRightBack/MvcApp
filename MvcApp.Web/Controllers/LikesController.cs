using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MvcApp.Common.Hubs;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Core.Pagination;
using MvcApp.Infrastructure.Helpers;
using MvcApp.Web.Models.LikesViewModels;

namespace MvcApp.Web.Controllers
{
    [Authorize]
    public class LikesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<UserDetails> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IHubContext<NotificationHub> _notificationHub;
        private readonly IGamificationService _gamification;
        private readonly ILogger<LikesController> _logger;

        public LikesController(
            IUnitOfWork unitOfWork,
            UserManager<UserDetails> userManager,
            IEmailSender emailSender,
            IHubContext<NotificationHub> notificationHub,
            IGamificationService gamification,
            ILogger<LikesController> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _emailSender = emailSender;
            _notificationHub = notificationHub;
            _gamification = gamification;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string predicate = "liked", string gender = "All", string orderBy = "LastActive", int page = 1)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (!predicate.Equals("liked", StringComparison.OrdinalIgnoreCase) &&
                !predicate.Equals("likedby", StringComparison.OrdinalIgnoreCase))
            {
                predicate = "liked";
            }

            var parameters = new LikesParameters
            {
                UserId = currentUser.Id,
                Predicate = predicate,
                Gender = gender,
                OrderBy = orderBy,
                PageNumber = page,
                PageSize = 20
            };

            var members = await _unitOfWork.LikesRepository.GetUserLikesAsync(parameters);
            var counts = await _unitOfWork.LikesRepository.GetLikeCountsAsync(currentUser.Id);
            var blockedIds = await _unitOfWork.UserBlockRepository.GetBlockedUserIdsAsync(currentUser.Id);

            var viewModel = new LikesIndexViewModel
            {
                Members = members,
                Cards = members.Select(m => new MemberCardViewModel
                {
                    Member = EntityMapper.MapToLikedMemberModel(m, m.IsLikedByYou, m.IsMutual, false, false, predicate.Equals("liked", StringComparison.OrdinalIgnoreCase) && !currentUser.IsVip),
                    ModalId = $"like-modal-{m.Id}",
                    ShowLikeButton = true,
                    ShowMessageButton = m.IsMutual || predicate.Equals("liked", StringComparison.OrdinalIgnoreCase),
                    LikeLabel = m.IsLikedByYou ? "Liked" : (predicate.Equals("likedby", StringComparison.OrdinalIgnoreCase) ? "Like Back" : "Like"),
                    CurrentUserId = currentUser.Id,
                    IsBlockedByYou = blockedIds.Contains(m.Id)
                }).ToList(),
                Predicate = predicate,
                Gender = string.IsNullOrWhiteSpace(gender) ? "All" : gender,
                OrderBy = string.IsNullOrWhiteSpace(orderBy) ? "LastActive" : orderBy,
                LikedCount = counts.Liked,
                LikedByCount = counts.LikedBy,
                CurrentUserId = currentUser.Id,
                ReturnUrl = Request.Path + Request.QueryString
            };

            ViewData["ReturnUrl"] = viewModel.ReturnUrl;

            return View(viewModel);
        }

        public async Task<IActionResult> Blocked()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var blocked = await _unitOfWork.UserBlockRepository.GetBlockedUsersAsync(currentUser.Id);

            var cards = blocked.Select(m => new MemberCardViewModel
            {
                Member = m,
                ModalId = $"blocked-modal-{m.Id}",
                ShowLikeButton = false,
                ShowMessageButton = false,
                LikeLabel = "Blocked",
                CurrentUserId = currentUser.Id,
                IsBlockedByYou = true
            }).ToList();

            ViewData["ReturnUrl"] = "/Likes/Blocked";

            return View(cards);
        }

        [HttpPost]
        public async Task<IActionResult> Like([FromBody] LikeRequest request)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.LikedUserId) || request.LikedUserId == currentUser.Id)
                return Json(new { success = false, message = "You cannot like yourself." });

            var (success, message) = await ToggleLikeAsync(currentUser, request.LikedUserId);
            return Json(new { success, message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(string likedUserId, string? returnUrl)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (!string.IsNullOrWhiteSpace(likedUserId) && likedUserId != currentUser.Id)
            {
                await ToggleLikeAsync(currentUser, likedUserId);
            }

            return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl)
                ? Url.Action(nameof(Index))!
                : returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Block(string blockedUserId, string? returnUrl)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (!string.IsNullOrWhiteSpace(blockedUserId) && blockedUserId != currentUser.Id)
            {
                await _unitOfWork.UserBlockRepository.AddAsync(new UserBlock
                {
                    SourceUserId = currentUser.Id,
                    BlockedUserId = blockedUserId
                });
                await _unitOfWork.CompleteAsync();
            }

            return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl)
                ? Url.Action(nameof(Index))!
                : returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unblock(string blockedUserId, string? returnUrl)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (!string.IsNullOrWhiteSpace(blockedUserId) && blockedUserId != currentUser.Id)
            {
                await _unitOfWork.UserBlockRepository.RemoveAsync(currentUser.Id, blockedUserId);
                await _unitOfWork.CompleteAsync();
            }

            return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl)
                ? Url.Action(nameof(Index))!
                : returnUrl);
        }

        private async Task<(bool Success, string Message)> ToggleLikeAsync(UserDetails currentUser, string targetUserId)
        {
            var existingLike = await _unitOfWork.LikesRepository.GetUserLikeAsync(currentUser.Id, targetUserId);
            if (existingLike != null)
            {
                var userWithLikes = await _unitOfWork.LikesRepository.GetUserWithLikesAsync(currentUser.Id);
                if (userWithLikes != null)
                {
                    var likeToRemove = userWithLikes.LikedUsers.FirstOrDefault(l => l.LikedUserId == targetUserId);
                    if (likeToRemove != null)
                    {
                        userWithLikes.LikedUsers.Remove(likeToRemove);
                        await _unitOfWork.CompleteAsync();
                        return (true, "Like removed.");
                    }
                }
                return (true, "Like removed.");
            }

            if (await _unitOfWork.UserBlockRepository.IsBlockedAsync(currentUser.Id, targetUserId))
                return (false, "You cannot like this member.");

            var likedUser = await _unitOfWork.MemberRepository.GetMemberByIdAsync(targetUserId);
            if (likedUser == null)
                return (false, "User not found.");

            var mutual = await _unitOfWork.LikesRepository.GetUserLikeAsync(targetUserId, currentUser.Id) != null;

            var user = await _unitOfWork.LikesRepository.GetUserWithLikesAsync(currentUser.Id);
            if (user == null) return (false, "User not found.");

            var newLike = new UserLike { SourceUserId = currentUser.Id, LikedUserId = targetUserId };
            user.LikedUsers.Add(newLike);
            var result = await _unitOfWork.CompleteAsync();
            if (!result)
                return (false, "Failed to like.");

            await _gamification.AwardPointsAsync(currentUser.Id, 10, "Liked a member", "Like");

            await CreateNotificationsAsync(currentUser, likedUser, mutual, $"{Request.Scheme}://{Request.Host}");

            if (mutual)
                await _gamification.AwardPointsAsync(currentUser.Id, 20, "Mutual match!", "Match");

            return (true, "Like sent!");
        }

        private async Task CreateNotificationsAsync(UserDetails actor, UserDetails target, bool mutual, string baseUrl)
        {
            var actorName = string.IsNullOrWhiteSpace(actor.KnownAs) ? actor.UserName ?? actor.Id : actor.KnownAs;
            var targetName = string.IsNullOrWhiteSpace(target.KnownAs) ? target.UserName ?? target.Id : target.KnownAs;

            await _unitOfWork.NotificationRepository.AddAsync(new Notification
            {
                UserId = target.Id,
                Type = "Like",
                ActorId = actor.Id,
                ActorUsername = actor.UserName ?? string.Empty,
                Message = $"{actorName} liked you",
                Url = "/Likes?predicate=likedby",
                CreatedAt = DateTime.UtcNow
            });

            var targetSettings = await _unitOfWork.NotificationSettingsRepository.GetOrCreateAsync(target.Id);
            await SendLikeEmailAsync(actorName, target, targetSettings, baseUrl);

            if (mutual)
            {
                await _unitOfWork.NotificationRepository.AddAsync(new Notification
                {
                    UserId = actor.Id,
                    Type = "Match",
                    ActorId = target.Id,
                    ActorUsername = target.UserName ?? string.Empty,
                    Message = $"You matched with {targetName}",
                    Url = $"/Messages/Chat/{target.Id}",
                    CreatedAt = DateTime.UtcNow
                });

                await _unitOfWork.NotificationRepository.AddAsync(new Notification
                {
                    UserId = target.Id,
                    Type = "Match",
                    ActorId = actor.Id,
                    ActorUsername = actor.UserName ?? string.Empty,
                    Message = $"You matched with {actorName}",
                    Url = $"/Messages/Chat/{actor.Id}",
                    CreatedAt = DateTime.UtcNow
                });

                var actorSettings = await _unitOfWork.NotificationSettingsRepository.GetOrCreateAsync(actor.Id);
                await SendMatchEmailAsync(targetName, actor, actorSettings, baseUrl);
                await SendMatchEmailAsync(actorName, target, targetSettings, baseUrl);
            }

            await _unitOfWork.CompleteAsync();

            await PushUnreadCountAsync(actor.Id);
            await PushUnreadCountAsync(target.Id);
        }

        private async Task SendLikeEmailAsync(string actorName, UserDetails target, UserNotificationSettings settings, string baseUrl)
        {
            if (!settings.EmailOnLike || string.IsNullOrWhiteSpace(target.Email))
                return;

            var link = $"{baseUrl}/Likes?predicate=likedby";
            try
            {
                await _emailSender.SendEmailAsync(
                    target.Email,
                    "You have a new like",
                    $"<p>Hi {(string.IsNullOrWhiteSpace(target.KnownAs) ? target.UserName : target.KnownAs)},</p>" +
                    $"<p><strong>{actorName}</strong> liked you on {baseUrl}.</p>" +
                    $"<p><a href=\"{link}\">See who likes you</a></p>");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send 'like' notification email to {Email}", target.Email);
            }
        }

        private async Task SendMatchEmailAsync(string otherName, UserDetails user, UserNotificationSettings settings, string baseUrl)
        {
            if (!settings.EmailOnMatch || string.IsNullOrWhiteSpace(user.Email))
                return;

            var link = $"{baseUrl}/Messages/Chat/{user.Id}";
            try
            {
                await _emailSender.SendEmailAsync(
                    user.Email,
                    "It's a match!",
                    $"<p>Hi {(string.IsNullOrWhiteSpace(user.KnownAs) ? user.UserName : user.KnownAs)},</p>" +
                    $"<p>You and <strong>{otherName}</strong> like each other. It's a match!</p>" +
                    $"<p><a href=\"{link}\">Start chatting</a></p>");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send 'match' notification email to {Email}", user.Email);
            }
        }

        private async Task PushUnreadCountAsync(string userId)
        {
            var count = await _unitOfWork.NotificationRepository.GetUnreadCountAsync(userId);
            await _notificationHub.Clients.Group(userId).SendAsync("UpdateNotificationCount", count);
        }

        public class LikeRequest
        {
            public string LikedUserId { get; set; } = string.Empty;
        }
    }
}
