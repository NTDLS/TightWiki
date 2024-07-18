using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TightWiki.Configuration;
using TightWiki.Controllers;
using TightWiki.Library;
using TightWiki.Models.ViewModels;
using TightWiki.Repository;

namespace TightWiki.Site.Controllers
{
    [Area("Identity")]
    [Route("Identity/Account")]
    public class AccountController : WikiControllerBase
    {
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;

        public AccountController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, IUserStore<IdentityUser> userStore)
            : base(signInManager, userManager)
        {
            _userStore = userStore;
            _emailStore = (IUserEmailStore<IdentityUser>)_userStore;
        }

        [HttpGet("ExternalLogin")]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLoginHttpGet(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { area = "Identity", ReturnUrl = returnUrl });
            var properties = SignInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpPost("ExternalLogin")]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLoginHttpPost(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { area = "Identity", ReturnUrl = returnUrl });
            var properties = SignInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            //We use this model to display any errors that occur.
            var model = new ExternalLoginCallbackViewModel();

            returnUrl ??= Url.Content("~/");
            if (remoteError != null)
            {
                model.ErrorMessage = $"Error from external provider: {remoteError}";
                return View(model);
            }

            var info = await SignInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                model.ErrorMessage = $"Failed to get information from external provider";
                return View(model);
            }

            var user = await UserManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (user != null)
            {
                // User exists, sign them in:
                await SignInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(returnUrl);
            }
            else
            {
                // If the user does not exist, check by email
                var email = info.Principal.FindFirstValue(ClaimTypes.Email).EnsureNotNull();
                if (string.IsNullOrEmpty(email))
                {
                    model.ErrorMessage = $"The email address was not supplied by the external provider.";
                    return View(model);
                }

                user = await UserManager.FindByEmailAsync(email);
                if (user == null)
                {
                    // If user with this email does not exist, create a new user:

                    if (GlobalConfiguration.AllowSignup != true)
                    {
                        return Redirect("/Identity/Account/RegistrationIsNotAllowed");
                    }

                    user = new IdentityUser { UserName = email, Email = email };
                    var result = await UserManager.CreateAsync(user);
                    if (!result.Succeeded)
                    {
                        model.ErrorMessage = string.Join("<br />\r\n", result.Errors.Select(o => o.Description));
                        return View(model);
                    }

                    result = await UserManager.AddLoginAsync(user, info);
                    if (!result.Succeeded)
                    {
                        model.ErrorMessage = string.Join("<br />\r\n", result.Errors.Select(o => o.Description));
                        return View(model);
                    }

                    UsersRepository.CreateProfile(Guid.Parse(user.Id));

                    var membershipConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Membership");
                    var claimsToAdd = new List<Claim>
                        {
                            new (ClaimTypes.Role, membershipConfig.Value<string>("Default Signup Role").EnsureNotNull()),
                            new ("timezone", membershipConfig.Value<string>("Default TimeZone").EnsureNotNull()),
                            new (ClaimTypes.Country, membershipConfig.Value<string>("Default Country").EnsureNotNull()),
                            new ("language", membershipConfig.Value<string>("Default Language").EnsureNotNull()),
                        };

                    SecurityRepository.UpsertUserClaims(UserManager, user, claimsToAdd);

                    await SignInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl ?? Url.Content("~/"));
                }
                else
                {
                    // User with this email exists but not linked with this external login, link them:
                    var result = await UserManager.AddLoginAsync(user, info);
                    if (!result.Succeeded)
                    {
                        model.ErrorMessage = string.Join("<br />\r\n", result.Errors.Select(o => o.Description));
                        return View(model);
                    }
                    await SignInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }
            }
        }
    }
}
