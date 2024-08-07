// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using TightWiki.Configuration;
using TightWiki.Library.Interfaces;
using TightWiki.Repository;


namespace TightWiki.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModelBase
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IWikiEmailSender _emailSender;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IWikiEmailSender emailSender)
                        : base(signInManager)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
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
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

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
            [Display(Name = "Email")]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }


        public async Task<IActionResult> OnGetAsync(string returnUrl = null)
        {
            if (GlobalConfiguration.AllowSignup != true)
            {
                return Redirect("/Identity/Account/RegistrationIsNotAllowed");
            }
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            if (GlobalConfiguration.AllowSignup != true)
            {
                return Redirect("/Identity/Account/RegistrationIsNotAllowed");
            }

            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                var user = new IdentityUser()
                {
                    UserName = Input.Email,
                    Email = Input.Email
                };

                var result = await _userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    var userId = await _userManager.GetUserIdAsync(user);

                    var membershipConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Membership");

                    UsersRepository.CreateProfile(Guid.Parse(userId));

                    var claimsToAdd = new List<Claim>
                    {
                        new (ClaimTypes.Role, membershipConfig.Value<string>("Default Signup Role")),
                        new ("timezone", membershipConfig.Value<string>("Default TimeZone")),
                        new (ClaimTypes.Country, membershipConfig.Value<string>("Default Country")),
                        new ("language", membershipConfig.Value<string>("Default Language")),
                    };

                    SecurityRepository.UpsertUserClaims(_userManager, user, claimsToAdd);

                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = encodedCode, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    var emailTemplate = new StringBuilder(ConfigurationRepository.Get<string>("Membership", "Template: Account Verification Email"));
                    var basicConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Basic");
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

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
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
