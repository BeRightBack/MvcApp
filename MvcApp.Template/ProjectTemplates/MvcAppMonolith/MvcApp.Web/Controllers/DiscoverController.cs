using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Core.Pagination;
using MvcApp.Infrastructure;
using MvcApp.Infrastructure.Helpers;
using MvcApp.Web.Models.LikesViewModels;

namespace MvcApp.Web.Controllers
{
    [Authorize]
    public class DiscoverController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<UserDetails> _userManager;
        private readonly ISuperLikeService _superLikeService;
        private readonly UserDbContext _db;

        public DiscoverController(IUnitOfWork unitOfWork, UserManager<UserDetails> userManager, ISuperLikeService superLikeService, UserDbContext db)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _superLikeService = superLikeService;
            _db = db;
        }

        public async Task<IActionResult> Index(string gender, int minAge = 18, int maxAge = 85, string orderBy = "LastActive", int page = 1, bool? hasPhoto = null, bool? isOnline = null, bool? availableNow = null, string? city = null, List<int>? interestTagIds = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var userGender = currentUser.Gender;
            string suggestedGender;
            if (Request.Query.ContainsKey("gender"))
            {
                suggestedGender = string.Equals(gender, "All", StringComparison.OrdinalIgnoreCase) ? "" : (gender ?? "");
            }
            else
            {
                suggestedGender = userGender == "Male" ? "Female" : userGender == "Female" ? "Male" : "";
            }

            var parameters = new MemberParameters
            {
                CurrentUsername = currentUser.UserName ?? "",
                Gender = suggestedGender,
                MinAge = minAge,
                MaxAge = maxAge,
                OrderBy = orderBy,
                HasPhoto = hasPhoto,
                IsOnline = isOnline,
                AvailableNow = availableNow,
                City = city,
                InterestTagIds = interestTagIds,
                PageNumber = page,
                PageSize = 20
            };

            var members = await _unitOfWork.MemberRepository.GetMembersAsync(parameters);
            var likedIds = await _unitOfWork.LikesRepository.GetLikedUserIdsAsync(currentUser.Id);
            var superLikedIds = await _superLikeService.GetSuperLikedUserIdsAsync(currentUser.Id);
            var boostedIds = await _superLikeService.GetBoostedUserIdsAsync();

            var cards = members.Select(m => new MemberCardViewModel
            {
                Member = EntityMapper.MapToLikedMemberModel(m, likedIds.Contains(m.Id), false, superLikedIds.Contains(m.Id), boostedIds.Contains(m.Id), !currentUser.IsVip),
                ModalId = $"discover-modal-{m.Id}",
                ShowLikeButton = true,
                ShowMessageButton = true,
                LikeLabel = likedIds.Contains(m.Id) ? "Liked" : "Like",
                CurrentUserId = currentUser.Id
            }).ToList();

            ViewBag.Cards = cards;
            ViewBag.SelectedGender = suggestedGender;
            ViewBag.MinAge = minAge;
            ViewBag.MaxAge = maxAge;
            ViewBag.OrderBy = orderBy;
            ViewBag.HasPhoto = hasPhoto;
            ViewBag.IsOnline = isOnline;
            ViewBag.AvailableNow = availableNow;
            ViewBag.City = city;
            ViewBag.InterestTagIds = interestTagIds ?? [];
            ViewBag.CurrentPage = page;
            ViewBag.CurrentUserId = currentUser.Id;
            ViewBag.ReturnUrl = Request.Path + Request.QueryString;
            ViewBag.IsAvailable = currentUser.IsAvailable;

            var slUsed = await _superLikeService.GetSuperLikesUsedTodayAsync(currentUser.Id);
            var slLimit = await _superLikeService.GetSuperLikeLimitAsync(currentUser.Id);
            var boostActive = await _superLikeService.IsBoostActiveAsync(currentUser.Id);
            var boostsUsed = await _superLikeService.GetBoostsUsedTodayAsync(currentUser.Id);
            var boostLimit = await _superLikeService.GetBoostLimitAsync(currentUser.Id);
            ViewBag.SuperLikesUsedToday = slUsed;
            ViewBag.SuperLikeLimit = slLimit;
            ViewBag.IsBoostActive = boostActive;
            ViewBag.BoostsUsedToday = boostsUsed;
            ViewBag.BoostLimit = boostLimit;

            var allTags = _db.InterestTags!.OrderBy(t => t.Category).ThenBy(t => t.Name).ToList();
            ViewBag.AllTags = allTags;

            return View(members);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAvailable()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            currentUser.IsAvailable = !currentUser.IsAvailable;
            await _userManager.UpdateAsync(currentUser);

            TempData["Message"] = currentUser.IsAvailable
                ? "You're now visible as Available Tonight!"
                : "You're no longer showing as Available Tonight.";

            return RedirectToAction(nameof(Index));
        }
    }
}
