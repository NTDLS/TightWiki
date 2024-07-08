// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using TightWiki.Library;
using TightWiki.Repository;

namespace TightWiki.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ResendEmailConfirmationModel : PageModelBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ResendEmailConfirmationModel(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, IEmailSender emailSender)
                        : base(signInManager)
        {
            _userManager = userManager;
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
            if (TightWiki.GlobalSettings.AllowSignup != true)
            {
                return Redirect("/Identity/Account/RegistrationIsNotAllowed");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (TightWiki.GlobalSettings.AllowSignup != true)
            {
                return Redirect("/Identity/Account/RegistrationIsNotAllowed");
            }
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Verification email sent. Please check your email.");
                return Page();
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { userId = userId, code = encodedCode },
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

            ModelState.AddModelError(string.Empty, "Verification email sent. Please check your email.");
            return Page();
        }
    }
}
