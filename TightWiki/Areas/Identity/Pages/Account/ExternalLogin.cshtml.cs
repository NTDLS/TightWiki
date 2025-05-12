// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using TightWiki.Library;
using TightWiki.Library.Interfaces;
using TightWiki.Models;
using TightWiki.Repository;

namespace TightWiki.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModelBase
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly IWikiEmailSender _emailSender;
        private readonly ILogger<ExternalLoginModel> _logger;

        public ExternalLoginModel(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            ILogger<ExternalLoginModel> logger,
            IWikiEmailSender emailSender)
                        : base(signInManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _logger = logger;
            _emailSender = emailSender;
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
        public string ProviderDisplayName { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public IActionResult OnGet()
        {
            return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/Login");
        }

        public IActionResult OnPost(string provider, string returnUrl = null)
        {
            ReturnUrl = WebUtility.UrlDecode(returnUrl ?? $"{GlobalConfiguration.BasePath}/");

            // Request a redirect to the external login provider.
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { ReturnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
        {
            ReturnUrl = WebUtility.UrlDecode(returnUrl ?? $"{GlobalConfiguration.BasePath}/");

            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/Login?ReturnUrl={WebUtility.UrlEncode(ReturnUrl)}");
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information.";
                return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/Login?ReturnUrl={WebUtility.UrlEncode(ReturnUrl)}");
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                _logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity.Name, info.LoginProvider);
                return Redirect(ReturnUrl);
            }
            if (result.IsLockedOut)
            {
                return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/Lockout");
            }
            else
            {
                // If the user does not have an account, then ask the user to create an account.
                ProviderDisplayName = info.ProviderDisplayName;
                if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
                {
                    Input = new InputModel
                    {
                        Email = info.Principal.FindFirstValue(ClaimTypes.Email)
                    };
                }
                return Page();
            }
        }

        public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
        {
            ReturnUrl = WebUtility.UrlDecode(returnUrl ?? $"{GlobalConfiguration.BasePath}/");

            // Get the information about the user from the external login provider
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information during confirmation.";
                return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/Login?ReturnUrl={WebUtility.UrlEncode(ReturnUrl)}");
            }

            if (ModelState.IsValid)
            {
                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded == false)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

                        var userId = await _userManager.GetUserIdAsync(user);
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                        var callbackUrl = Url.Page(
                            "/Account/ConfirmEmail",
                            pageHandler: null,
                            values: new { area = "Identity", userId = userId, code = encodedCode },
                            protocol: Request.Scheme);

                        var emailTemplate = new StringBuilder(ConfigurationRepository.Get<string>(Constants.ConfigurationGroup.Membership, "Template: Account Verification Email"));
                        var basicConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName(Constants.ConfigurationGroup.Basic);
                        var siteName = basicConfig.Value<string>("Name");
                        var address = basicConfig.Value<string>("Address");
                        var profile = UsersRepository.GetAccountProfileByUserId(Guid.Parse(userId));

                        var emailSubject = "Confirm your email";
                        emailTemplate.Replace("##SUBJECT##", emailSubject);
                        emailTemplate.Replace("##ACCOUNTCOUNTRY##", profile.Country);
                        emailTemplate.Replace("##ACCOUNTTIMEZONE##", profile.TimeZone);
                        emailTemplate.Replace("##ACCOUNTLANGUAGE##", profile.Language);
                        emailTemplate.Replace("##ACCOUNTEMAIL##", profile.EmailAddress);
                        emailTemplate.Replace("##ACCOUNTNAME##", profile.AccountName);
                        emailTemplate.Replace("##PERSONNAME##", $"{profile.FirstName} {profile.LastName}");
                        emailTemplate.Replace("##CODE##", code);
                        emailTemplate.Replace("##USERID##", userId);
                        emailTemplate.Replace("##SITENAME##", siteName);
                        emailTemplate.Replace("##SITEADDRESS##", address);
                        emailTemplate.Replace("##CALLBACKURL##", HtmlEncoder.Default.Encode(callbackUrl));

                        await _emailSender.SendEmailAsync(Input.Email, emailSubject, emailTemplate.ToString());

                        // If account confirmation is required, we need to show the link if we don't have a real email sender
                        if (_userManager.Options.SignIn.RequireConfirmedAccount)
                        {
                            return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/RegisterConfirmation?Email={Input.Email}");
                        }

                        await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
                        return Redirect(ReturnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ProviderDisplayName = info.ProviderDisplayName;
            return Page();
        }

        private IdentityUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<IdentityUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(IdentityUser)}'. " +
                    $"Ensure that '{nameof(IdentityUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the external login page in /Areas/Identity/Pages/Account/ExternalLogin.cshtml");
            }
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }
}
