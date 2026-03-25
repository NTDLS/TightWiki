using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Library;
using TightWiki.Models;
using TightWiki.Models.ViewModels;
using TightWiki.Repository;

namespace TightWiki.Controllers
{
    [Area("Identity")]
    [Route("Identity/Account")]
    public class AccountController
        : WikiControllerBase<AccountController>
    {
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccountController(
            IHttpContextAccessor httpContextAccessor,
            ILogger<ITightEngine> logger,
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore, ISharedLocalizationText localizer)
            : base(logger, signInManager, userManager, localizer)
        {
            _httpContextAccessor = httpContextAccessor;
            _userStore = userStore;
            _emailStore = (IUserEmailStore<IdentityUser>)_userStore;
        }

        [HttpGet("ExternalLogin")]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLoginHttpGet(string provider, string? returnUrl = null)
        {
            try
            {
                returnUrl = WebUtility.UrlDecode(returnUrl ?? Url.Content("~/"));

                var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { area = "Identity", ReturnUrl = returnUrl });
                var properties = SignInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
                return Challenge(properties, provider);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error initiating external login");
                throw;
            }
        }

        [HttpPost("ExternalLogin")]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLoginHttpPost(string provider, string? returnUrl = null)
        {
            try
            {
                returnUrl = WebUtility.UrlDecode(returnUrl ?? Url.Content("~/"));

                var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { area = "Identity", ReturnUrl = returnUrl });
                var properties = SignInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
                return Challenge(properties, provider);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error initiating external login");
                throw;
            }
        }

        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            try
            {
                returnUrl = WebUtility.UrlDecode(returnUrl ?? Url.Content("~/"));

                //We use this model to display any errors that occur.
                var model = new ExternalLoginCallbackViewModel();

                if (remoteError != null)
                {
                    return NotifyOfError(Localize("Error from external provider: {0}", remoteError));
                }

                var info = await SignInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return NotifyOfError(Localize("Failed to get information from external provider"));
                }

                var user = await UserManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (user != null)
                {
                    // User exists, sign them in:
                    await SignInManager.SignInAsync(user, isPersistent: false);

                    if (await UsersRepository.GetBasicProfileByUserId(Guid.Parse(user.Id)) == null)
                    {
                        if (GlobalConfiguration.AllowSignup != true)
                        {
                            return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/RegistrationIsNotAllowed");
                        }

                        //User exists but does not have a profile.
                        //This means that the user has authenticated externally, but has yet to complete the signup process.
                        return RedirectToPage($"{GlobalConfiguration.BasePath}/Account/ExternalLoginSupplemental", new { ReturnUrl = returnUrl });
                    }

                    return Redirect(returnUrl);
                }
                else
                {
                    // If the user does not exist, check by email
                    var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                    if (string.IsNullOrEmpty(email))
                    {
                        return NotifyOfError(Localize("The email address was not supplied by the external provider."));
                    }

                    user = await UserManager.FindByEmailAsync(email);
                    if (user != null)
                    {
                        // User with this email exists but not linked with this external login, link them:
                        var result = await UserManager.AddLoginAsync(user, info);
                        if (!result.Succeeded)
                        {
                            return NotifyOfError(string.Join("<br />\r\n", result.Errors.Select(o => o.Description)));
                        }
                        await SignInManager.SignInAsync(user, isPersistent: false);

                        if (await UsersRepository.GetBasicProfileByUserId(Guid.Parse(user.Id)) == null)
                        {
                            if (GlobalConfiguration.AllowSignup != true)
                            {
                                return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/RegistrationIsNotAllowed");
                            }

                            //User exists but does not have a profile.
                            //This means that the user has authenticated externally, but has yet to complete the signup process.
                            return RedirectToPage($"{GlobalConfiguration.BasePath}/Account/ExternalLoginSupplemental", new { ReturnUrl = returnUrl });
                        }

                        return Redirect(returnUrl);
                    }
                    else
                    {
                        // If user with this email does not exist, then we need to create the user and profile.

                        if (GlobalConfiguration.AllowSignup != true)
                        {
                            return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/RegistrationIsNotAllowed");
                        }

                        return RedirectToPage($"{GlobalConfiguration.BasePath}/Account/ExternalLoginSupplemental", new { ReturnUrl = returnUrl });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during external login callback");
                throw;
            }
            finally
            {
                await UpdateUserCultureCookie();
            }
        }

        private async Task UpdateUserCultureCookie()
        {
            try
            {
                var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

                if (_httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated != true)
                {
                    return;
                }

                if (Guid.TryParse(userIdString, out var userId))
                {
                    var profile = await UsersRepository.GetBasicProfileByUserId(userId);
                    if (profile != null)
                    {
                        Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName,
                                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(profile.Language)),
                            new CookieOptions
                            {
                                Expires = DateTimeOffset.UtcNow.AddYears(1),
                                IsEssential = true,
                                SameSite = SameSiteMode.Lax,
                                Secure = true,
                                HttpOnly = false
                            }
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error updating user culture cookie");
                //throw; // We catch and log the error but do not rethrow it, as this is not critical and we don't want to disrupt the user experience.
            }
        }
    }
}
