using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Common.Filters;
using MvcApp.Core;
using MvcApp.Infrastructure;
using MvcApp.Module.IPTV.Data.Extensions;
using MvcApp.Module.IPTV.Models.StoreViewModels;
using MvcApp.Module.IPTV.Services;

namespace MvcApp.Module.IPTV.Controllers;

[ModuleEnabledFilter("Iptv")]
public class IptvStoreController(UserDbContext context, IShoppingCartService shoppingCartService) : Controller
{
    [HttpGet("iptv-store")]
    public async Task<IActionResult> Index(int? planId)
    {
        Guid? userId = GetUserId();
        List<ShoppingCartItem> cartItems;

        if (userId.HasValue)
        {
            cartItems = (await shoppingCartService.GetCartItemsAsync(userId.Value)).ToList();
        }
        else
        {
            cartItems = HttpContext.Session.GetObjectFromJson<List<ShoppingCartItem>>("Cart") ?? new List<ShoppingCartItem>();
        }

        var viewModel = new StoreViewModel
        {
            SubscriptionPlans = await context.SubscriptionPlans.Include(p => p.SubscriptionDetails).ToListAsync(),
            CartItemCount = cartItems.Count
        };

        if (planId.HasValue)
        {
            ViewBag.SelectedPlan = viewModel.SubscriptionPlans.FirstOrDefault(p => p.Id == planId.Value);
        }
        else
        {
            ViewBag.SelectedPlan = viewModel.SubscriptionPlans.FirstOrDefault();
        }
        ViewBag.UserId = userId;

        ViewData["Title"] = "Store - Quebec Iptv";
        ViewData["Description"] = "Browse our selection of IPTV subscription plans.";
        ViewData["Keywords"] = "IPTV, Quebec, Streaming, Live Channels, Online TV, VOD, Canada TV";
        return View(viewModel);
    }

    [HttpPost("iptv-store")]
    public async Task<IActionResult> Index(int selectedPlanId)
    {
        var viewModel = new StoreViewModel
        {
            SubscriptionPlans = await context.SubscriptionPlans.Include(p => p.SubscriptionDetails).ToListAsync()
        };
        var selectedPlan = viewModel.SubscriptionPlans.FirstOrDefault(p => p.Id == selectedPlanId);
        ViewBag.SelectedPlan = selectedPlan;
        return View(viewModel);
    }

    private Guid? GetUserId()
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                return Guid.Parse(userIdClaim.Value);
            }
        }
        return null;
    }
}
