using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MvcApp.Common.Filters;
using MvcApp.Infrastructure;
using MvcApp.Module.IPTV.Models.HomeViewModels;
using MvcApp.Module.IPTV.Models.Localization;

namespace MvcApp.Module.IPTV.Controllers;

[ModuleEnabledFilter("Iptv")]
[Microsoft.AspNetCore.Authorization.AllowAnonymous]
public class IptvHomeController(UserDbContext context, IStringLocalizer<SharedResource> localizer) : Controller
{
    [TempData]
    public string? StatusMessage { get; set; }

    [HttpGet("iptv")]
    public async Task<IActionResult> Index()
    {
        var viewModel = new PlansViewModel
        {
            SubscriptionPlans = await context.SubscriptionPlans.Include(p => p.SubscriptionDetails).ToListAsync()
        };

        ViewData["StatusMessage"] = StatusMessage;
        return View(viewModel);
    }

    [HttpGet("iptv/setup")]
    public IActionResult Setup()
    {
        ViewData["Title"] = localizer["Setup - Quebec Iptv"];
        ViewData["Description"] = localizer["Learn how to set up your IPTV service."];
        ViewData["Keywords"] = "IPTV, Quebec, Streaming, Live Channels, Online TV, VOD, Canada TV";

        return View();
    }

    [HttpGet("iptv/channels")]
    public IActionResult Channels()
    {
        ViewData["Title"] = localizer["Channels - Quebec Iptv"];
        ViewData["Description"] = localizer["Browse our selection of live TV channels."];
        ViewData["Keywords"] = "IPTV, Quebec, Streaming, Live Channels, Online TV, VOD, Canada TV";

        return View();
    }

    [HttpPost("iptv/set-currency")]
    public IActionResult SetCurrency(string currency, string? returnUrl)
    {
        HttpContext.Session.SetString("SelectedCurrency", currency);

        returnUrl ??= "/";
        return LocalRedirect(returnUrl);
    }

    [HttpPost("iptv/change-language")]
    public IActionResult ChangeLanguage(string culture, string? returnUrl)
    {
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            }
        );

        returnUrl ??= "/";
        return LocalRedirect(returnUrl);
    }

    [HttpGet("iptv/generate-captcha")]
    public IActionResult GenerateCaptcha()
    {
        try
        {
            var random = new Random();
            var captchaCode = random.Next(100000, 999999).ToString();
            HttpContext.Session.SetString("CaptchaCode", captchaCode);

            var png = MvcApp.Common.Captcha.CaptchaChallenge.RenderPng(captchaCode);
            return File(png, "image/png");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error generating captcha: {ex.Message}");
            return StatusCode(500, "Internal server error while generating captcha.");
        }
    }
}
