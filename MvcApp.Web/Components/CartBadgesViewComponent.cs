using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Module.IPTV.Data.Extensions;
using MvcApp.Module.IPTV.Services;

namespace MvcApp.Web.Components;

public class CartBadgesViewComponent(
    INavService navService,
    IShoppingCartService iptvCartService,
    IRepository<CartItem> storeCartRepo) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var navbar = await navService.GetNavItemsAsync();
        var footer = await navService.GetFooterItemsAsync();
        var selectedModules = navbar.Concat(footer)
            .Select(i => i.Module)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var badges = new List<CartBadge>();

        if (selectedModules.Contains("Store"))
        {
            badges.Add(new CartBadge
            {
                Module = "Store",
                Label = "Store Cart",
                Controller = "Store",
                Action = "Cart",
                Count = await GetStoreCountAsync()
            });
        }

        if (selectedModules.Contains("Iptv"))
        {
            badges.Add(new CartBadge
            {
                Module = "Iptv",
                Label = "IPTV Cart",
                Controller = "IptvCart",
                Action = "Index",
                Count = await GetIptvCountAsync()
            });
        }

        return View(badges);
    }

    private async Task<int> GetStoreCountAsync()
    {
        var user = HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
            return 0;

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return 0;

        var items = await storeCartRepo.Query()
            .Where(c => c.UserId == userId)
            .ToListAsync();
        return items.Sum(c => c.Quantity);
    }

    private async Task<int> GetIptvCountAsync()
    {
        var user = HttpContext.User;
        var userIdClaim = user.Identity?.IsAuthenticated == true ? user.FindFirstValue(ClaimTypes.NameIdentifier) : null;
        if (Guid.TryParse(userIdClaim, out var userId))
            return await iptvCartService.GetCartItemsCountAsync(userId);

        var sessionCart = HttpContext.Session.GetObjectFromJson<List<ShoppingCartItem>>("Cart");
        return sessionCart?.Sum(i => i.Quantity) ?? 0;
    }
}

public class CartBadge
{
    public string Module { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public int Count { get; set; }
}
