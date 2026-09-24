using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MvcApp.Common.Filters;
using MvcApp.Common.Models.PagedList;
using MvcApp.Infrastructure;
using MvcApp.Module.IPTV.Models.AdminViewModels;
using MvcApp.Module.IPTV.Models.Localization;
using MvcApp.Module.IPTV.Services;

namespace MvcApp.Module.IPTV.Controllers;

[ModuleEnabledFilter("Iptv")]
[Authorize(Roles = "Admin")]
public class IptvAdminController(UserDbContext context, SubscriptionStatusUpdater subscriptionStatusUpdater, IStringLocalizer<SharedResource> localizer) : Controller
{
    [HttpPost("iptv-admin/update-subscription-statuses")]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateSubscriptionStatuses()
    {
        subscriptionStatusUpdater.UpdateSubscriptionStatuses();
        TempData["Message"] = "Subscription statuses updated successfully.";
        return RedirectToAction(nameof(Index), "IptvSubscription");
    }

    [HttpGet("iptv-admin")]
    public async Task<IActionResult> Index(string visitsSearchString, string locationsSearchString, int? visitsPageNumber, int? locationsPageNumber)
    {
        var visitorLogs = context.VisitorLogs.AsQueryable();

        var totalVisits = await visitorLogs.CountAsync();
        var uniqueVisitors = await visitorLogs.Select(v => v.IpAddress).Distinct().CountAsync();

        var visitsQuery = visitorLogs
            .GroupBy(v => v.VisitTime.Date)
            .Select(g => new VisitPerDay { Date = g.Key, Count = g.Count() })
            .OrderByDescending(v => v.Date)
            .AsQueryable();

        if (!string.IsNullOrEmpty(visitsSearchString))
        {
            visitsQuery = visitsQuery.Where(v => v.Date.ToString().Contains(visitsSearchString));
        }

        var visitsPerDay = await PaginatedList<VisitPerDay>.CreateAsync(visitsQuery, visitsPageNumber ?? 1, 10);

        var locationsQuery = visitorLogs
            .GroupBy(v => new { v.VisitTime.Date, v.Country, v.City, v.Region })
            .Select(g => new VisitorLocation
            {
                Date = g.Key.Date,
                Country = g.Key.Country,
                City = g.Key.City,
                Region = g.Key.Region,
                Count = g.Count()
            })
            .OrderByDescending(v => v.Date)
            .AsQueryable();

        if (!string.IsNullOrEmpty(locationsSearchString))
        {
            locationsQuery = locationsQuery.Where(v => v.Date.ToString().Contains(locationsSearchString));
        }

        var visitorLocations = await PaginatedList<VisitorLocation>.CreateAsync(locationsQuery, locationsPageNumber ?? 1, 10);

        var model = new VisitorStatisticsViewModel
        {
            TotalVisits = totalVisits,
            UniqueVisitors = uniqueVisitors,
            VisitsPerDay = visitsPerDay,
            VisitorLocations = visitorLocations,
            VisitsSearchString = visitsSearchString,
            LocationsSearchString = locationsSearchString
        };

        ViewData["Title"] = localizer["Admin Dashboard"];
        return View(model);
    }

    [HttpPost("iptv-admin/delete-visits")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteVisits(DateTime date)
    {
        var visitsToDelete = context.VisitorLogs.Where(v => v.VisitTime.Date == date);
        context.VisitorLogs.RemoveRange(visitsToDelete);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("iptv-admin/delete-locations")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLocations(DateTime date)
    {
        var locationsToDelete = context.VisitorLogs.Where(v => v.VisitTime.Date == date);
        context.VisitorLogs.RemoveRange(locationsToDelete);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
