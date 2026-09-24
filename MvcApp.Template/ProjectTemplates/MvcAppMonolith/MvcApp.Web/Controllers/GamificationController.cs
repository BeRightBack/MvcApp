using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvcApp.Core.Abstractions;

namespace MvcApp.Web.Controllers;

[Authorize]
public class GamificationController(IGamificationService gamificationService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var totalPoints = await gamificationService.GetTotalPointsAsync(userId);
        var transactions = await gamificationService.GetRecentTransactionsAsync(userId);
        var badges = await gamificationService.GetUserBadgesAsync(userId);
        var allBadges = await gamificationService.GetAllBadgesAsync();

        ViewBag.AllBadges = allBadges;
        ViewBag.EarnedBadgeIds = badges.Select(b => b.BadgeId).ToHashSet();
        ViewBag.TotalPoints = totalPoints;
        ViewBag.Level = GetLevel(totalPoints);

        return View(transactions);
    }

    public async Task<IActionResult> Badges()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var allBadges = await gamificationService.GetAllBadgesAsync();
        var userBadges = await gamificationService.GetUserBadgesAsync(userId);

        ViewBag.UserBadges = userBadges.ToDictionary(b => b.BadgeId, b => b.EarnedAt);
        ViewBag.TotalPoints = await gamificationService.GetTotalPointsAsync(userId);

        return View(allBadges);
    }

    public async Task<IActionResult> Leaderboard()
    {
        var leaderboard = await gamificationService.GetLeaderboardAsync(50);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        ViewBag.CurrentUserId = userId;
        ViewBag.TotalPoints = await gamificationService.GetTotalPointsAsync(userId);
        ViewBag.Level = GetLevel(ViewBag.TotalPoints);

        return View(leaderboard);
    }

    private static int GetLevel(int points)
    {
        if (points < 100) return 1;
        if (points < 300) return 2;
        if (points < 600) return 3;
        if (points < 1000) return 4;
        if (points < 2000) return 5;
        if (points < 5000) return 6;
        if (points < 10000) return 7;
        return 8;
    }
}
