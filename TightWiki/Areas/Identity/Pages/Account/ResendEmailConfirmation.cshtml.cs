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
using TightWiki.Pages;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ResendEmailConfirmationModel : TwPageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ITwEmailSender _emailSender;
        private readonly ILogger<ITwEngine> _logger;
        private readonly ITwSharedLocalizationText _localizer;
        private readonly ITwConfigurationRepository _configurationRepository;
        private readonly ITwUsersRepository _usersRepository;

        public ResendEmailConfirmationModel(
                ILogger<ITwEngine> logger,
                SignInManager<IdentityUser> signInManager,
                UserManager<IdentityUser> userManager,
                ITwEmailSender emailSender,
                ITwSharedLocalizationText localizer,
                TwConfiguration wikiConfiguration,
                ITwConfigurationRepository configurationRepository,
                ITwUsersRepository usersRepository,
                ITwDatabaseManager databaseManager
            )
            : base(logger, signInManager, localizer, wikiConfiguration, databaseManager)
        {
            _logger = logger;
            _userManager = userManager;
            _emailSender = emailSender;
            _localizer = localizer;
            _configurationRepository = configurationRepository;
            _usersRepository = usersRepository;
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

                var configEmailTemplate = await _configurationRepository.Get<string>(TwConfigGroup.Membership, "Template: Account Verification Email");
                var emailTemplate = new StringBuilder(configEmailTemplate);
                var basicConfig = await _configurationRepository.GetConfigurationEntryValuesByGroupName(TwConfigGroup.Basic);
                var siteName = basicConfig.Value<string>("Name");
                var address = basicConfig.Value<string>("Address");
                var profile = await _usersRepository.GetAccountProfileByUserId(Guid.Parse(userId));

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
