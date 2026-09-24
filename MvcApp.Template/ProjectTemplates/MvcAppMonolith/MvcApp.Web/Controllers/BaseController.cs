using DeepL;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Serilog;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using MvcApp.Localization;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Web.Models;
using MvcApp.Services;

namespace MvcApp.Web.Controllers;

public class BaseController : Controller
{
    private readonly ILanguageService _languageService;
    private readonly ILocalizationService _localizationService;
    private readonly IList<CultureInfo> _supportedCultures;
    private readonly Translator _translator;
    private readonly IConfiguration _configuration;
    private readonly IEmailSender _emailSender;

    public BaseController(
        ILanguageService languageService,
        ILocalizationService localizationService,
        IOptions<RequestLocalizationOptions> localizationOptions,
        Translator translator,
        IConfiguration configuration,
        IEmailSender emailSender)
    {
        _languageService = languageService;
        _localizationService = localizationService;
        _supportedCultures = localizationOptions.Value.SupportedCultures!;
        _translator = translator;
        _configuration = configuration;
        _emailSender = emailSender;
    }
    public string SourceLang => _configuration["DeepLConfig:SourceLang"]!;

    public HtmlString Localize(string resourceKey, params object[] args)
    {
        return LocalizeAsync(resourceKey, args).GetAwaiter().GetResult();
    }

    public async Task<HtmlString> LocalizeAsync(string resourceKey, params object[] args)
    {
        var currentCulture = Thread.CurrentThread.CurrentUICulture.Name;
        var language = _languageService.GetLanguageByCulture(currentCulture);

        if (language != null)
        {
            var stringResource = _localizationService.GetStringResource(resourceKey, language.Id);
            if (stringResource == null || string.IsNullOrEmpty(stringResource.Value))
            {
                foreach (var item in _supportedCultures)
                {
                    var languageId = _languageService?.GetLanguageByCulture(item.Name)?.Id;
                    var targetLanguage = GetTargetLanguage(languageId!.Value);

                    var translatedValue = await TranslateText(resourceKey, targetLanguage!);
                    var _resource = new StringResource
                    {
                        LanguageId = languageId!,
                        Name = resourceKey,
                        Value = translatedValue
                    };
                    _localizationService?.AddOrUpdateStringResource(_resource);
                }
                return new HtmlString(resourceKey);
            }

            return new HtmlString(args == null || args.Length == 0
                ? stringResource.Value
                : string.Format(stringResource.Value, args));
        }

        return new HtmlString(resourceKey);
    }

    public string LocString(string resourceKey, params object[] args)
    {
        return LocStringAsync(resourceKey, args).GetAwaiter().GetResult();
    }

    public async Task<string> LocStringAsync(string resourceKey, params object[] args)
    {
        var currentCulture = Thread.CurrentThread.CurrentUICulture.Name;
        var language = _languageService.GetLanguageByCulture(currentCulture);

        if (language != null)
        {
            var stringResource = _localizationService.GetStringResource(resourceKey, language.Id);
            if (stringResource == null || string.IsNullOrEmpty(stringResource.Value))
            {
                foreach (var item in _supportedCultures)
                {
                    var languageId = _languageService?.GetLanguageByCulture(item.Name)?.Id;
                    var targetLanguage = GetTargetLanguage(languageId!.Value);

                    var translatedValue = await TranslateText(resourceKey, targetLanguage!);
                    var _resource = new StringResource
                    {
                        LanguageId = languageId!,
                        Name = resourceKey,
                        Value = translatedValue
                    };
                    _localizationService?.AddOrUpdateStringResource(_resource);
                }
                return resourceKey;
            }

            return args == null || args.Length == 0
                ? stringResource.Value
                : string.Format(stringResource.Value, args);
        }

        return resourceKey;
    }

