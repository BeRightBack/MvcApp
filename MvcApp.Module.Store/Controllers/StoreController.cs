using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MvcApp.Core;
using MvcApp.Common.Filters;
using MvcApp.Core.Abstractions;
using MvcApp.Infrastructure;
using Microsoft.Extensions.Localization;
using MvcApp.Localization;

using Microsoft.Extensions.Logging;

namespace MvcApp.Module.Store.Controllers;

[ModuleEnabledFilter("Store")]
public class StoreController(
    UserManager<UserDetails> userManager,
    IRepository<Product> productRepo,
    IRepository<ProductCategory> categoryRepo,
    IRepository<CartItem> cartRepo,
    IRepository<Order> orderRepo,
    IRepository<OrderItem> orderItemRepo,
    IStringLocalizer<SharedResource> localizer,
    Microsoft.Extensions.Logging.ILogger<StoreController> logger,
    MvcApp.Common.Payments.IPaymentGatewayResolver paymentGateways) : Controller
{
    [AllowAnonymous]
    public async Task<IActionResult> Index(int page = 1, int? categoryId = null)
    {
        const int pageSize = 12;
        var productsQuery = productRepo.Query().Where(p => p.IsActive);

        if (categoryId.HasValue)
            productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);

        var totalProducts = await productsQuery.CountAsync();
        var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
        var pagedProducts = await productsQuery
            .OrderBy(p => p.SortOrder)
            .ThenByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Categories = await categoryRepo.Query().OrderBy(c => c.SortOrder).ToListAsync();
        ViewBag.SelectedCategory = categoryId;
        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;

        return View(pagedProducts);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(string slug)
    {
        var product = await productRepo.Query()
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);
        if (product == null) return NotFound();

        var db = HttpContext.RequestServices.GetRequiredService<UserDbContext>();
        product.Category = await db.ProductCategories.FindAsync(product.CategoryId);

        ViewBag.RelatedProducts = await productRepo.Query()
            .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Take(4)
            .ToListAsync();

        return View(product);
    }

    [Authorize]
    public async Task<IActionResult> Cart()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var items = await cartRepo.Query()
            .Where(c => c.UserId == user.Id)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var db = HttpContext.RequestServices.GetRequiredService<UserDbContext>();
        foreach (var item in items)
        {
            item.Product = await db.Products.FindAsync(item.ProductId);
        }

        return View(items);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var product = await productRepo.GetByIdAsync(productId);
        if (product == null || !product.IsActive) return NotFound();

        var existing = await cartRepo.Query()
            .FirstOrDefaultAsync(c => c.UserId == user.Id && c.ProductId == productId);

        if (existing != null)
        {
            existing.Quantity += quantity;
            await cartRepo.UpdateAsync(existing);
        }
        else
        {
            await cartRepo.AddAsync(new CartItem
            {
                UserId = user.Id,
                ProductId = productId,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = quantity
            });
        }

        TempData["Success"] = localizer["Item added to cart."];
        return RedirectToAction(nameof(Cart));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCartItem(int id, int quantity)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var item = await cartRepo.GetByIdAsync(id);
        if (item == null || item.UserId != user.Id) return NotFound();

        if (quantity <= 0)
            await cartRepo.DeleteAsync(item);
        else
        {
            item.Quantity = quantity;
            await cartRepo.UpdateAsync(item);
        }

        return RedirectToAction(nameof(Cart));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveFromCart(int id)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var item = await cartRepo.GetByIdAsync(id);
        if (item == null || item.UserId != user.Id) return NotFound();

        await cartRepo.DeleteAsync(item);
        return RedirectToAction(nameof(Cart));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var items = await cartRepo.Query()
            .Where(c => c.UserId == user.Id)
            .ToListAsync();

        if (items.Count == 0)
            return RedirectToAction(nameof(Cart));

        return View(items);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(
        string shippingName, string shippingAddress, string shippingCity,
        string shippingState, string shippingPostalCode, string shippingCountry,
        string paymentMethod, string? notes)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var items = await cartRepo.Query()
            .Where(c => c.UserId == user.Id)
            .ToListAsync();

        if (items.Count == 0)
            return RedirectToAction(nameof(Cart));

        var subtotal = items.Sum(i => i.UnitPrice * i.Quantity);
        var shippingCost = subtotal >= 100 ? 0 : 10;
        var tax = subtotal * 0.10m;
        var total = subtotal + shippingCost + tax;

        var order = new Order
        {
            UserId = user.Id,
            Subtotal = subtotal,
            ShippingCost = shippingCost,
            Tax = tax,
            TotalAmount = total,
            ShippingName = shippingName,
            ShippingAddress = shippingAddress,
            ShippingCity = shippingCity,
            ShippingState = shippingState,
            ShippingPostalCode = shippingPostalCode,
            ShippingCountry = shippingCountry,
            PaymentMethod = paymentMethod,
            Notes = notes,
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending
        };

        await orderRepo.AddAsync(order);

        var db = HttpContext.RequestServices.GetRequiredService<UserDbContext>();
        foreach (var item in items)
        {
            var product = await db.Products.FindAsync(item.ProductId);
            await orderItemRepo.AddAsync(new OrderItem
            {
                OrderId = order.Id,
                ProductId = item.ProductId,
                ProductName = item.ProductName ?? "",
                ProductImage = product?.ImageUrl,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity
            });

            // Stock is NOT decremented and the cart is NOT cleared here: both moved to
            // CommitOrderAsync, which runs only once the order is confirmed. Doing it at this point
            // meant an abandoned or cancelled PayPal checkout still consumed stock and wiped the
            // customer's cart (audit 3.12).
        }

        if (paymentMethod == "PayPal")
        {
            try
            {
                // The rail comes from the platform gate, so this module no longer owns a PayPal
                // integration of its own.
                var gateway = paymentGateways.Resolve(MvcApp.Common.Payments.PaymentProviderKind.PayPal);

                var returnUrl = Url.Action(nameof(PaymentSuccess), "Store", new { orderId = order.Id }, Request.Scheme);
                var cancelUrl = Url.Action(nameof(PaymentCancel), "Store", null, Request.Scheme);

                var initiation = await gateway.InitiateAsync(new MvcApp.Common.Payments.PaymentRequest(
                    Amount: total,
                    Currency: "USD",
                    Reference: order.Id.ToString(),
                    Description: $"Order {order.Id}",
                    ReturnUrl: returnUrl,
                    CancelUrl: cancelUrl));

                if (string.IsNullOrEmpty(initiation.RedirectUrl))
                {
                    throw new InvalidOperationException("The PayPal rail returned no approval URL.");
                }

                return Redirect(initiation.RedirectUrl);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not start a PayPal payment for order {OrderId}.", order.Id);
                TempData["Error"] = localizer["Unable to start PayPal payment. Please try again or choose Cash on Delivery."];
                return RedirectToAction(nameof(Checkout));
            }
        }

        await CommitOrderAsync(order);

        TempData["Success"] = localizer["Order placed successfully!"];
        return RedirectToAction(nameof(OrderConfirmation), new { id = order.Id });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> PaymentSuccess(int orderId, string token, string? PayerID)
    {
        var order = await orderRepo.GetByIdAsync(orderId);
        if (order == null || order.UserId != userManager.GetUserId(User)) return NotFound();

        try
        {
            var gateway = paymentGateways.Resolve(MvcApp.Common.Payments.PaymentProviderKind.PayPal);
            var verification = await gateway.CaptureAsync(token);

            // A capture that has not completed is still pending, not a failure — keep that distinction
            // for the customer rather than reporting a failed payment that may yet settle.
            if (verification.Status != MvcApp.Common.Payments.PaymentStatus.Completed)
            {
                ViewBag.PaymentStatus = "pending";
                return View(order);
            }

            // Only a completed capture matching THIS order's total counts. Matches() fails closed on a
            // non-positive expectation, so an empty capture can never satisfy a forgotten amount. The
            // order row is already the server-side record, so no separate binding is needed — but the
            // amount is what ties the captured token to this order (audit 3.12).
            if (!verification.Matches(order.TotalAmount, "USD"))
            {
                logger.LogWarning(
                    "Store capture does not match order {OrderId}: expected {Expected} USD, captured {Captured} {Currency}.",
                    order.Id, order.TotalAmount, verification.Amount, verification.Currency);

                TempData["Error"] = localizer["We could not match this payment to your order. If you were charged, contact support with your PayPal receipt."];
                ViewBag.PaymentStatus = "failed";
                return View(order);
            }

            ViewBag.PaymentStatus = "paid";
            order.PaymentStatus = PaymentStatus.Paid;
            order.Status = OrderStatus.Processing;
            await orderRepo.UpdateAsync(order);

            // The payment is confirmed, so now commit stock and clear the lines this order covers.
            await CommitOrderAsync(order);
        }
        catch
        {
            ViewBag.PaymentStatus = "failed";
        }

        return View(order);
    }

    /// <summary>
    /// Commits an order: decrements stock and removes the cart lines it covers. Called only once the
    /// order is confirmed — immediately for a non-PayPal method, and from PaymentSuccess after the
    /// PayPal capture has been verified. Previously this ran at placement, so abandoning the approval
    /// page still consumed stock and emptied the cart (audit 3.12).
    /// </summary>
    private async Task CommitOrderAsync(Order order)
    {
        var orderItems = await orderItemRepo.Query()
            .Where(i => i.OrderId == order.Id)
            .ToListAsync();

        var db = HttpContext.RequestServices.GetRequiredService<UserDbContext>();

        foreach (var item in orderItems)
        {
            var product = await db.Products.FindAsync(item.ProductId);
            if (product != null)
            {
                product.StockQuantity -= item.Quantity;
                await productRepo.UpdateAsync(product);
            }

            // Only the cart lines this order covers are removed, so anything the customer added since
            // checkout survives.
            var cartItems = await cartRepo.Query()
                .Where(c => c.UserId == order.UserId && c.ProductId == item.ProductId)
                .ToListAsync();

            foreach (var cartItem in cartItems)
            {
                await cartRepo.DeleteAsync(cartItem);
            }
        }
    }

    [Authorize]
    [HttpGet]
    public IActionResult PaymentCancel()
    {
        return View();
    }

    [Authorize]
    public async Task<IActionResult> OrderConfirmation(int id)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var order = await orderRepo.GetByIdAsync(id);
        if (order == null || order.UserId != user.Id) return NotFound();

        var db = HttpContext.RequestServices.GetRequiredService<UserDbContext>();
        order.Items = await db.OrderItems.Where(i => i.OrderId == order.Id).ToListAsync();

        return View(order);
    }

    [Authorize]
    public async Task<IActionResult> Orders()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var orders = await orderRepo.Query()
            .Where(o => o.UserId == user.Id)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        var db = HttpContext.RequestServices.GetRequiredService<UserDbContext>();
        foreach (var order in orders)
        {
            order.Items = await db.OrderItems.Where(i => i.OrderId == order.Id).ToListAsync();
        }

        return View(orders);
    }
}