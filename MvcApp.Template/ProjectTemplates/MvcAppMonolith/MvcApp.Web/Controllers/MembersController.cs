using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Infrastructure.Helpers;
using MvcApp.Infrastructure.Extentions;
using MvcApp.Web.Models;

namespace MvcApp.Web.Controllers
{
    [Authorize]
    public class MembersController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<UserDetails> _userManager;

        public MembersController(IUnitOfWork unitOfWork, UserManager<UserDetails> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var user = await _unitOfWork.MemberRepository.GetMemberByUsernameAsync(id);
            if (user == null) return NotFound();

            var memberModel = EntityMapper.MapToMemberModel(user);
            if (memberModel == null) return NotFound();

            var likedIds = await _unitOfWork.LikesRepository.GetLikedUserIdsAsync(currentUser.Id);
            var isMutual = likedIds.Contains(user.Id) &&
                           await _unitOfWork.LikesRepository.GetLikedUserIdsAsync(user.Id)
                               .ContinueWith(t => t.Result.Contains(currentUser.Id));

            var viewModel = new MemberProfileViewModel
            {
                Member = EntityMapper.MapToLikedMemberModel(memberModel, likedIds.Contains(user.Id), isMutual),
                Photos = user.Photos
                    ?.Where(p => p.IsApproved && (
                        p.PrivacyLevel == PrivacyLevel.Public ||
                        p.PrivacyLevel == PrivacyLevel.MembersOnly ||
                        (p.PrivacyLevel == PrivacyLevel.ExplicitUnlocked && isMutual)))
                    .OrderByDescending(p => p.IsMain)
                    .ThenByDescending(p => p.Id)
                    .Select(p => EntityMapper.MapToPhotoModel(p, user.UserName))
                    .OfType<MvcApp.Core.Models.PhotoModel>()
                    .ToList() ?? []
            };

            return View(viewModel);
        }
    }
}
