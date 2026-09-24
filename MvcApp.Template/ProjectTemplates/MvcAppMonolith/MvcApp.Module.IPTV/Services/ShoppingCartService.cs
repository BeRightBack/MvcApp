using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Infrastructure;

namespace MvcApp.Module.IPTV.Services;

public class ShoppingCartService(UserDbContext context) : IShoppingCartService
{
    public async Task AddToCartAsync(Guid userId, int subscriptionPlanId, int subscriptionDetailId)
    {
        if (subscriptionDetailId == 0)
        {
            throw new ArgumentException("SubscriptionDetailId cannot be zero.", nameof(subscriptionDetailId));
        }

        var cartItem = await context.ShoppingCartItems
            .FirstOrDefaultAsync(c => c.UserId == userId && c.SubscriptionPlanId == subscriptionPlanId && c.SubscriptionDetailId == subscriptionDetailId);

        if (cartItem == null)
        {
            cartItem = new ShoppingCartItem
            {
                UserId = userId,
                SubscriptionPlanId = subscriptionPlanId,
                SubscriptionDetailId = subscriptionDetailId,
                Quantity = 1
            };
            context.ShoppingCartItems.Add(cartItem);
        }
        else
        {
            cartItem.Quantity++;
        }

        await context.SaveChangesAsync();
    }

    public async Task RemoveFromCartAsync(Guid userId, int subscriptionPlanId, int subscriptionDetailId)
    {
        var cartItem = await context.ShoppingCartItems
            .FirstOrDefaultAsync(c => c.UserId == userId && c.SubscriptionPlanId == subscriptionPlanId && c.SubscriptionDetailId == subscriptionDetailId);

        if (cartItem != null)
        {
            context.ShoppingCartItems.Remove(cartItem);
            await context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<ShoppingCartItem>> GetCartItemsAsync(Guid userId)
    {
        return await context.ShoppingCartItems
            .Where(c => c.UserId == userId)
            .Include(c => c.SubscriptionPlan)
            .Include(c => c.SubscriptionDetail)
            .ToListAsync();
    }

    public async Task<int> GetCartItemsCountAsync(Guid userId)
    {
        return await context.ShoppingCartItems
            .Where(c => c.UserId == userId)
            .SumAsync(c => c.Quantity);
    }
}
