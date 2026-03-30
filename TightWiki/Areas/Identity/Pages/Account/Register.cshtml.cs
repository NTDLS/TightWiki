using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using NTDLS.Helpers;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using TightWiki.Pages;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Library;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Areas.Identity.Pages.Account
{
    public class RegisterInputModel
    {
        public List<TwTimeZoneItem> TimeZones { get; set; } = new();
        public List<TwCountryItem> Countries { get; set; } = new();
        public List<TwLanguageItem> Languages { get; set; } = new();


        [Display(Name = "Display Name")]
        [Required]
        public string AccountName { get; set; } = string.Empty;

        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string? LastName { get; set; } = string.Empty;

        [Display(Name = "Time-Zone")]
        [Required]
        public string TimeZone { get; set; } = string.Empty;

        [Display(Name = "Country")]
        [Required]
        public string Country { get; set; } = string.Empty;

        [Display(Name = "Language")]
        [Required]
        public string Language { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password")]
        [Required]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class RegisterModel : TwPageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly ILogger<ITwEngine> _logger;
        private readonly ITwEmailSender _emailSender;
        private readonly ITwSharedLocalizationText _localizer;
        private readonly ITwConfigurationRepository _configurationRepository;
        private readonly ITwUsersRepository _usersRepository;


        public RegisterModel(
                UserManager<IdentityUser> userManager,
                IUserStore<IdentityUser> userStore,
                SignInManager<IdentityUser> signInManager,
                ILogger<ITwEngine> logger,
                ITwEmailSender emailSender,
                ITwSharedLocalizationText localizer,
                TwConfiguration wikiConfiguration,
                ITwConfigurationRepository configurationRepository,
                ITwUsersRepository usersRepository,
                ITwDatabaseManager databaseManager
            )
            : base(logger, signInManager, localizer, wikiConfiguration, databaseManager)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _localizer = localizer;
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _configurationRepository = configurationRepository;
            _usersRepository = usersRepository;
        }

        [BindProperty]
        public RegisterInputModel Input { get; set; } = new();

        [BindProperty]
        public string? ReturnUrl { get; set; }

        public IList<AuthenticationScheme>? ExternalLogins { get; set; }

        private async Task PopulateDefaults()
        {
            Input.TimeZones = TwTimeZoneItem.GetAll();
            Input.Countries = TwCountryItem.GetAll();
            Input.Languages = TwLanguageItem.GetAll();

            var membershipConfig = await _configurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Membership);

            if (string.IsNullOrEmpty(Input.TimeZone))
                Input.TimeZone = membershipConfig.Value<string>("Default TimeZone").EnsureNotNull();

            if (string.IsNullOrEmpty(Input.Country))
                Input.Country = membershipConfig.Value<string>("Default Country").EnsureNotNull();

            if (string.IsNullOrEmpty(Input.Language))
                Input.Language = membershipConfig.Value<string>("Default Language").EnsureNotNull();
        }

        public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
        {
            try
            {
                ReturnUrl = WebUtility.UrlDecode(returnUrl ?? $"{WikiConfiguration.BasePath}/");

                if (WikiConfiguration.AllowSignup != true)
                {
                    return Redirect($"{WikiConfiguration.BasePath}/Identity/Account/RegistrationIsNotAllowed");
                }
                await PopulateDefaults();

                ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {Message}", ex.Message);
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            try
            {
                ReturnUrl = WebUtility.UrlDecode(returnUrl ?? $"{WikiConfiguration.BasePath}/");

                if (WikiConfiguration.AllowSignup != true)
                {
                    return Redirect($"{WikiConfiguration.BasePath}/Identity/Account/RegistrationIsNotAllowed");
                }

                await PopulateDefaults();

                if (!ModelState.IsValid)
                {
                    return Page();
                }

                if (string.IsNullOrWhiteSpace(Input.AccountName))
                {
                    ModelState.AddModelError("Input.AccountName", _localizer["Display Name is required."]);
                    return Page();
                }
                else if (await _usersRepository.DoesProfileAccountExist(Input.AccountName))
                {
                    ModelState.AddModelError("Input.AccountName", _localizer["Display Name is already in use."]);
                    return Page();
                }

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

                        var membershipConfig = await _configurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Membership);
                        await _usersRepository.CreateProfile(Guid.Parse(userId), Input.AccountName);
                        await _usersRepository.AddRoleMemberByname(Guid.Parse(user.Id), membershipConfig.Value<string>("Default Signup Role").EnsureNotNull());

                        var claimsToAdd = new List<Claim>
                        {
                            new ("timezone", Input.TimeZone),
                            new (ClaimTypes.Country, Input.Country),
                            new ("language", Input.Language),
                            new ("firstname", Input.FirstName ?? ""),
                            new ("lastname", Input.LastName ?? ""),
                        };

                        await _usersRepository.UpsertUserClaims(_userManager, user, claimsToAdd);

                        if (_userManager.Options.SignIn.RequireConfirmedAccount)
                        {
                            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                            var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                            var callbackUrl = Url.Page(
                                "/Account/ConfirmEmail",
                                pageHandler: null,
                                values: new { area = "Identity", userId = userId, code = encodedCode, returnUrl = ReturnUrl },
                                protocol: Request.Scheme);

                            var configEmailTemplate = await _configurationRepository.Get<string>(WikiConfigurationGroup.Membership, "Template: Account Verification Email");
                            var emailTemplate = new StringBuilder(configEmailTemplate);
                            var basicConfig = await _configurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Basic);
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
                            emailTemplate.Replace("##CALLBACKURL##", HtmlEncoder.Default.Encode(callbackUrl ?? string.Empty));

                            await _emailSender.SendEmailAsync(Input.Email, emailSubject, emailTemplate.ToString());

                            return Redirect($"{WikiConfiguration.BasePath}/Identity/Account/RegisterConfirmation?email={Input.Email}&returnUrl={WebUtility.UrlEncode(returnUrl)}");
                        }
                        else
                        {
                            await _signInManager.SignInAsync(user, isPersistent: false);
                            return Redirect(ReturnUrl);
                        }
                    }
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {Message}", ex.Message);
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
