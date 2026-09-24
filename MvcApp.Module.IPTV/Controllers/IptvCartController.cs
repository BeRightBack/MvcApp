using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using MvcApp.Common.Filters;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Infrastructure;
using MvcApp.Module.IPTV.Data.Extensions;
using MvcApp.Module.IPTV.Models.Localization;
using MvcApp.Module.IPTV.Models.StoreViewModels;
using MvcApp.Module.IPTV.Services;

namespace MvcApp.Module.IPTV.Controllers;

[ModuleEnabledFilter("Iptv")]
public class IptvCartController(
    IShoppingCartService shoppingCartService,
    PayPalService payPalService,
    PayPalMeService payPalMeService,
    InteractService interactService,
    UserDbContext context,
    IConfiguration configuration,
    SignInManager<UserDetails> signInManager,
    UserManager<UserDetails> userManager,
    IRepository<UserDetails> userRepository,
    IEmailSender emailSender,
    IStringLocalizer<SharedResource> localizer) : Controller
{
    [HttpPost("iptv-cart/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart(int subscriptionPlanId, int subscriptionDetailId)
    {
        if (subscriptionDetailId == 0)
        {
            return BadRequest("SubscriptionDetailId cannot be zero.");
        }

        var userId = GetUserId();
        if (userId.HasValue)
        {
            await shoppingCartService.AddToCartAsync(userId.Value, subscriptionPlanId, subscriptionDetailId);
        }
        else
        {
            var subscriptionPlan = await context.SubscriptionPlans.FindAsync(subscriptionPlanId);
            var subscriptionDetail = await context.SubscriptionDetails.FindAsync(subscriptionDetailId);

            if (subscriptionPlan == null || subscriptionDetail == null)
            {
                return NotFound("Subscription plan or detail not found.");
            }

            var cart = HttpContext.Session.GetObjectFromJson<List<ShoppingCartItem>>("Cart") ?? new List<ShoppingCartItem>();
            var existingItem = cart.FirstOrDefault(item => item.SubscriptionPlanId == subscriptionPlanId && item.SubscriptionDetailId == subscriptionDetailId);
            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                cart.Add(new ShoppingCartItem
                {
                    SubscriptionPlanId = subscriptionPlanId,
                    SubscriptionDetailId = subscriptionDetailId,
                    Quantity = 1,
                    SubscriptionPlan = subscriptionPlan,
                    SubscriptionDetail = subscriptionDetail
                });
            }
            HttpContext.Session.SetObjectAsJson("Cart", cart);
        }

        return RedirectToAction("Index", "IptvStore");
    }

    [HttpGet("iptv-cart")]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Shopping Cart";
        ViewData["Description"] = "Review your items before checkout.";
        ViewData["Keywords"] = "IPTV, Quebec, Streaming, Live Channels, Online TV, VOD, Canada TV";

        var userId = GetUserId();
        if (userId.HasValue)
        {
            var cartItems = await shoppingCartService.GetCartItemsAsync(userId.Value);
            return View(cartItems);
        }
        else
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<ShoppingCartItem>>("Cart") ?? new List<ShoppingCartItem>();
            return View(cart);
        }
    }

    [HttpPost("iptv-cart/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveFromCart(int subscriptionPlanId, int subscriptionDetailId)
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            await shoppingCartService.RemoveFromCartAsync(userId.Value, subscriptionPlanId, subscriptionDetailId);
        }
        else
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<ShoppingCartItem>>("Cart") ?? new List<ShoppingCartItem>();
            var itemToRemove = cart.FirstOrDefault(item => item.SubscriptionPlanId == subscriptionPlanId && item.SubscriptionDetailId == subscriptionDetailId);
            if (itemToRemove != null)
            {
                cart.Remove(itemToRemove);
                HttpContext.Session.SetObjectAsJson("Cart", cart);
            }
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("iptv-cart/checkout")]
    public async Task<IActionResult> Checkout()
    {
        ViewData["Title"] = "Checkout";
        ViewData["Description"] = "Enter your payment details to complete your purchase.";
        ViewData["Keywords"] = "IPTV, Quebec, Streaming, Live Channels, Online TV, VOD, Canada TV";

        var userId = GetUserId();
        if (userId.HasValue)
        {
            var cartItems = await shoppingCartService.GetCartItemsAsync(userId.Value);
            var model = new CheckoutViewModel
            {
                CartItems = cartItems,
                SelectedDeviceType = cartItems.FirstOrDefault()?.SubscriptionDetail?.Description,
                VerificationCode = string.Empty
            };
            return View(model);
        }
        else
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<ShoppingCartItem>>("Cart") ?? new List<ShoppingCartItem>();
            var model = new CheckoutViewModel
            {
                CartItems = cart,
                SelectedDeviceType = cart.FirstOrDefault()?.SubscriptionDetail?.Description,
                VerificationCode = string.Empty
            };
            return View(model);
        }
    }

    [HttpPost("iptv-cart/checkout/update-device-type")]
    public async Task<IActionResult> UpdateDeviceType(CheckoutViewModel model)
    {
        var userId = GetUserId();
        var cartItems = userId.HasValue
            ? (await shoppingCartService.GetCartItemsAsync(userId.Value)).ToList()
            : HttpContext.Session.GetObjectFromJson<List<ShoppingCartItem>>("Cart") ?? new List<ShoppingCartItem>();

        model.SelectedDeviceType = model.DeviceType;
        model.CartItems = cartItems;

        return View("Checkout", model);
    }

    [HttpPost("iptv-cart/checkout/process-payment")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessPayment(CheckoutViewModel model)
    {
        var userId = GetUserId();
        UserDetails? user = null;
        var cartItems = userId.HasValue
            ? await shoppingCartService.GetCartItemsAsync(userId.Value)
            : HttpContext.Session.GetObjectFromJson<List<ShoppingCartItem>>("Cart") ?? new List<ShoppingCartItem>();

        var adminEmail = configuration["AdminEmail"];
        var currency = model.SelectedCurrency ?? "USD";

        var totalAmount = cartItems.Sum(item => item.SubscriptionDetail?.Price ?? 0);

        string cartItemsDescription;
        string emailMessage;

        if (totalAmount == 0)
        {
            if (!userId.HasValue)
            {
                user = await userManager.FindByEmailAsync(model.Email!);
                if (user == null)
                {
                    if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
                    {
                        return BadRequest("Email and Password are required for unauthenticated users.");
                    }

                    user = new UserDetails
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        EmailConfirmed = true,
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        LastLoginDate = DateTime.Now
                    };

                    var result = await userManager.CreateAsync(user, model.Password);
                    if (!result.Succeeded)
                    {
                        return BadRequest("Failed to create user account.");
                    }

                    await signInManager.SignInAsync(user, isPersistent: false);
                    userId = Guid.Parse(user.Id);
                }
                else
                {
                    await signInManager.SignInAsync(user, isPersistent: false);
                    userId = Guid.Parse(user.Id);
                }
            }
            else
            {
                user = await userRepository.GetFirstOrDefaultAsync(u => u.Id == userId.ToString());
            }

            cartItemsDescription = string.Join("<br>", cartItems.Select(item =>
                $"Plan: {item.SubscriptionPlan?.Name}, Duration: {item.SubscriptionDetail?.DurationInHours} hours, Price: {item.SubscriptionDetail?.Price.ToString("F2")}, Quantity: {item.Quantity}"));

            emailMessage = $@"
                <h3>New Free Subscription</h3>
                <p>A new free subscription has been added for user {user!.UserName}.</p>
                <p><strong>Subscription Plan:</strong><br> {cartItemsDescription}</p>
                <p><strong>Contact Email:</strong> {user.Email}</p>
                <p><strong>Device Type:</strong> {model.SelectedDeviceType}</p>
                <p><strong>Mac Address:</strong> {model.MacAddress}</p>
                <p><strong>Account Type:</strong> {model.AccountType}</p>";

            await AddSubscriptionToAccount((Guid)userId!, cartItems, "pending");

            if (!string.IsNullOrEmpty(adminEmail))
            {
                await emailSender.SendEmailAsync(adminEmail, "New Free Subscription Added", emailMessage);
            }

            foreach (var item in cartItems)
            {
                await shoppingCartService.RemoveFromCartAsync(userId!.Value, item.SubscriptionPlanId, item.SubscriptionDetailId);
            }

            HttpContext.Session.Clear();

            return View("FreeSubscriptionSuccess");
        }

        if (!userId.HasValue)
        {
            user = await userManager.FindByEmailAsync(model.Email!);
            if (user == null)
            {
                if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
                {
                    return BadRequest("Email and Password are required for unauthenticated users.");
                }

                user = new UserDetails
                {
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    LastLoginDate = DateTime.Now
                };

                var result = await userManager.CreateAsync(user, model.Password);
                if (!result.Succeeded)
                {
                    return BadRequest("Failed to create user account.");
                }
            }

            await signInManager.SignInAsync(user, isPersistent: false);
            userId = Guid.Parse(user.Id);
        }
        else
        {
            user = await userRepository.GetFirstOrDefaultAsync(u => u.Id == userId.ToString());
        }

        cartItemsDescription = string.Join("<br>", cartItems.Select(item =>
            $"Plan: {item.SubscriptionPlan?.Name}, Duration: {item.SubscriptionDetail?.DurationInMonths} months, Price: {item.SubscriptionDetail?.Price.ToString("F2")}, Quantity: {item.Quantity}"));

        emailMessage = $@"
            <h3>New Subscription</h3>
            <p>A new subscription has been added for user {user!.UserName}.</p>
            <p><strong>Subscription Plan:</strong><br> {cartItemsDescription}</p>
            <p><strong>Contact Email:</strong> {user.Email}</p>
            <p><strong>Device Type:</strong> {model.SelectedDeviceType}</p>
            <p><strong>Mac Address:</strong> {model.MacAddress}</p>
            <p><strong>Payment Method:</strong> {model.PaymentMethod}</p>
            <p><strong>Account Type:</strong> {model.AccountType}</p>";

        switch (model.PaymentMethod)
        {
            case "PayPalMe":
                var payPalMeLink = await payPalMeService.GeneratePayPalMeLink(totalAmount, currency);

                if (!string.IsNullOrEmpty(adminEmail))
                {
                    await emailSender.SendEmailAsync(adminEmail, "New Subscription Added", emailMessage);
                }
                await AddSubscriptionToAccount((Guid)userId!, cartItems, "pending");
                foreach (var item in cartItems)
                {
                    await shoppingCartService.RemoveFromCartAsync(userId!.Value, item.SubscriptionPlanId, item.SubscriptionDetailId);
                }
                HttpContext.Session.Clear();
                return RedirectToAction("PayPalMePayment", new { link = payPalMeLink });

            case "PayPal":
                if (!string.IsNullOrEmpty(adminEmail))
                {
                    await emailSender.SendEmailAsync(adminEmail, "New Subscription Added", emailMessage);
                }
                var returnUrl = Url.Action("PaymentSuccess", "IptvCart", null, Request.Scheme);
                var cancelUrl = Url.Action("PaymentCancel", "IptvCart", null, Request.Scheme);

                var orderResponse = await payPalService.CreateOrderAsync(returnUrl!, cancelUrl!, cartItems, currency);

                var order = JsonDocument.Parse(orderResponse);
                var approvalUrl = order.RootElement.GetProperty("links").EnumerateArray()
                    .First(link => link.GetProperty("rel").GetString() == "approve")
                    .GetProperty("href").GetString();

                foreach (var item in cartItems)
                {
                    await shoppingCartService.RemoveFromCartAsync((Guid)userId!, item.SubscriptionPlanId, item.SubscriptionDetailId);
                }
                HttpContext.Session.Clear();

                return Redirect(approvalUrl!);

            case "Interact":
                var interactAmount = cartItems.Sum(item => item.SubscriptionDetail!.Price);
                var interactInstructions = await interactService.GenerateInteractPaymentInstructionsAsync(interactAmount);

                await AddSubscriptionToAccount((Guid)userId!, cartItems, "pending");

                if (!string.IsNullOrEmpty(adminEmail))
                {
                    await emailSender.SendEmailAsync(adminEmail, "New Subscription Added", emailMessage);
                }
                foreach (var item in cartItems)
                {
                    await shoppingCartService.RemoveFromCartAsync(userId!.Value, item.SubscriptionPlanId, item.SubscriptionDetailId);
                }
                HttpContext.Session.Clear();
                return RedirectToAction("InteractPayment", new { instructions = interactInstructions });

            default:
                return BadRequest("Unknown payment method.");
        }
    }

    [HttpGet("iptv-cart/payment-success")]
    public async Task<IActionResult> PaymentSuccess(string token, string PayerID)
    {
        var captureResponse = await payPalService.CaptureOrderAsync(token);

        var captureResult = JsonDocument.Parse(captureResponse);
        var status = captureResult.RootElement.GetProperty("status").GetString();

        if (status == "COMPLETED")
        {
            var userId = GetUserId();
            if (userId.HasValue)
            {
                var cartItems = await shoppingCartService.GetCartItemsAsync(userId.Value);

                await AddSubscriptionToAccount(userId.Value, cartItems, "pending");

                var subscriptions = await context.Subscriptions.Where(s => s.UserId == userId.ToString() && s.Status == "pending").ToListAsync();
                foreach (var subscription in subscriptions)
                {
                    subscription.Status = "processing";
                }
                await context.SaveChangesAsync();

                foreach (var item in cartItems)
                {
                    await shoppingCartService.RemoveFromCartAsync(userId.Value, item.SubscriptionPlanId, item.SubscriptionDetailId);
                }

                ViewBag.Message = localizer["Your payment was successful. Thank you for your purchase!"];
            }
        }
        else
        {
            ViewBag.Message = "Your payment is pending. Please check your PayPal account for more details.";
        }

        ViewBag.Token = token;
        ViewBag.PayerID = PayerID;
        return View();
    }

    [HttpGet("iptv-cart/payment-cancel")]
    public IActionResult PaymentCancel()
    {
        ViewBag.Message = "Your payment was cancelled. If this was a mistake, please try again.";
        return View();
    }

    [HttpGet("iptv-cart/paypal-me-payment")]
    public IActionResult PayPalMePayment(string link)
    {
        ViewBag.Link = link;
        return View();
    }

    [HttpGet("iptv-cart/interact-payment")]
    public IActionResult InteractPayment(string instructions)
    {
        ViewBag.Instructions = instructions;
        return View();
    }

    [HttpPost("iptv-cart/verify-paypal-me/{userId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyPayPalMePayment(Guid userId)
    {
        await SetPendingSubscriptionsToProcessing(userId);
        await ActivateSubscription(userId);
        return RedirectToAction(nameof(PaymentSuccess));
    }

    [HttpPost("iptv-cart/verify-interact/{userId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyInteractPayment(Guid userId)
    {
        await SetPendingSubscriptionsToProcessing(userId);
        await ActivateSubscription(userId);
        return RedirectToAction(nameof(PaymentSuccess));
    }

    private async Task SetPendingSubscriptionsToProcessing(Guid userId)
    {
        var subscriptions = await context.Subscriptions.Where(s => s.UserId == userId.ToString() && s.Status == "pending").ToListAsync();
        foreach (var subscription in subscriptions)
        {
            subscription.Status = "processing";
        }
        await context.SaveChangesAsync();
    }

    private async Task ActivateSubscription(Guid userId)
    {
        var subscriptions = await context.Subscriptions.Where(s => s.UserId == userId.ToString() && s.Status == "processing").ToListAsync();
        foreach (var subscription in subscriptions)
        {
            subscription.UserCode = GenerateUserCode();
            subscription.Password = GeneratePassword();
            subscription.Status = "active";
        }
        await context.SaveChangesAsync();
    }

    private static string GenerateUserCode()
    {
        return Guid.NewGuid().ToString();
    }

    private static string GeneratePassword()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 8);
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

    private async Task AddSubscriptionToAccount(Guid userId, IEnumerable<ShoppingCartItem> cartItems, string initialStatus)
    {
        var user = await userRepository.GetFirstOrDefaultAsync(u => u.Id == userId.ToString());

        foreach (var item in cartItems)
        {
            var existingPlan = await context.SubscriptionPlans.FindAsync(item.SubscriptionPlanId);
            if (existingPlan == null)
            {
                throw new Exception("Subscription plan not found.");
            }

            var subscription = new Subscription
            {
                UserId = userId.ToString(),
                SubscriptionPlanId = existingPlan.Id,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(item.SubscriptionDetail!.DurationInMonths),
                Status = initialStatus,
                SubscriptionPlan = existingPlan,
                User = user!
            };

            context.Subscriptions.Add(subscription);
        }

        await context.SaveChangesAsync();
    }
}
