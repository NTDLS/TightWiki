using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.ViewModels;

namespace TightWiki.Controllers
{
    [Area("Identity")]
    [Route("Identity/Account")]
    public class AccountController(
            IHttpContextAccessor httpContextAccessor,
            ILogger<ITwEngine> logger,
            ITwSharedLocalizationText localizer,
            ITwUsersRepository usersRepository,
            SignInManager<IdentityUser> signInManager,
            TwConfiguration wikiConfiguration,
            UserManager<IdentityUser> userManager,
            ITwDatabaseManager databaseManager
        )
        : TwController<AccountController>(logger, signInManager, userManager, localizer, wikiConfiguration, databaseManager)
    {
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

                    if (await usersRepository.GetBasicProfileByUserId(Guid.Parse(user.Id)) == null)
                    {
                        if (WikiConfiguration.AllowSignup != true)
                        {
                            return Redirect($"{WikiConfiguration.BasePath}/Identity/Account/RegistrationIsNotAllowed");
                        }

                        //User exists but does not have a profile.
                        //This means that the user has authenticated externally, but has yet to complete the signup process.
                        return RedirectToPage($"{WikiConfiguration.BasePath}/Account/ExternalLoginSupplemental", new { ReturnUrl = returnUrl });
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

                        if (await usersRepository.GetBasicProfileByUserId(Guid.Parse(user.Id)) == null)
                        {
                            if (WikiConfiguration.AllowSignup != true)
                            {
                                return Redirect($"{WikiConfiguration.BasePath}/Identity/Account/RegistrationIsNotAllowed");
                            }

                            //User exists but does not have a profile.
                            //This means that the user has authenticated externally, but has yet to complete the signup process.
                            return RedirectToPage($"{WikiConfiguration.BasePath}/Account/ExternalLoginSupplemental", new { ReturnUrl = returnUrl });
                        }

                        return Redirect(returnUrl);
                    }
                    else
                    {
                        // If user with this email does not exist, then we need to create the user and profile.

                        if (WikiConfiguration.AllowSignup != true)
                        {
                            return Redirect($"{WikiConfiguration.BasePath}/Identity/Account/RegistrationIsNotAllowed");
                        }

                        return RedirectToPage($"{WikiConfiguration.BasePath}/Account/ExternalLoginSupplemental", new { ReturnUrl = returnUrl });
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
                var userIdString = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

                if (httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated != true)
                {
                    return;
                }

                if (Guid.TryParse(userIdString, out var userId))
                {
                    var profile = await usersRepository.GetBasicProfileByUserId(userId);
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
