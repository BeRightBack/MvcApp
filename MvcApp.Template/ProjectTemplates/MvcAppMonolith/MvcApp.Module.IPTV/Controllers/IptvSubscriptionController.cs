using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MvcApp.Common.Filters;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Infrastructure;
using MvcApp.Module.IPTV.Models.Localization;
using MvcApp.Module.IPTV.Models.SubscriptionViewModels;

namespace MvcApp.Module.IPTV.Controllers;

[ModuleEnabledFilter("Iptv")]
[Authorize(Roles = "Admin")]
public class IptvSubscriptionController(
    IStringLocalizer<SharedResource> localizer,
    IRepository<Subscription> subscriptionRepository,
    UserManager<UserDetails> userManager,
    IRepository<SubscriptionPlan> subscriptionPlanRepository,
    IEmailSender emailSender,
    UserDbContext context) : Controller
{
    [HttpGet("iptv-subscription")]
    public async Task<IActionResult> Index(string searchQuery, int pageNumber = 1, int pageSize = 10, string? sortOrder = null)
    {
        var query = context.Subscriptions.AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(s => s.User != null && (s.User.UserName!.Contains(searchQuery) || s.Status.Contains(searchQuery)));
        }

        query = sortOrder switch
        {
            "username_desc" => query.OrderByDescending(s => s.User!.UserName),
            "plan" => query.OrderBy(s => s.SubscriptionPlan!.Name),
            "plan_desc" => query.OrderByDescending(s => s.SubscriptionPlan!.Name),
            "startdate" => query.OrderBy(s => s.StartDate),
            "startdate_desc" => query.OrderByDescending(s => s.StartDate),
            "enddate" => query.OrderBy(s => s.EndDate),
            "enddate_desc" => query.OrderByDescending(s => s.EndDate),
            "status" => query.OrderBy(s => s.Status),
            "status_desc" => query.OrderByDescending(s => s.Status),
            _ => query.OrderBy(s => s.User!.UserName),
        };

        var totalItems = await query.CountAsync();
        var subscriptions = await query
            .Include(s => s.User)
            .Include(s => s.SubscriptionPlan)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var subscriptionViewModels = subscriptions.Select(subscription => new SubscriptionViewModel
        {
            Id = subscription.Id,
            UserId = subscription.UserId,
            UserName = subscription.User?.UserName ?? "Unknown",
            SubscriptionPlanId = subscription.SubscriptionPlanId,
            SubscriptionPlanName = subscription.SubscriptionPlan?.Name ?? "Unknown",
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            Status = subscription.Status,
            UserCode = subscription.UserCode,
            Password = subscription.Password
        }).ToList();

        var viewModel = new SubscriptionIndexViewModel
        {
            Subscriptions = subscriptionViewModels,
            SearchQuery = searchQuery,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = totalItems,
            SortOrder = sortOrder
        };

        ViewData["Title"] = "Subscriptions - Admin";
        return View(viewModel);
    }

    [HttpGet("iptv-subscription/details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var subscription = await context.Subscriptions
            .Include(s => s.User)
            .Include(s => s.SubscriptionPlan)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (subscription == null)
        {
            return NotFound();
        }

        var viewModel = new SubscriptionViewModel
        {
            Id = subscription.Id,
            UserId = subscription.UserId,
            UserName = subscription.User?.UserName ?? "Unknown",
            SubscriptionPlanId = subscription.SubscriptionPlanId,
            SubscriptionPlanName = subscription.SubscriptionPlan?.Name ?? "Unknown",
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            Status = subscription.Status,
            UserCode = subscription.UserCode,
            Password = subscription.Password
        };

        return View(viewModel);
    }

    [HttpGet("iptv-subscription/create")]
    public IActionResult Create()
    {
        ViewBag.SubscriptionPlans = new SelectList(subscriptionPlanRepository.GetAll(), "Id", "Name");

        var viewModel = new SubscriptionViewModel
        {
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddMonths(1),
            Status = "active",
            UserName = string.Empty,
            SubscriptionPlanName = string.Empty
        };

        ViewData["Title"] = "Create Subscription";
        return View(viewModel);
    }

    [HttpPost("iptv-subscription/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SubscriptionViewModel viewModel)
    {
        var user = await userManager.FindByEmailAsync(viewModel.UserEmail!);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "User not found.");
            ViewBag.SubscriptionPlans = new SelectList(subscriptionPlanRepository.GetAll(), "Id", "Name");
            return View(viewModel);
        }

        var subscriptionPlan = await subscriptionPlanRepository.GetByIdAsync(viewModel.SubscriptionPlanId);
        if (subscriptionPlan == null)
        {
            ModelState.AddModelError(string.Empty, "Subscription plan not found.");
            ViewBag.SubscriptionPlans = new SelectList(subscriptionPlanRepository.GetAll(), "Id", "Name");
            return View(viewModel);
        }

        viewModel.UserId = user.Id;
        viewModel.UserName = user.UserName!;
        viewModel.SubscriptionPlanName = subscriptionPlan.Name!;

        ModelState.Clear();
        TryValidateModel(viewModel);

        if (ModelState.IsValid)
        {
            var subscription = new Subscription
            {
                UserId = user.Id,
                SubscriptionPlanId = viewModel.SubscriptionPlanId,
                StartDate = viewModel.StartDate,
                EndDate = viewModel.EndDate,
                Status = viewModel.Status,
                UserCode = viewModel.UserCode,
                Password = viewModel.Password,
                User = user,
                SubscriptionPlan = subscriptionPlan
            };

            await subscriptionRepository.AddAsync(subscription);

            var emailMessage = BuildSubscriptionEmail(user, subscription);

            await emailSender.SendEmailAsync(user.Email!, "New Subscription Assigned", emailMessage);

            return RedirectToAction(nameof(Index));
        }

        ViewBag.SubscriptionPlans = new SelectList(subscriptionPlanRepository.GetAll(), "Id", "Name");
        return View(viewModel);
    }

    [HttpGet("iptv-subscription/edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(id);
        if (subscription == null)
        {
            return NotFound();
        }

        var user = await userManager.FindByIdAsync(subscription.UserId);
        var subscriptionPlan = await subscriptionPlanRepository.GetByIdAsync(subscription.SubscriptionPlanId);

        var viewModel = new SubscriptionViewModel
        {
            Id = subscription.Id,
            UserName = user?.UserName ?? "Unknown",
            UserId = subscription.UserId,
            SubscriptionPlanId = subscription.SubscriptionPlanId,
            SubscriptionPlanName = subscriptionPlan?.Name ?? "Unknown",
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            Status = subscription.Status,
            UserCode = subscription.UserCode,
            Password = subscription.Password
        };

        ViewData["Title"] = "Edit Subscription";
        return View(viewModel);
    }

    [HttpPost("iptv-subscription/edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SubscriptionViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        var user = await userManager.FindByIdAsync(viewModel.UserId);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "User not found.");
            return View(viewModel);
        }

        var subscriptionPlan = await subscriptionPlanRepository.GetByIdAsync(viewModel.SubscriptionPlanId);
        if (subscriptionPlan == null)
        {
            ModelState.AddModelError(string.Empty, "Subscription plan not found.");
            ViewBag.SubscriptionPlans = new SelectList(subscriptionPlanRepository.GetAll(), "Id", "Name");
            return View(viewModel);
        }

        viewModel.UserId = user.Id;
        viewModel.UserEmail = user.Email;
        viewModel.UserName = user.UserName!;
        viewModel.SubscriptionPlanName = subscriptionPlan.Name!;

        ModelState.Clear();
        TryValidateModel(viewModel);

        if (ModelState.IsValid)
        {
            try
            {
                var subscription = await subscriptionRepository.GetByIdAsync(id);
                if (subscription == null)
                {
                    return NotFound();
                }

                subscription.UserId = viewModel.UserId;
                subscription.SubscriptionPlanId = viewModel.SubscriptionPlanId;
                subscription.StartDate = viewModel.StartDate;
                subscription.EndDate = viewModel.EndDate;
                subscription.Status = viewModel.Status;
                subscription.UserCode = viewModel.UserCode;
                subscription.Password = viewModel.Password;
                subscription.User = user;
                subscription.SubscriptionPlan = subscriptionPlan;

                await subscriptionRepository.UpdateAsync(subscription);

                var emailMessage = BuildSubscriptionEmail(user, subscription);

                await emailSender.SendEmailAsync(user.Email!, "New Subscription Assigned", emailMessage);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (await subscriptionRepository.GetByIdAsync(viewModel.Id) == null)
                {
                    return NotFound();
                }
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        return View(viewModel);
    }

    [HttpGet("iptv-subscription/delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var subscription = await context.Subscriptions
            .Include(s => s.User)
            .Include(s => s.SubscriptionPlan)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (subscription == null)
        {
            return NotFound();
        }

        var viewModel = new SubscriptionViewModel
        {
            Id = subscription.Id,
            UserName = subscription.User?.UserName ?? "Unknown",
            UserId = subscription.UserId,
            SubscriptionPlanId = subscription.SubscriptionPlanId,
            SubscriptionPlanName = subscription.SubscriptionPlan?.Name ?? "Unknown",
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            Status = subscription.Status,
            UserCode = subscription.UserCode,
            Password = subscription.Password
        };

        ViewData["Title"] = "Delete Subscription";
        return View(viewModel);
    }

    [HttpPost("iptv-subscription/delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(id);
        if (subscription == null)
        {
            return NotFound();
        }

        await subscriptionRepository.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private static string BuildSubscriptionEmail(UserDetails user, Subscription subscription)
    {
        return $@"
            <html>
            <body>
                <p>Dear {user.UserName},</p>
                <p>You have been assigned a new subscription:</p>
                <ul>
                    <li><strong>Subscription Plan:</strong> {subscription.SubscriptionPlan?.Name}</li>
                    <li><strong>Start Date:</strong> {subscription.StartDate:yyyy-MM-dd}</li>
                    <li><strong>End Date:</strong> {subscription.EndDate:yyyy-MM-dd}</li>
                    <li><strong>Status:</strong> {subscription.Status}</li>
                    <li><strong>User Code:</strong> {subscription.UserCode}</li>
                    <li><strong>Password:</strong> {subscription.Password}</li>
                </ul>
                <p>Instructions to use your subscription.</p><br>
                <p>you can download Iptv apps from following links:</p>
                <p>ANDROID [firestick](M3U & xtream code)</p>
                <ul>
                    <li><a href='https://www.iptvsmarters.com/smarters.apk'>IPTV Smarters Pro</a></li>
                    <li><a href='https://www.apkfollow.com/app/iptv-smarters-pro/com.nst.iptvsmarterstvbox/'>IPTV Smarters Pro</a></li>
                    <li><a href='https://tivimates.com/tiviapk'>TiviMate</a></li>
                </ul>
                <p>STB (MAC address)</p>
                <ul>
                    <li><a href='https://tivimates.com/tiviapk'>TiviMate</a></li>
                    <li><a href='https://quebeciptv.org/downloads/stb4k.apk'>STB Emulator 4k</a></li>
                    <li><a href='https://www.apkmonk.com/app/com.mvas.stb.emu.pro/'>STB Emu</a></li>
                    <li><a href='https://smart-stb.net/index.php?_url=/order/unlock-custom-portal-for-smart-tv'>Smart STB</a></li>
                </ul>
                <p>ANDROID phone [APKs]</p>
                <ul>
                    <li><a href='https://quebeciptv.net/v12.apk'>Android (username & password)</a></li>
                </ul>
                <p>Iphone (GSE IPTV App)</p>
                <ul>
                    <li><a href='https://apps.apple.com/us/app/gse-smart-iptv-pro/id1028734023'>GSE IPTV App</a></li>
                </ul>
                <p>More Instructions are available on the website setup section: https://quebeciptv.org/Home/Setup</p>
                <p>If you have any questions, Q&A is available on the website: https://quebeciptv.org/Home/Faq</p>
                <p>M3U: https://quebeciptv.net/get.php?username={subscription.UserCode}&password={subscription.Password}&type=m3u&output=mpegts</p>
                <p>M3U with options: https://quebeciptv.net/get.php?username={subscription.UserCode}&password={subscription.Password}&type=m3u_plus&output=mpegts</p>
                <p>EPG: https://quebeciptv.net/xmltv.php?username={subscription.UserCode}&password={subscription.Password}</p>
                <p>xtream code API:</p>
                <p>username: {subscription.UserCode}</p>
                <p>password: {subscription.Password}</p>
                <p>URL: https://quebeciptv.net/</p>
                <p>you can also contact us at any time to our customer service email: admin@quebeciptv.org</p>
                <h2>You may have to use a VPN as some provider block streaming</h2>
            </body>
            </html>";
    }
}
