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
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Library;
using TightWiki.Library.Interfaces;
using TightWiki.Models;
using TightWiki.Repository;
using static TightWiki.Library.Constants;

namespace TightWiki.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ResendEmailConfirmationModel : PageModelBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWikiEmailSender _emailSender;
        private readonly ILogger<ITightEngine> _logger;
        private readonly ISharedLocalizationText _localizer;

        public ResendEmailConfirmationModel(
            ILogger<ITightEngine> logger,
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            IWikiEmailSender emailSender,
            ISharedLocalizationText localizer, TightWikiConfiguration wikiConfiguration)
                        : base(logger, signInManager, localizer, wikiConfiguration)
        {
            _logger = logger;
            _userManager = userManager;
            _emailSender = emailSender;
            _localizer = localizer;
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

        public IActionResult OnGet()
        {
            try
            {
                if (WikiConfiguration.AllowSignup != true)
                {
                    return Redirect($"{WikiConfiguration.BasePath}/Identity/Account/RegistrationIsNotAllowed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {Message}", ex.Message);
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (WikiConfiguration.AllowSignup != true)
                {
                    return Redirect($"{WikiConfiguration.BasePath}/Identity/Account/RegistrationIsNotAllowed");
                }
                if (!ModelState.IsValid)
                {
                    return Page();
                }

                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, _localizer["Verification email sent. Please check your email."]);
                    return Page();
                }

                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId = userId, code = encodedCode },
                    protocol: Request.Scheme);

                var configEmailTemplate = await ConfigurationRepository.Get<string>(WikiConfigurationGroup.Membership, "Template: Account Verification Email");
                var emailTemplate = new StringBuilder(configEmailTemplate);
                var basicConfig = await ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Basic);
                var siteName = basicConfig.Value<string>("Name");
                var address = basicConfig.Value<string>("Address");
                var profile = await UsersRepository.GetAccountProfileByUserId(Guid.Parse(userId));

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

                ModelState.AddModelError(string.Empty, _localizer["Verification email sent. Please check your email."]);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {Message}", ex.Message);
            }
            return Page();
        }
    }
}
