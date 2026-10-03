using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace MvcApp.Common.Cart;

/// <summary>One cart badge in the site chrome.</summary>
public sealed record CartBadge(string Module, string Label, string Controller, string Action, int Count);

/// <summary>
/// Supplies the cart badge for one module. The PLATFORM defines this contract; a module that owns a
/// cart implements it, and the shared layout renders whatever happens to be registered.
///
/// This exists because the platform used to reach into IPTV's <c>IShoppingCartService</c> directly.
/// A deployment composed without IPTV could not construct the chrome component at all — and because
/// that component renders in the shared layout, EVERY page answered 500 (verified). The rule this
/// encodes: modules depend on the platform; the platform never depends on a module.
///
/// It also keeps the module's own knowledge where it belongs — the route to its cart, its wording,
/// and how its cart is stored (a table for one, a session for the other).
/// </summary>
public interface ICartBadgeProvider
{
    /// <summary>Module name — the same name used by navigation and by template composition.</summary>
    string Module { get; }

    Task<CartBadge> GetBadgeAsync(ClaimsPrincipal user, ISession session, CancellationToken cancellationToken = default);
}
