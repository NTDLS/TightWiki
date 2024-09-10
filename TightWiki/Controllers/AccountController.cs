﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NTDLS.Helpers;
using System.Security.Claims;
using TightWiki.Models;
using TightWiki.Models.ViewModels;
using TightWiki.Repository;

namespace TightWiki.Controllers
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
                return NotifyOfError($"Error from external provider: {remoteError}");
            }

            var info = await SignInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return NotifyOfError($"Failed to get information from external provider");
            }

            var user = await UserManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (user != null)
            {
                // User exists, sign them in:
                await SignInManager.SignInAsync(user, isPersistent: false);

                if (UsersRepository.TryGetBasicProfileByUserId(Guid.Parse(user.Id), out _) == false)
                {
                    if (GlobalConfiguration.AllowSignup != true)
                    {
                        return Redirect("/Identity/Account/RegistrationIsNotAllowed");
                    }

                    //User exits but does not have a profile.
                    //This means that the user has authenticated externally, but has yet to complete the signup process.
                    return RedirectToPage("/Account/ExternalLoginSupplemental", new { ReturnUrl = returnUrl });
                }

                return LocalRedirect(returnUrl);
            }
            else
            {
                // If the user does not exist, check by email
                var email = info.Principal.FindFirstValue(ClaimTypes.Email).EnsureNotNull();
                if (string.IsNullOrEmpty(email))
                {
                    return NotifyOfError($"The email address was not supplied by the external provider.");
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
                    return LocalRedirect(returnUrl);
                }
                else
                {
                    // If user with this email does not exist, then we need to create the user and profile.

                    if (GlobalConfiguration.AllowSignup != true)
                    {
                        return Redirect("/Identity/Account/RegistrationIsNotAllowed");
                    }

                    return RedirectToPage("/Account/ExternalLoginSupplemental", new { ReturnUrl = returnUrl });
                }
            }
        }
    }
}
