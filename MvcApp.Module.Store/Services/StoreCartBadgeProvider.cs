using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MvcApp.Common.Cart;
using MvcApp.Core;
using MvcApp.Core.Abstractions;

namespace MvcApp.Module.Store.Services;

/// <summary>
/// The store cart badge, owned by the module that owns the cart. The platform asks for badges
/// through <see cref="ICartBadgeProvider"/> and never names this module.
/// </summary>
public class StoreCartBadgeProvider(IRepository<CartItem> cartRepo) : ICartBadgeProvider
{
    public string Module => "Store";

    public async Task<CartBadge> GetBadgeAsync(
        ClaimsPrincipal user,
        ISession session,
        CancellationToken cancellationToken = default)
    {
        var count = 0;

        if (user.Identity?.IsAuthenticated == true)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var items = await cartRepo.Query()
                    .Where(c => c.UserId == userId)
                    .ToListAsync(cancellationToken);

                count = items.Sum(c => c.Quantity);
            }
        }

        return new CartBadge("Store", "Store Cart", "Store", "Cart", count);
    }
}
