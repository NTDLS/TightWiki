using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using NTDLS.Helpers;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.AccessControl;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using TightWiki.Library;
using TightWiki.Library.Interfaces;
using TightWiki.Models;
using TightWiki.Repository;
using TightWiki.Resources.Areas.Identity.Pages.Account;

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

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty]
        public string? ReturnUrl { get; set; }

        public IList<AuthenticationScheme>? ExternalLogins { get; set; }

        public class InputModel
        {
            public List<TimeZoneItem> TimeZones { get; set; } = new();
            public List<CountryItem> Countries { get; set; } = new();
            public List<LanguageItem> Languages { get; set; } = new();


            [Display(Name = nameof(RegisterModel_InputModel.Account_Name), ResourceType = typeof(RegisterModel_InputModel))]
            [Required(ErrorMessageResourceName = nameof(RegisterModel_InputModel.Account_Name_is_required), ErrorMessageResourceType = typeof(RegisterModel_InputModel))]
            public string AccountName { get; set; } = string.Empty;

            [Display(Name = nameof(RegisterModel_InputModel.First_Name), ResourceType = typeof(RegisterModel_InputModel))]
            public string? FirstName { get; set; }

            [Display(Name = nameof(RegisterModel_InputModel.Last_Name), ResourceType = typeof(RegisterModel_InputModel))]
            public string? LastName { get; set; } = string.Empty;

            [Display(Name = nameof(RegisterModel_InputModel.Time_Zone), ResourceType = typeof(RegisterModel_InputModel))]
            [Required(ErrorMessageResourceName = nameof(RegisterModel_InputModel.TimeZone_is_required), ErrorMessageResourceType = typeof(RegisterModel_InputModel))]
            public string TimeZone { get; set; } = string.Empty;

            [Display(Name = nameof(RegisterModel_InputModel.Country), ResourceType = typeof(RegisterModel_InputModel))]
            [Required(ErrorMessageResourceName = nameof(RegisterModel_InputModel.Country_is_required), ErrorMessageResourceType = typeof(RegisterModel_InputModel))]
            public string Country { get; set; } = string.Empty;

            [Display(Name = nameof(RegisterModel_InputModel.Language), ResourceType = typeof(RegisterModel_InputModel))]
            [Required(ErrorMessageResourceName = nameof(RegisterModel_InputModel.Language_is_required), ErrorMessageResourceType = typeof(RegisterModel_InputModel))]
            public string Language { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            [Display(Name = nameof(RegisterModel_InputModel.Email), ResourceType = typeof(RegisterModel_InputModel))]
            public string Email { get; set; } = string.Empty;

            [Required]
            [StringLength(100, ErrorMessageResourceName = nameof(RegisterModel_InputModel.The_must_be_at_least_and_at_max_characters_long_), MinimumLength = 6, ErrorMessageResourceType = typeof(RegisterModel_InputModel))]
            [DataType(DataType.Password)]
            [Display(Name = nameof(RegisterModel_InputModel.Password), ResourceType = typeof(RegisterModel_InputModel))]
            public string Password { get; set; } = string.Empty;

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = nameof(RegisterModel_InputModel.Confirm_password), ResourceType = typeof(RegisterModel_InputModel))]
            [Compare("Password", ErrorMessageResourceName = nameof(RegisterModel_InputModel.The_password_and_confirmation_password_do_not_match_), ErrorMessageResourceType = typeof(RegisterModel_InputModel))]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        private void PopulateDefaults()
        {
            Input.TimeZones = TimeZoneItem.GetAll();
            Input.Countries = CountryItem.GetAll();
            Input.Languages = LanguageItem.GetAll();

            var membershipConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName(Constants.ConfigurationGroup.Membership);

            if (string.IsNullOrEmpty(Input.TimeZone))
                Input.TimeZone = membershipConfig.Value<string>("Default TimeZone").EnsureNotNull();

            if (string.IsNullOrEmpty(Input.Country))
                Input.Country = membershipConfig.Value<string>("Default Country").EnsureNotNull();

            if (string.IsNullOrEmpty(Input.Language))
                Input.Language = membershipConfig.Value<string>("Default Language").EnsureNotNull();
        }

        public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
        {
            ReturnUrl = WebUtility.UrlDecode(returnUrl ?? $"{GlobalConfiguration.BasePath}/");

            if (GlobalConfiguration.AllowSignup != true)
            {
                return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/RegistrationIsNotAllowed");
            }
            PopulateDefaults();

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = WebUtility.UrlDecode(returnUrl ?? $"{GlobalConfiguration.BasePath}/");

            if (GlobalConfiguration.AllowSignup != true)
            {
                return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/RegistrationIsNotAllowed");
            }

            PopulateDefaults();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Input.AccountName))
            {
                ModelState.AddModelError("Input.AccountName", "Account Name is required.");
                return Page();
            }
            else if (UsersRepository.DoesProfileAccountExist(Input.AccountName))
            {
                ModelState.AddModelError("Input.AccountName", "Account Name is already in use.");
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

                    var membershipConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName(Constants.ConfigurationGroup.Membership);

                    UsersRepository.CreateProfile(Guid.Parse(userId), Input.AccountName);

                    var claimsToAdd = new List<Claim>
                    {
                        new (ClaimTypes.Role, membershipConfig.Value<string>("Default Signup Role").EnsureNotNull()),
                        new ("timezone", Input.TimeZone),
                        new (ClaimTypes.Country, Input.Country),
                        new ("language", Input.Language),
                        new ("firstname", Input.FirstName ?? ""),
                        new ("lastname", Input.LastName ?? ""),
                    };

                    SecurityRepository.UpsertUserClaims(_userManager, user, claimsToAdd);

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                        var callbackUrl = Url.Page(
                            "/Account/ConfirmEmail",
                            pageHandler: null,
                            values: new { area = "Identity", userId = userId, code = encodedCode, returnUrl = ReturnUrl },
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
                        emailTemplate.Replace("##CALLBACKURL##", HtmlEncoder.Default.Encode(callbackUrl ?? string.Empty));

                        await _emailSender.SendEmailAsync(Input.Email, emailSubject, emailTemplate.ToString());

                        return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/RegisterConfirmation?email={Input.Email}&returnUrl={WebUtility.UrlEncode(returnUrl)}");
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
