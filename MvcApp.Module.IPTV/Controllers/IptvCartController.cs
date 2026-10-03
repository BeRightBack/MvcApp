using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
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
    IRepository<UserDetails> userRepository,
    IEmailSender emailSender,
    MvcApp.Common.Payments.ActivationPolicy activationPolicy,
    Microsoft.Extensions.Logging.ILogger<IptvCartController> logger,
    IStringLocalizer<SharedResource> localizer) : Controller
{
    [HttpPost("iptv-cart/add")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
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
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
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
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
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
        SetCheckoutViewData();
        return View(await BuildCheckoutModelAsync());
    }

    private void SetCheckoutViewData()
    {
        ViewData["Title"] = "Checkout";
        ViewData["Description"] = "Enter your payment details to complete your purchase.";
        ViewData["Keywords"] = "IPTV, Quebec, Streaming, Live Channels, Online TV, VOD, Canada TV";
    }

    private async Task<CheckoutViewModel> BuildCheckoutModelAsync()
    {
        var userId = GetUserId();

        var cartItems = userId.HasValue
            ? await shoppingCartService.GetCartItemsAsync(userId.Value)
            : HttpContext.Session.GetObjectFromJson<List<ShoppingCartItem>>("Cart") ?? new List<ShoppingCartItem>();

        return new CheckoutViewModel
        {
            CartItems = cartItems,
            SelectedDeviceType = cartItems.FirstOrDefault()?.SubscriptionDetail?.Description,
            VerificationCode = string.Empty
        };
    }

    /// <summary>
    /// Validates the CAPTCHA posted with the checkout form and consumes it (single use). This form
    /// carries an email address and, for a new account, a password, so the challenge is mandatory —
    /// the posted code used to be collected by the view and never checked by the controller.
    /// </summary>
    private bool IsVerificationCodeValid(string? provided)
    {
        var expected = HttpContext.Session.GetString("CaptchaCode");
        HttpContext.Session.Remove("CaptchaCode");

        return !string.IsNullOrEmpty(expected) &&
               string.Equals(expected, provided, StringComparison.Ordinal);
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
        // Mandatory CAPTCHA. The view collected this code, but the controller never checked it — so
        // the one form that carries an email address (and a password, for new accounts) had no
        // challenge at all. On failure the typed details are preserved so the visitor just
        // re-enters the code; only the code is cleared.
        if (!IsVerificationCodeValid(model.VerificationCode))
        {
            ModelState.AddModelError(nameof(model.VerificationCode),
                localizer["The verification code is incorrect or has expired. Please request a new code."]);

            model.CartItems = (await BuildCheckoutModelAsync()).CartItems;
            model.VerificationCode = string.Empty;
            SetCheckoutViewData();

            return View("Checkout", model);
        }

        var userId = GetUserId();

        if (!userId.HasValue)
        {
            // Identity must come from authentication, NEVER from the posted email. Deriving it from
            // the form previously signed the caller in as any existing account with no password
            // check at all (audit 2.8 — anonymous account takeover). Ask properly, then come back;
            // the cart lives in the session, so nothing is lost across the round trip.
            return RedirectToPage("/Account/Login", new { returnUrl = Url.Action(nameof(Checkout), "IptvCart") });
        }

        UserDetails? user = null;

        // Prefer the persisted cart, but fall back to the session cart so a basket started
        // anonymously is not silently emptied by the sign-in round trip.
        var sessionCart = HttpContext.Session.GetObjectFromJson<List<ShoppingCartItem>>("Cart") ?? new List<ShoppingCartItem>();
        var persistedCart = (await shoppingCartService.GetCartItemsAsync(userId.Value)).ToList();
        var cartItems = persistedCart.Count > 0 ? persistedCart : sessionCart;

        var adminEmail = configuration["AdminEmail"];
        var currency = model.SelectedCurrency ?? "USD";

        var totalAmount = cartItems.Sum(item => item.SubscriptionDetail?.Price ?? 0);

        string cartItemsDescription;
        string emailMessage;

        if (totalAmount == 0)
        {
            user = await userRepository.GetFirstOrDefaultAsync(u => u.Id == userId.ToString());

            if (user is null)
            {
                return RedirectToPage("/Account/Login", new { returnUrl = Url.Action(nameof(Checkout), "IptvCart") });
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

        user = await userRepository.GetFirstOrDefaultAsync(u => u.Id == userId.ToString());

        if (user is null)
        {
            return RedirectToPage("/Account/Login", new { returnUrl = Url.Action(nameof(Checkout), "IptvCart") });
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
        var capture = await payPalService.CaptureOrderAsync(token);

        if (capture.Status == "COMPLETED")
        {
            var userId = GetUserId();
            if (userId.HasValue)
            {
                var cartItems = await shoppingCartService.GetCartItemsAsync(userId.Value);

                // Refuse to grant anything unless we can say what was charged. An unreadable currency
                // would otherwise reach the conversion below and throw mid-grant.
                if (string.IsNullOrEmpty(capture.Currency))
                {
                    logger.LogWarning("IPTV capture returned no currency; refusing to grant.");
                    ViewBag.Message = localizer["We could not confirm your payment. Please contact support."];
                    ViewBag.Token = token;
                    ViewBag.PayerID = PayerID;
                    return View();
                }

                // Verify what was captured against what the cart costs, in the currency that was
                // actually charged. Reading only the status meant any completed payment satisfied
                // this — the same defect fixed for Store and VIP (audit 3.12). The expected value is
                // recomputed through the same conversion CreateOrderAsync used, so the check needs
                // no knowledge of which currency the customer had selected.
                var expected = await payPalService.ExpectedTotalAsync(cartItems, capture.Currency);

                if (capture.Amount != expected)
                {
                    logger.LogWarning(
                        "IPTV capture does not match the cart: expected {Expected} {Currency}, captured {Captured}.",
                        expected, capture.Currency, capture.Amount);

                    ViewBag.Message = localizer["The amount paid did not match your order. Please contact support."];
                    ViewBag.Token = token;
                    ViewBag.PayerID = PayerID;
                    return View();
                }

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

    // The route still carries userId so the existing forms keep working, but it is deliberately
    // IGNORED: trustworthy identity comes from the authenticated principal. Taking it from the URL
    // let any caller activate an arbitrary user's pending subscriptions (audit 2.4).
    [Authorize]
    [HttpPost("iptv-cart/verify-paypal-me/{userId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyPayPalMePayment(Guid userId)
    {
        var currentUserId = GetUserId();
        if (!currentUserId.HasValue)
        {
            return Challenge();
        }

        await HandleOfflinePaymentClaimAsync(currentUserId.Value, "PayPal.Me");
        return RedirectToAction(nameof(PaymentSuccess));
    }

    [Authorize]
    [HttpPost("iptv-cart/verify-interact/{userId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyInteractPayment(Guid userId)
    {
        var currentUserId = GetUserId();
        if (!currentUserId.HasValue)
        {
            return Challenge();
        }

        await HandleOfflinePaymentClaimAsync(currentUserId.Value, "Interac e-Transfer");
        return RedirectToAction(nameof(PaymentSuccess));
    }

    /// <summary>
    /// A customer reports that they have paid over a rail nothing can verify automatically (Interac
    /// e-Transfer, PayPal.Me). Whether that claim activates the subscription is a PER-TEMPLATE
    /// setting, not a fixed behaviour.
    ///
    /// Previously both endpoints unconditionally flipped the caller's own pending subscriptions to
    /// processing and then to active, generating working credentials — so any signed-in user could
    /// POST to them and grant themselves a subscription for free, with no payment involved
    /// (audit 3.12). Under the default mode a claim now only notifies an administrator, and the
    /// subscription stays pending until a human checks the deposit.
    /// </summary>
    private async Task HandleOfflinePaymentClaimAsync(Guid userId, string rail)
    {
        var total = (await shoppingCartService.GetCartItemsAsync(userId))
            .Sum(item => item.SubscriptionDetail?.Price ?? 0m);

        if (activationPolicy.ShouldActivateAutomatically(total))
        {
            logger.LogInformation(
                "Activating subscription(s) automatically for {UserId}: {Amount} claimed via {Rail} (activation mode {Mode}).",
                userId, total, rail, activationPolicy.Mode);

            await SetPendingSubscriptionsToProcessing(userId);
            await ActivateSubscription(userId);
            return;
        }

        // No verified payment exists, so nothing activates. Ask the administrator to check the
        // deposit — this is the email step the offline model depends on.
        logger.LogInformation(
            "Payment claim from {UserId} for {Amount} via {Rail} held for verification (activation mode {Mode}).",
            userId, total, rail, activationPolicy.Mode);

        var adminEmail = configuration["AdminEmail"];
        if (!string.IsNullOrEmpty(adminEmail))
        {
            await emailSender.SendEmailAsync(
                adminEmail,
                "IPTV subscription awaiting payment verification",
                $"User {userId} reports paying {total:F2} via {rail}. "
                + "Check the deposit against the reference before activating this subscription.");
        }
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
        return SubscriptionCredentials.GenerateUserCode();
    }

    private static string GeneratePassword()
    {
        return SubscriptionCredentials.GeneratePassword();
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
