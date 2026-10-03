using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MvcApp.Common.Cart;
using MvcApp.Core;
using MvcApp.Module.IPTV.Data.Extensions;

namespace MvcApp.Module.IPTV.Services;

/// <summary>
/// The IPTV cart badge, owned by the module that owns the cart.
///
/// It knows two things the platform must not: how an IPTV cart is stored (a table for signed-in
/// customers, a session entry for anonymous browsers while they are still deciding) and where the
/// cart lives (IptvCart/Index).
/// </summary>
public class IptvCartBadgeProvider(IShoppingCartService cartService) : ICartBadgeProvider
{
    public string Module => "Iptv";

    public async Task<CartBadge> GetBadgeAsync(
        ClaimsPrincipal user,
        ISession session,
        CancellationToken cancellationToken = default)
        => new("Iptv", "IPTV Cart", "IptvCart", "Index", await CountAsync(user, session));

    private async Task<int> CountAsync(ClaimsPrincipal user, ISession session)
    {
        var userIdClaim = user.Identity?.IsAuthenticated == true
            ? user.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;

        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return await cartService.GetCartItemsCountAsync(userId);
        }

        // Anonymous visitors keep their cart in the session.
        var sessionCart = session.GetObjectFromJson<List<ShoppingCartItem>>("Cart");
        return sessionCart?.Sum(i => i.Quantity) ?? 0;
    }
}
