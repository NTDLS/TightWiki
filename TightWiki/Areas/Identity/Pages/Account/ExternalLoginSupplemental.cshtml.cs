using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using NTDLS.Helpers;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using TightWiki.Library;
using TightWiki.Models;
using TightWiki.Repository;

namespace TightWiki.Areas.Identity.Pages.Account
{
    public class ExternalLoginSupplementalInputModel
    {
        public List<TimeZoneItem> TimeZones { get; set; } = new();
        public List<CountryItem> Countries { get; set; } = new();
        public List<LanguageItem> Languages { get; set; } = new();


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


    public class ExternalLoginSupplementalModel : PageModelBase
    {
        [BindProperty]
        public string? ReturnUrl { get; set; }

        private UserManager<IdentityUser> _userManager;
        private readonly IStringLocalizer<ExternalLoginSupplementalModel> _localizer;

        public ExternalLoginSupplementalModel(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            IStringLocalizer<ExternalLoginSupplementalModel> localizer)
            : base(signInManager)
        {
            _userManager = userManager;
            _localizer = localizer;
        }

        [BindProperty]
        public ExternalLoginSupplementalInputModel Input { get; set; } = new ExternalLoginSupplementalInputModel();

        public IActionResult OnGet()
        {
            ReturnUrl = WebUtility.UrlDecode(ReturnUrl ?? $"{GlobalConfiguration.BasePath}/");

            if (GlobalConfiguration.AllowSignup != true)
            {
                return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/RegistrationIsNotAllowed");
            }

            PopulateDefaults();

            return Page();
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

        public async Task<IActionResult> OnPostAsync()
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

            var info = await SignInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return NotifyOfError(_localizer["An error occurred retrieving user information from the external provider."]);
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email).EnsureNotNull();
            if (string.IsNullOrEmpty(email))
            {
                return NotifyOfError(_localizer["The email address was not supplied by the external provider."]);
            }

            var user = new IdentityUser { UserName = email, Email = email };
            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                return NotifyOfError(_localizer["An error occurred while creating the user."]);
            }

            result = await _userManager.AddLoginAsync(user, info);
            if (!result.Succeeded)
            {
                return NotifyOfError(_localizer["An error occurred while adding the login."]);
            }

            var membershipConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName(Constants.ConfigurationGroup.Membership);
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

            if (string.IsNullOrEmpty(ReturnUrl))
            {
                return Redirect($"{GlobalConfiguration.BasePath}/");
            }
            return Redirect(ReturnUrl);
        }
    }
}
