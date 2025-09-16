// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using TightWiki.Library;
using TightWiki.Library.Interfaces;
using TightWiki.Models;
using TightWiki.Repository;

namespace TightWiki.Areas.Identity.Pages.Account
{

    public class ForgotPasswordModel : PageModelBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWikiEmailSender _emailSender;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(
            ILogger<ForgotPasswordModel> logger, UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager, IWikiEmailSender emailSender)
            : base(signInManager)
        {
            _logger = logger;
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
            [Required(ErrorMessageResourceName = "RequiredAttribute_ValidationError", ErrorMessageResourceType = typeof(Models.Resources.ValTexts))]
            [EmailAddress(ErrorMessageResourceName = "EmailAddressAttribute_Invalid", ErrorMessageResourceType = typeof(Models.Resources.ValTexts))]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try{
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/ForgotPasswordConfirmation");
                }

                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", encodedCode },
                    protocol: Request.Scheme);

                var emailTemplate = new StringBuilder(ConfigurationRepository.Get<string>(Constants.ConfigurationGroup.Membership, "Template: Reset Password Email"));
                var basicConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName(Constants.ConfigurationGroup.Basic);
                var siteName = basicConfig.Value<string>("Name");
                var address = basicConfig.Value<string>("Address");
                var profile = UsersRepository.GetAccountProfileByUserId(Guid.Parse(user.Id));

                var emailSubject = "Reset password";
                emailTemplate.Replace("##SUBJECT##", emailSubject);
                emailTemplate.Replace("##ACCOUNTCOUNTRY##", profile.Country);
                emailTemplate.Replace("##ACCOUNTTIMEZONE##", profile.TimeZone);
                emailTemplate.Replace("##ACCOUNTLANGUAGE##", profile.Language);
                emailTemplate.Replace("##ACCOUNTEMAIL##", profile.EmailAddress);
                emailTemplate.Replace("##ACCOUNTNAME##", profile.AccountName);
                emailTemplate.Replace("##PERSONNAME##", $"{profile.FirstName} {profile.LastName}");
                emailTemplate.Replace("##CODE##", code);
                emailTemplate.Replace("##USERID##", user.Id);
                emailTemplate.Replace("##SITENAME##", siteName);
                emailTemplate.Replace("##SITEADDRESS##", address);
                emailTemplate.Replace("##CALLBACKURL##", HtmlEncoder.Default.Encode(callbackUrl));

                await _emailSender.SendEmailAsync(Input.Email, emailSubject, emailTemplate.ToString());

                return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/ForgotPasswordConfirmation");
            }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {Message}", ex.Message);
                ExceptionRepository.InsertException(ex);
            }
            return Page();
        }
    }
}
