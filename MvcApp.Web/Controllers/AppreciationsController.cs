using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MvcApp.Core;
using MvcApp.Core.Abstractions;

namespace MvcApp.Web.Controllers;

[Authorize]
public class AppreciationsController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<UserDetails> _userManager;

    public AppreciationsController(IUnitOfWork unitOfWork, UserManager<UserDetails> userManager)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string userId)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null) return Challenge();

        var targetUser = await _unitOfWork.MemberRepository.GetMemberByIdAsync(userId);
        if (targetUser == null) return NotFound();

        var existing = await _unitOfWork.AppreciationRepository.GetAppreciationAsync(currentUser.Id, userId);
        var avgScore = await _unitOfWork.AppreciationRepository.GetAverageScoreAsync(userId);
        var total = await _unitOfWork.AppreciationRepository.GetTotalAppreciationsAsync(userId);

        ViewBag.TargetUserId = userId;
        ViewBag.TargetUserName = targetUser.UserName;
        ViewBag.ExistingValue = existing?.Value ?? 0;
        ViewBag.AverageScore = avgScore;
        ViewBag.TotalAppreciations = total;

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Rate(string userId, int value)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null) return Unauthorized();

        if (value < 1 || value > 5) return Json(new { success = false, message = "Value must be between 1 and 5." });

        if (userId == currentUser.Id) return Json(new { success = false, message = "You cannot rate yourself." });

        var existing = await _unitOfWork.AppreciationRepository.GetAppreciationAsync(currentUser.Id, userId);
        if (existing != null)
        {
            existing.Value = value;
            existing.CreatedAt = DateTime.UtcNow;
            await _unitOfWork.CompleteAsync();
            return Json(new { success = true, message = "Rating updated!" });
        }

        var appreciation = new ProfileAppreciation
        {
            SourceUserId = currentUser.Id,
            TargetUserId = userId,
            Value = value
        };

        var result = await _unitOfWork.AppreciationRepository.AddAppreciationAsync(appreciation);
        return Json(new { success = result, message = result ? "Rating submitted!" : "Failed to submit rating." });
    }
}
