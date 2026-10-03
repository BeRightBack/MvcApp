using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Infrastructure;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MvcApp.Localization;

namespace MvcApp.Web.Controllers;

[Authorize]
public class VipController(
    UserDbContext db,
    UserManager<UserDetails> userManager, IStringLocalizer<SharedResource> localizer,
    MvcApp.Common.Payments.PaymentIntentProtector paymentIntents,
    MvcApp.Common.Payments.IPaymentGatewayResolver paymentGateways,
    ILogger<VipController> logger) : Controller
{
    private static readonly Dictionary<string, (decimal Rate, string Symbol, string Code)> _currencies = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USD"] = (1.00m, "$", "USD"),
        ["CAD"] = (1.36m, "CA$", "CAD"),
        ["EUR"] = (0.92m, "\u20ac", "EUR"),
        ["GBP"] = (0.79m, "\u00a3", "GBP"),
    };

    private (decimal Rate, string Symbol, string Code) GetCurrency()
    {
        var code = HttpContext.Session.GetString("SelectedCurrency") ?? "USD";
        return _currencies.TryGetValue(code, out var c) ? c : _currencies["USD"];
    }

    public async Task<IActionResult> Index()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var plans = await db.SubscriptionPlans
            .Include(p => p.SubscriptionDetails)
            .ToListAsync();

        var activeSub = await db.Subscriptions
            .Where(s => s.UserId == user.Id && s.Status == "active")
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();

        var curr = GetCurrency();

        ViewBag.CurrentUser = user;
        ViewBag.ActiveSubscription = activeSub;
        ViewBag.IsVip = user.IsVip;
        ViewBag.CurrencySymbol = curr.Symbol;
        ViewBag.CurrencyCode = curr.Code;
        ViewBag.CurrencyRate = curr.Rate;

        return View(plans);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(int planId, int detailId)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var plan = await db.SubscriptionPlans
            .Include(p => p.SubscriptionDetails)
            .FirstOrDefaultAsync(p => p.Id == planId);
        if (plan == null) return NotFound();

        var detail = plan.SubscriptionDetails.FirstOrDefault(d => d.Id == detailId);
        if (detail == null) return BadRequest("Plan has no such pricing detail.");

        // Bind the checkout to the payment: the plan, detail and quoted amount travel back from the
        // provider inside a signed payload instead of raw query-string ids, so they cannot be swapped
        // for a cheaper order's token (audit 3.12).
        var binding = paymentIntents.Protect(plan.Id, detail.Id, detail.Price, "USD");
        var returnUrl = Url.Action(nameof(PaymentSuccess), "Vip", new { b = binding }, Request.Scheme)!;
        var cancelUrl = Url.Action(nameof(Index), "Vip", null, Request.Scheme)!;

        try
        {
            var gateway = paymentGateways.Resolve(MvcApp.Common.Payments.PaymentProviderKind.PayPal);

            var initiation = await gateway.InitiateAsync(new MvcApp.Common.Payments.PaymentRequest(
                Amount: detail.Price,
                Currency: "USD",
                Reference: $"{plan.Id}:{detail.Id}",
                Description: $"{plan.Name} - {detail.Description}",
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
            TempData["Error"] = $"Payment error: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    public async Task<IActionResult> PaymentSuccess(string b, string token, string? PayerID)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        // The plan, detail and quoted amount come from the signed binding issued at checkout — never
        // from the query string. Without a readable binding there is nothing to honour: previously
        // planId/detailId were plain parameters, so a buyer could pay for the cheapest detail and
        // present that order's token alongside an expensive detail id (audit 3.12).
        var intent = paymentIntents.Unprotect(b);
        if (intent is null)
        {
            logger.LogWarning("VIP payment returned without a valid binding; refusing to grant.");
            TempData["Error"] = localizer["We could not match this payment to an order. If you were charged, contact support with your PayPal receipt."];
            return RedirectToAction(nameof(Index));
        }

        var plan = await db.SubscriptionPlans
            .Include(p => p.SubscriptionDetails)
            .FirstOrDefaultAsync(p => p.Id == intent.Value.PlanId);
        if (plan == null) return NotFound();

        var detail = plan.SubscriptionDetails.FirstOrDefault(d => d.Id == intent.Value.DetailId);
        if (detail == null) return BadRequest("Plan has no such pricing detail.");

        try
        {
            var gateway = paymentGateways.Resolve(MvcApp.Common.Payments.PaymentProviderKind.PayPal);
            var verification = await gateway.CaptureAsync(token);

            // Compare what was actually captured with what was quoted, through the sanctioned gate:
            // Matches() fails closed on a non-positive or blank expectation, which the hand-written
            // comparison it replaces did not. This is what ties the captured token to the plan the
            // customer was quoted (audit 3.12).
            if (verification.Status == MvcApp.Common.Payments.PaymentStatus.Completed &&
                !verification.Matches(intent.Value.Amount, intent.Value.Currency))
            {
                logger.LogWarning(
                    "VIP capture does not match the bound order: quoted {QuotedAmount} {QuotedCurrency}, captured {CapturedAmount} {CapturedCurrency}.",
                    intent.Value.Amount, intent.Value.Currency, verification.Amount, verification.Currency);

                TempData["Error"] = localizer["The amount paid did not match this order. Please contact support."];
                return RedirectToAction(nameof(Index));
            }

            if (verification.Status == MvcApp.Common.Payments.PaymentStatus.Completed)
            {
                var now = DateTime.UtcNow;
                var months = detail.DurationInMonths > 0 ? detail.DurationInMonths : 1;

                var existingActive = await db.Subscriptions
                    .Where(s => s.UserId == user.Id && s.Status == "active" && s.EndDate > now)
                    .FirstOrDefaultAsync();

                var startDate = existingActive != null && existingActive.EndDate > now ? existingActive.EndDate.AddSeconds(1) : now;
                var endDate = startDate.AddMonths(months);

                if (existingActive != null)
                    existingActive.Status = "extended";

                var subscription = new Subscription
                {
                    UserId = user.Id,
                    SubscriptionPlanId = plan.Id,
                    StartDate = startDate,
                    EndDate = endDate,
                    Status = "active"
                };
                db.Subscriptions.Add(subscription);

                user.PremiumExpiryDate = endDate;
                await userManager.UpdateAsync(user);
                await db.SaveChangesAsync();

                TempData["Message"] = $"Welcome to VIP! Your {plan.Name} plan is active until {endDate:MMMM dd, yyyy}.";
                return RedirectToAction(nameof(MySubscription));
            }

            TempData["Error"] = localizer["Payment was not completed. Please try again."];
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Payment capture failed: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    public IActionResult PaymentCancel()
    {
        TempData["Error"] = localizer["Payment was cancelled."];
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> MySubscription()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var subscriptions = await db.Subscriptions
            .Where(s => s.UserId == user.Id)
            .Include(s => s.SubscriptionPlan)
            .OrderByDescending(s => s.StartDate)
            .ToListAsync();

        ViewBag.CurrentUser = user;
        ViewBag.IsVip = user.IsVip;

        return View(subscriptions);
    }
}
