// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Text.Encodings.Web;
using TightWiki.Library;
using TightWiki.Repository;
using IEmailSender = TightWiki.Library.IEmailSender;

namespace TightWiki.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModelBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;

        public RegisterConfirmationModel(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, IEmailSender emailSender)
                        : base(signInManager)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public async Task<IActionResult> OnGetAsync(string email, string returnUrl = null)
        {
            if (TightWiki.GlobalSettings.AllowSignup != true)
            {
                return Redirect("/Identity/Account/RegistrationIsNotAllowed");
            }
            if (email == null)
            {
                return RedirectToPage("/Index");
            }
            returnUrl = returnUrl ?? Url.Content("~/");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound($"Unable to load user with email '{email}'.");
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = userId, code = encodedCode, returnUrl = returnUrl },
                protocol: Request.Scheme);

            var emailTemplate = ConfigurationRepository.Get<string>("Membership", "Template: Account Verification Email");
            var basicConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Basic");
            var siteName = basicConfig.As<string>("Name");
            var address = basicConfig.As<string>("Address");
            var profile = ProfileRepository.GetAccountProfileByUserId(Guid.Parse(userId));

            var emailSubject = "Confirm your account";
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

            await _emailSender.SendEmailAsync(email, emailSubject, emailTemplate);

            return Page();
        }
    }
}
