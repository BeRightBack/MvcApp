// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;

namespace MvcApp.Identity.Pages.Account
{
    [EnableRateLimiting("auth")]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<UserDetails> _signInManager;
        private readonly UserManager<UserDetails> _userManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly ISettingsService _settings;
        private readonly IBanService _banService;

        public LoginModel(
            SignInManager<UserDetails> signInManager,
            UserManager<UserDetails> userManager,
            ILogger<LoginModel> logger,
            ISettingsService settings,
            IBanService banService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _settings = settings;
            _banService = banService;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            [Required]
            [Display(Name = "Username")]
            public string Username { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(Input.Username);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }

                // Check DB-configured lockout before signing in
                var maxAttempts = await _settings.GetAsync<int>("MaxLoginAttempts") ?? 3;
                var lockoutMinutes = await _settings.GetAsync<int>("LockoutDurationMinutes") ?? 15;

                if (await _userManager.IsLockedOutAsync(user))
                {
                    _logger.LogWarning("User {Username} account locked out.", user.UserName);
                    return RedirectToPage("./Lockout");
                }

                if (user.AccessFailedCount >= maxAttempts)
                {
                    var lockoutEnd = DateTimeOffset.UtcNow.AddMinutes(lockoutMinutes);
                    await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);
                    await _userManager.UpdateAsync(user);
                    _logger.LogWarning("User {Username} account locked out (DB setting: {MaxAttempts} attempts, {Minutes} min).", user.UserName, maxAttempts, lockoutMinutes);
                    return RedirectToPage("./Lockout");
                }

                var activeBan = await _banService.GetActiveBanAsync(user.Id);
                if (activeBan != null)
                {
                    _logger.LogWarning("User {Username} login blocked - banned until {Until}",
                        user.UserName, activeBan.BannedUntil?.ToString("o") ?? "indefinite");
                    ModelState.AddModelError(string.Empty, activeBan.BannedUntil.HasValue
                        ? $"Your account is suspended until {activeBan.BannedUntil:yyyy-MM-dd HH:mm}."
                        : "Your account has been suspended.");
                    return Page();
                }

                var result = await _signInManager.PasswordSignInAsync(user, Input.Password, Input.RememberMe, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Username} logged in.", user.UserName);
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User {Username} account locked out.", user.UserName);
                    return RedirectToPage("./Lockout");
                }
                if (result.IsNotAllowed)
                {
                    _logger.LogWarning("User {Username} login not allowed - EmailConfirmed: {EmailConfirmed}", 
                        user.UserName, user.EmailConfirmed);
                    ModelState.AddModelError(string.Empty, "Account not confirmed. Please confirm your email.");
                    return Page();
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
