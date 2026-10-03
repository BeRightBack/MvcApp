using Microsoft.AspNetCore.Mvc;
using MvcApp.Common.Cart;
using MvcApp.Core.Abstractions;

namespace MvcApp.Web.Components;

/// <summary>
/// Renders one cart badge per cart-owning module that the site's navigation currently offers.
///
/// The platform does not know which modules have carts, or how to count one: it asks whatever
/// <see cref="ICartBadgeProvider"/> implementations are registered. It previously named IPTV's
/// IShoppingCartService directly, so a site composed without IPTV could not construct this component
/// — and because it renders in the shared layout, EVERY page answered 500 (verified). A module owns
/// its cart; the platform owns only the contract.
/// </summary>
public class CartBadgesViewComponent(
    INavService navService,
    IEnumerable<ICartBadgeProvider> badgeProviders) : ViewComponent
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

        // A module shows a badge only if the navigation offers it AND a registered module provides
        // one. Whether that module is part of this deployment is not this class's concern.
        foreach (var provider in badgeProviders.Where(p => selectedModules.Contains(p.Module)))
        {
            badges.Add(await provider.GetBadgeAsync(HttpContext.User, HttpContext.Session));
        }

        return View(badges);
    }
}
