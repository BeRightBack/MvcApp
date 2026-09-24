using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Infrastructure;
using MvcApp.Web.Services;

namespace MvcApp.Web.Controllers;

[Authorize]
public class VipController(
    UserDbContext db,
    UserManager<UserDetails> userManager,
    VipPayPalService payPalService) : Controller
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
    public async Task<IActionResult> Checkout(int planId)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var plan = await db.SubscriptionPlans
            .Include(p => p.SubscriptionDetails)
            .FirstOrDefaultAsync(p => p.Id == planId);
        if (plan == null) return NotFound();

        var detail = plan.SubscriptionDetails.FirstOrDefault();
        if (detail == null) return BadRequest("Plan has no pricing detail.");

        var returnUrl = Url.Action(nameof(PaymentSuccess), "Vip", new { planId }, Request.Scheme)!;
        var cancelUrl = Url.Action(nameof(Index), "Vip", null, Request.Scheme)!;

        try
        {
            var approvalUrl = await payPalService.CreateOrderAsync(detail.Price, "USD", returnUrl, cancelUrl);
            return Redirect(approvalUrl);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Payment error: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    public async Task<IActionResult> PaymentSuccess(int planId, string token, string? PayerID)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var plan = await db.SubscriptionPlans
            .Include(p => p.SubscriptionDetails)
            .FirstOrDefaultAsync(p => p.Id == planId);
        if (plan == null) return NotFound();

        var detail = plan.SubscriptionDetails.FirstOrDefault();
        if (detail == null) return BadRequest("Plan has no pricing detail.");

        try
        {
            var status = await payPalService.CaptureOrderAsync(token);
            if (status == "COMPLETED")
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

            TempData["Error"] = "Payment was not completed. Please try again.";
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
        TempData["Error"] = "Payment was cancelled.";
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
