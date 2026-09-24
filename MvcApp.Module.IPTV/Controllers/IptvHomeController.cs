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
            var captchaCode = random.Next(1000, 9999).ToString();
            HttpContext.Session.SetString("CaptchaCode", captchaCode);

            var svg = GenerateSvgCaptcha(captchaCode);
            return Content(svg, "image/svg+xml");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error generating captcha: {ex.Message}");
            return StatusCode(500, "Internal server error while generating captcha.");
        }
    }

    private static string GenerateSvgCaptcha(string code)
    {
        var random = new Random();
        var svg = new StringBuilder();
        svg.Append("<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40' viewBox='0 0 100 40'>");
        svg.Append("<rect width='100%' height='100%' fill='white'/>");

        for (int i = 0; i < 5; i++)
        {
            var x1 = random.Next(100);
            var y1 = random.Next(40);
            var x2 = random.Next(100);
            var y2 = random.Next(40);
            var color = $"rgb({random.Next(200)},{random.Next(200)},{random.Next(200)})";
            svg.Append($"<line x1='{x1}' y1='{y1}' x2='{x2}' y2='{y2}' stroke='{color}' stroke-width='1' />");
        }

        for (int i = 0; i < code.Length; i++)
        {
            var x = 15 + i * 20;
            var y = 25 + random.Next(-5, 5);
            var rotation = random.Next(-15, 15);
            var color = $"rgb(0,0,{random.Next(100, 200)})";
            svg.Append($"<text x='{x}' y='{y}' font-family='Arial' font-size='20' fill='{color}' transform='rotate({rotation} {x} {y})'>{code[i]}</text>");
        }

        svg.Append("</svg>");
        return svg.ToString();
    }
}
