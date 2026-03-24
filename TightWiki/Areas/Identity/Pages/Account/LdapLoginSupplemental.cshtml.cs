using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NTDLS.Helpers;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Library;
using TightWiki.Models;
using TightWiki.Repository;

namespace TightWiki.Areas.Identity.Pages.Account
{
    public class LdapLoginSupplementalInputModel
    {
        public List<TimeZoneItem> TimeZones { get; set; } = new();
        public List<CountryItem> Countries { get; set; } = new();
        public List<LanguageItem> Languages { get; set; } = new();

        [Required(ErrorMessageResourceName = "RequiredAttribute_ValidationError", ErrorMessageResourceType = typeof(Models.Resources.ValTexts))]
        [EmailAddress(ErrorMessageResourceName = "EmailAddressAttribute_Invalid", ErrorMessageResourceType = typeof(Models.Resources.ValTexts))]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Display Name")]
        [Required(ErrorMessageResourceName = "RequiredAttribute_ValidationError", ErrorMessageResourceType = typeof(Models.Resources.ValTexts))]
        public string AccountName { get; set; } = string.Empty;

        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string? LastName { get; set; } = string.Empty;

        [Display(Name = "Time-Zone")]
        [Required(ErrorMessageResourceName = "RequiredAttribute_ValidationError", ErrorMessageResourceType = typeof(Models.Resources.ValTexts))]
        public string TimeZone { get; set; } = string.Empty;

        [Display(Name = "Country")]
        [Required(ErrorMessageResourceName = "RequiredAttribute_ValidationError", ErrorMessageResourceType = typeof(Models.Resources.ValTexts))]
        public string Country { get; set; } = string.Empty;

        [Display(Name = "Language")]
        [Required(ErrorMessageResourceName = "RequiredAttribute_ValidationError", ErrorMessageResourceType = typeof(Models.Resources.ValTexts))]
        public string Language { get; set; } = string.Empty;
    }

    public class LdapLoginSupplementalModel : PageModelBase
    {
        [BindProperty(SupportsGet = true)]
        public Guid UserId { get; set; }

        [BindProperty]
        public string? ReturnUrl { get; set; }

        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<ITightEngine> _logger;
        private readonly ISharedLocalizationText _localizer;

        public LdapLoginSupplementalModel(
            ILogger<ITightEngine> logger,
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore, ISharedLocalizationText localizer)
            : base(logger, signInManager, localizer)
        {
            _logger = logger;
            _userManager = userManager;
            _localizer = localizer;
        }

        [BindProperty]
        public LdapLoginSupplementalInputModel Input { get; set; } = new LdapLoginSupplementalInputModel();

        public IActionResult OnGet()
        {
            try
            {
                ReturnUrl = WebUtility.UrlDecode(ReturnUrl ?? $"{GlobalConfiguration.BasePath}/");

                if (GlobalConfiguration.AllowSignup != true)
                {
                    return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/RegistrationIsNotAllowed");
                }

                PopulateDefaults();
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {Message}", ex.Message);
            }
            return Page();
        }

        private void PopulateDefaults()
        {
            Input.TimeZones = TimeZoneItem.GetAll();
            Input.Countries = CountryItem.GetAll();
            Input.Languages = LanguageItem.GetAll();

            var membershipConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName(Constants.WikiConfigurationGroup.Membership);

            if (string.IsNullOrEmpty(Input.TimeZone))
                Input.TimeZone = membershipConfig.Value<string>("Default TimeZone").EnsureNotNull();

            if (string.IsNullOrEmpty(Input.Country))
                Input.Country = membershipConfig.Value<string>("Default Country").EnsureNotNull();

            if (string.IsNullOrEmpty(Input.Language))
                Input.Language = membershipConfig.Value<string>("Default Language").EnsureNotNull();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                ReturnUrl = WebUtility.UrlDecode(ReturnUrl ?? $"{GlobalConfiguration.BasePath}/");

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
                    ModelState.AddModelError("Input.AccountName", _localizer["Display Name is required."]);
                    return Page();
                }
                else if (UsersRepository.DoesProfileAccountExist(Input.AccountName))
                {
                    ModelState.AddModelError("Input.AccountName", _localizer["Display Name is already in use."]);
                    return Page();
                }

                var user = await _userManager.FindByIdAsync(UserId.ToString());
                if (user == null)
                {
                    return NotifyOfError(_localizer["The specified user could not be found."]);
                }

                await _userManager.SetEmailAsync(user, Input.Email);

                var membershipConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName(Constants.WikiConfigurationGroup.Membership);
                UsersRepository.CreateProfile(Guid.Parse(user.Id), Input.AccountName);
                UsersRepository.AddRoleMemberByname(Guid.Parse(user.Id), membershipConfig.Value<string>("Default Signup Role").EnsureNotNull());

                var claimsToAdd = new List<Claim>
            {
                new ("timezone", Input.TimeZone),
                new (ClaimTypes.Country, Input.Country),
                new ("language", Input.Language),
                new ("firstname", Input.FirstName ?? ""),
                new ("lastname", Input.LastName ?? ""),
            };

                SecurityRepository.UpsertUserClaims(_userManager, user, claimsToAdd);

                await SignInManager.SignInAsync(user, isPersistent: false);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {Message}", ex.Message);
            }

            if (string.IsNullOrEmpty(ReturnUrl))
            {
                return Redirect($"{GlobalConfiguration.BasePath}/");
            }
            return Redirect(ReturnUrl);
        }
    }
}
