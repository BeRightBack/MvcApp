using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MvcApp.Common.Hubs;
using MvcApp.Core;
using MvcApp.Core.Abstractions;

namespace MvcApp.Web.Controllers
{
    [Authorize]
    public class SuperLikeController(
        ISuperLikeService superLikeService,
        IGamificationService gamification,
        IUnitOfWork unitOfWork,
        UserManager<UserDetails> userManager,
        IHubContext<NotificationHub> notificationHub) : Controller
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(string targetUserId, string? returnUrl)
        {
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (string.IsNullOrWhiteSpace(targetUserId) || targetUserId == currentUser.Id)
                return LocalRedirect(returnUrl ?? "/Discover");

            var sent = await superLikeService.SendSuperLikeAsync(currentUser.Id, targetUserId);
            if (!sent)
            {
                TempData["Error"] = "You've used all your Super Likes for today, or you've already Super Liked this person.";
                return LocalRedirect(returnUrl ?? "/Discover");
            }

            await gamification.AwardPointsAsync(currentUser.Id, 25, "Sent a Super Like", "SuperLike");
            await gamification.CheckAndAwardBadgesAsync(currentUser.Id);

            var targetUser = await unitOfWork.MemberRepository.GetMemberByIdAsync(targetUserId);
            if (targetUser != null)
            {
                var actorName = string.IsNullOrWhiteSpace(currentUser.KnownAs) ? currentUser.UserName : currentUser.KnownAs;
                var mutual = await unitOfWork.LikesRepository.GetUserLikeAsync(targetUserId, currentUser.Id) != null;

                await unitOfWork.NotificationRepository.AddAsync(new Notification
                {
                    UserId = targetUserId,
                    Type = "SuperLike",
                    ActorId = currentUser.Id,
                    ActorUsername = currentUser.UserName ?? string.Empty,
                    Message = $"{actorName} sent you a Super Like!",
                    Url = "/Likes?predicate=likedby",
                    CreatedAt = DateTime.UtcNow
                });
                await unitOfWork.CompleteAsync();

                var count = await unitOfWork.NotificationRepository.GetUnreadCountAsync(targetUserId);
                await notificationHub.Clients.Group(targetUserId).SendAsync("UpdateNotificationCount", count);

                if (mutual)
                    await gamification.AwardPointsAsync(currentUser.Id, 30, "Mutual Super Like match!", "Match");
            }

            TempData["Message"] = "Super Like sent!";
            return LocalRedirect(returnUrl ?? "/Discover");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Boost()
        {
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var activated = await superLikeService.ActivateBoostAsync(currentUser.Id);
            if (!activated)
            {
                TempData["Error"] = "You've used all your Boosts for today, or a Boost is already active.";
                return RedirectToAction("Index", "Discover");
            }

            await gamification.AwardPointsAsync(currentUser.Id, 15, "Activated a Boost", "Boost");
            await gamification.CheckAndAwardBadgesAsync(currentUser.Id);
            TempData["Message"] = "Your profile is now boosted for 30 minutes!";
            return RedirectToAction("Index", "Discover");
        }

        [HttpGet]
        public async Task<IActionResult> Status()
        {
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var used = await superLikeService.GetSuperLikesUsedTodayAsync(currentUser.Id);
            var limit = await superLikeService.GetSuperLikeLimitAsync(currentUser.Id);
            var boostActive = await superLikeService.IsBoostActiveAsync(currentUser.Id);
            var boostsUsed = await superLikeService.GetBoostsUsedTodayAsync(currentUser.Id);
            var boostLimit = await superLikeService.GetBoostLimitAsync(currentUser.Id);

            return Json(new
            {
                superLikes = new { used, remaining = limit - used, limit },
                boost = new { active = boostActive, used = boostsUsed, remaining = boostLimit - boostsUsed, limit = boostLimit }
            });
        }
    }
}
