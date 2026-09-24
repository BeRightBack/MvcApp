using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Web.Models.LikesViewModels;

namespace MvcApp.Web.Controllers
{
    [Authorize]
    public class MatchesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<UserDetails> _userManager;

        public MatchesController(IUnitOfWork unitOfWork, UserManager<UserDetails> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var matches = await _unitOfWork.LikesRepository.GetMatchesAsync(currentUser.Id);
            var blockedIds = await _unitOfWork.UserBlockRepository.GetBlockedUserIdsAsync(currentUser.Id);

            var cards = matches.Select(m => new MemberCardViewModel
            {
                Member = m,
                ModalId = $"match-modal-{m.Id}",
                ShowLikeButton = false,
                ShowMessageButton = true,
                LikeLabel = "Liked",
                CurrentUserId = currentUser.Id,
                IsBlockedByYou = blockedIds.Contains(m.Id)
            }).ToList();

            ViewData["ReturnUrl"] = "/Matches";

            return View(cards);
        }
    }
}
