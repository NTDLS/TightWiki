// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.Repository;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Areas.Identity.Pages.Account
{

    public class ForgotPasswordModel : PageModelBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ITwEmailSender _emailSender;
        private readonly ILogger<ITwEngine> _logger;

        public ForgotPasswordModel(
            ILogger<ITwEngine> logger, UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager, ITwEmailSender emailSender, ITwSharedLocalizationText localizer, TwConfiguration wikiConfiguration)
            : base(logger, signInManager, localizer, wikiConfiguration)
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
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var user = await _userManager.FindByEmailAsync(Input.Email);
                    if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                    {
                        // Don't reveal that the user does not exist or is not confirmed
                        return Redirect($"{WikiConfiguration.BasePath}/Identity/Account/ForgotPasswordConfirmation");
                    }

                    var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ResetPassword",
                        pageHandler: null,
                        values: new { area = "Identity", encodedCode },
                        protocol: Request.Scheme);

                    var configEmailTemplate = await ConfigurationRepository.Get<string>(WikiConfigurationGroup.Membership, "Template: Reset Password Email");

                    var emailTemplate = new StringBuilder(configEmailTemplate);
                    var basicConfig = await ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Basic);
                    var siteName = basicConfig.Value<string>("Name");
                    var address = basicConfig.Value<string>("Address");
                    var profile = await UsersRepository.GetAccountProfileByUserId(Guid.Parse(user.Id));

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

                    return Redirect($"{WikiConfiguration.BasePath}/Identity/Account/ForgotPasswordConfirmation");
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