    [HttpPost]
    public IActionResult ChangeLanguage(string culture, string returnUrl)
    {
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            }
        );

        return LocalRedirect(returnUrl);
    }

    [HttpPost]
    public IActionResult SetCulture(string culture, string redirectUri)
    {
        Log.Information("SetCulture");
        if (culture != null)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), Secure = true, SameSite = SameSiteMode.None }
            );
        }

        return LocalRedirect(redirectUri);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
        
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> SendVerificationCode(string email)
    {
        var code = new Random().Next(100000, 999999).ToString();
        HttpContext.Session.SetString("RegisterVerificationCode", code);
        await _emailSender.SendEmailAsync(email, "Your Verification Code", "Your verification code",
            $"<p>Use the code below to complete your registration. It expires shortly.</p>" +
            $"<div style=\"display:inline-block;padding:12px 24px;background:#f3f4f6;border-radius:6px;font-size:26px;font-weight:700;letter-spacing:3px;color:#111827;\">{code}</div>");
        return Ok();
    }

    private string? GetTargetLanguage(int culture)
    {
        return culture switch
        {
            1 => _configuration?["DeepLConfig:TargetLangEn"]!,
            4 => _configuration?["DeepLConfig:TargetLangIt"]!,
            2 => _configuration?["DeepLConfig:TargetLangFr"]!,
            3 => _configuration?["DeepLConfig:TargetLangEs"]!,
            6 => _configuration?["DeepLConfig:TargetLangDe"]!,
            5 => _configuration?["DeepLConfig:TargetLangPt"]!,
            _ => null,
        };
    }    

    private async Task<string> TranslateText(string text, string targetLanguage)
    {
        try
        {
            var result = await _translator.TranslateTextAsync(text, SourceLang, targetLanguage);
            return result.Text; // Access the Text property after awaiting
        }
        catch (TooManyRequestsException)
        {
            // Log the error and implement a retry mechanism
            await Task.Delay(1000); // Wait for 1 second before retrying
            var retryResult = await _translator.TranslateTextAsync(text, SourceLang, targetLanguage);
            return retryResult.Text;
        }
        catch (Exception ex)
        {
            // Log other exceptions and return the original text as a fallback
            Console.WriteLine($"Translation failed: {ex.Message}");
            return text;
        }
    }

    //[HttpPost]
    //public async Task<IActionResult> HandleButtonClick(string buttonType)
    //{
    //    if (!User.Identity!.IsAuthenticated)
    //    {
    //        ViewBag.ShowLoginModal = true;
    //        ViewBag.ButtonType = buttonType;

    //        // Retrieve the model
    //        PlansViewModel viewModel = new PlansViewModel
    //        {
    //            SubscriptionPlans = await _context.SubscriptionPlans.Include(p => p.SubscriptionDetails).ToListAsync()
    //        };

    //        // Pass the model to the view
    //        return View("Index", viewModel);
    //    }
    //    else
    //    {
    //        return RedirectToAction("Index", "Store");
    //    }
    //}

    [HttpPost]
    public IActionResult SetCurrency(string currency, string returnUrl)
    {
        // Store the selected currency in session (or cookie). Example: session
        HttpContext.Session.SetString("SelectedCurrency", currency);

        // Return to the same page (fallback to home if none)
        returnUrl ??= "/";
        return LocalRedirect(returnUrl);
    }    

    [HttpGet]
    public IActionResult GenerateCaptcha()
    {
        try
        {
            var random = new Random();
            var captchaCode = random.Next(1000, 9999).ToString(); // Generate a 4-digit random number
            HttpContext.Session.SetString("CaptchaCode", captchaCode); // Store it in session

            // Generate a simple text-based captcha as SVG
            var svg = GenerateSvgCaptcha(captchaCode);
            return Content(svg, "image/svg+xml");
        }
        catch (Exception ex)
        {
            // Log the error
            Debug.WriteLine($"Error generating captcha: {ex.Message}");
            return StatusCode(500, "Internal server error while generating captcha.");
        }
    }

    private static string GenerateSvgCaptcha(string code)
    {
        var random = new Random();
        var svg = new StringBuilder();
        svg.Append($"<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40' viewBox='0 0 100 40'>");
        svg.Append("<rect width='100%' height='100%' fill='white'/>");

        // Add some random lines for noise
        for (int i = 0; i < 5; i++)
        {
            var x1 = random.Next(100);
            var y1 = random.Next(40);
            var x2 = random.Next(100);
            var y2 = random.Next(40);
            var color = $"rgb({random.Next(200)},{random.Next(200)},{random.Next(200)})";
            svg.Append($"<line x1='{x1}' y1='{y1}' x2='{x2}' y2='{y2}' stroke='{color}' stroke-width='1' />");
        }

        // Add the text
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