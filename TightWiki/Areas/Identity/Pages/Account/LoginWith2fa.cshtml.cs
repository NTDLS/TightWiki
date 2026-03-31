// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;
using TightWiki.Pages;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Areas.Identity.Pages.Account
{
    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class LoginWith2faInputModel
    {
        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [Required]
        [StringLength(7, MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Authenticator code")]
        public string TwoFactorCode { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [Display(Name = "Remember this machine")]
        public bool RememberMachine { get; set; }
    }

    public class LoginWith2faModel : TwPageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<ITwEngine> _logger;

        public LoginWith2faModel(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ILogger<ITwEngine> logger, ITwSharedLocalizationText localizer, TwConfiguration wikiConfiguration, ITwDatabaseManager databaseManager)
                        : base(logger, signInManager, localizer, wikiConfiguration, databaseManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public LoginWith2faInputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public bool RememberMe { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        public async Task<IActionResult> OnGetAsync(bool rememberMe, string returnUrl = null)
        {
            try
            {
                ReturnUrl = WebUtility.UrlDecode(returnUrl ?? $"{WikiConfiguration.BasePath}/");

                // Ensure the user has gone through the username & password screen first
                var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

                if (user == null)
                {
                    throw new InvalidOperationException($"Unable to load two-factor authentication user.");
                }

                RememberMe = rememberMe;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {Message}", ex.Message);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(bool rememberMe, string returnUrl = null)
        {
            try
            {
                ReturnUrl = WebUtility.UrlDecode(returnUrl ?? $"{WikiConfiguration.BasePath}/");

                if (!ModelState.IsValid)
                {
                    return Page();
                }

                var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
                if (user == null)
                {
                    throw new InvalidOperationException($"Unable to load two-factor authentication user.");
                }

                var authenticatorCode = Input.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

                var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe, Input.RememberMachine);

                var userId = await _userManager.GetUserIdAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User with ID '{UserId}' logged in with 2fa.", user.Id);
                    return Redirect(ReturnUrl);
                }
                else if (result.IsLockedOut)
                {
                    _logger.LogWarning("User with ID '{UserId}' account locked out.", user.Id);
                    return Redirect($"{WikiConfiguration.BasePath}/Identity/Account/Lockout");
                }
                else
                {
                    _logger.LogWarning("Invalid authenticator code entered for user with ID '{UserId}'.", user.Id);
                    ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {Message}", ex.Message);
            }
            return Page();
        }
    }
}
