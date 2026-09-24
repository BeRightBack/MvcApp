using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MvcApp.Core.Abstractions;

namespace MvcApp.Web.Components;

public class NotificationBadgeViewComponent : ViewComponent
{
    private readonly IUnitOfWork _unitOfWork;

    public NotificationBadgeViewComponent(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var user = HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
            return View(0);

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return View(0);

        var count = await _unitOfWork.NotificationRepository.GetUnreadCountAsync(userId);
        return View(count);
    }
}
