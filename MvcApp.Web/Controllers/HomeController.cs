using DeepL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using MvcApp.Infrastructure;
using MvcApp.Localization;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Core.Models;
using MvcApp.Infrastructure.Helpers;
using MvcApp.Services;
using MvcApp.Web.Models.HomeViewModels;

namespace MvcApp.Web.Controllers
{
    public class HomeController : BaseController
    {

        //private readonly UserManager<ApplicationUser> userManager;
        //private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<UserDetails> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITemplateService _templateService;
        private readonly ICompositeViewEngine _viewEngine;

        public HomeController(ILanguageService languageService,
         ILocalizationService localizationService,
         IOptions<RequestLocalizationOptions> localizationOptions,
         Translator translator,
         IConfiguration configuration,
         IEmailSender emailSender,
         UrlEncoder urlEncoder,
         UserManager<UserDetails> userManager,
         IUnitOfWork unitOfWork,
         ITemplateService templateService,
         ICompositeViewEngine viewEngine) : base(languageService,
             localizationService,
             localizationOptions,
             translator,
             configuration,
             emailSender)
        {
            _emailSender = emailSender;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _templateService = templateService;
            _viewEngine = viewEngine;
        }

        private bool ViewExists(string viewName)
        {
            var result = _viewEngine.FindView(ControllerContext, viewName, isMainPage: false);
            return result.View != null;
        }

        private async Task<string> ResolveViewAsync(string baseName)
        {
            var template = await _templateService.GetActiveTemplateAsync();
            var viewName = $"{baseName}.{template}";
            return ViewExists(viewName) ? viewName : $"{baseName}.Default";
        }

        [TempData]
        public string? ErrorMessage { get; set; }


        [TempData]
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var membersParams = new MvcApp.Core.Pagination.MemberParameters
            {
                CurrentUsername = currentUser?.UserName ?? "",
                PageNumber = 1,
                PageSize = 12
            };
            var members = await _unitOfWork.MemberRepository.GetMembersAsync(membersParams);
            ViewBag.FeaturedMembers = members;

            var template = await _templateService.GetActiveTemplateAsync();
            var viewName = await _templateService.IsLandingPageEnabledAsync(template)
                ? await ResolveViewAsync("Index")
                : "Index.Default";
            return View(viewName);
        }

        public async Task<IActionResult> Faq()
        {
            ViewData["Description"] = @Localize("Frequently asked questions.");
            ViewData["Keywords"] = "FAQ, help, support";
            return View(await ResolveViewAsync("Faq"));
        }
        public async Task<IActionResult> Contact()
        {
            ViewData["Title"] = @Localize("Contact");
            ViewData["Description"] = @Localize("Get in touch with us.");
            ViewData["Keywords"] = "contact, support, help";
            ViewData["Message"] = @LocString("Your contact page.");

            var model = new ContactViewModel
            {
                Name = string.Empty,
                Email = string.Empty,
                Subject = string.Empty,
                Message = string.Empty,
                CaptchaCode = string.Empty // Initialize required property
            };

            // Generate a CAPTCHA code and store it in the session
            var random = new Random();
            var captchaCode = random.Next(1000, 9999).ToString(); // Generate a 4-digit random number
            HttpContext.Session.SetString("CaptchaCode", captchaCode);

            // Optionally, pass the CAPTCHA code to the view for rendering (e.g., as an image or text)
            ViewData["CaptchaCode"] = captchaCode;

            return View(await ResolveViewAsync("Contact"), model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactViewModel model)
        {
            ViewData["Title"] = @Localize("Contact");
            ViewData["Description"] = @Localize("Get in touch with us.");
            ViewData["Keywords"] = "contact, support, help";
            ViewData["Message"] = @LocString("Your contact page.");

            // Validate CAPTCHA
            var captchaCode = HttpContext.Session.GetString("CaptchaCode");
            if (string.IsNullOrEmpty(captchaCode) || captchaCode != model.CaptchaCode)
            {
                var errorMessage = @Localize("Invalid CAPTCHA code.").ToString(); // Convert HtmlString to string
                ModelState.AddModelError("CaptchaCode", errorMessage);
            }

            if (ModelState.IsValid)
            {
                // Process the contact form (e.g., send an email)
                await _emailSender.SendFromContactAsync(model.Name, model.Email, model.Subject, model.Message);

                TempData["StatusMessage"] = @Localize("Your message has been sent successfully.").ToString();
                return RedirectToAction(nameof(Contact));
            }

            // If validation fails, redisplay the form
            return View(await ResolveViewAsync("Contact"), model);
        }


        public async Task<IActionResult> Privacy()
        {
            ViewData["Description"] = @Localize("Our privacy policy.");
            ViewData["Keywords"] = "privacy, policy, data";
            return View(await ResolveViewAsync("Privacy"));
        }

        public IActionResult Test()
        {
            return View();
        }


    }
}
