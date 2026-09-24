using MvcApp.Core;

namespace MvcApp.Module.IPTV.Services;

public interface IShoppingCartService
{
    Task AddToCartAsync(Guid userId, int subscriptionPlanId, int subscriptionDetailId);
    Task RemoveFromCartAsync(Guid userId, int subscriptionPlanId, int subscriptionDetailId);
    Task<IEnumerable<ShoppingCartItem>> GetCartItemsAsync(Guid userId);
    Task<int> GetCartItemsCountAsync(Guid userId);
}
