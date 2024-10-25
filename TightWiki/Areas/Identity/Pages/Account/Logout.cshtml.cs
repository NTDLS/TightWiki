// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace TightWiki.Areas.Identity.Pages.Account
{
    public class LogoutModel : PageModelBase
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly UserManager<IdentityUser> _userManager;

        public LogoutModel(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, ILogger<LogoutModel> logger, IAuthenticationSchemeProvider schemeProvider)
                        : base(signInManager)
        {
            _schemeProvider = schemeProvider;
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            await _signInManager.SignOutAsync();

            /*
            var allSchemes = await _schemeProvider.GetAllSchemesAsync();
            foreach (var scheme in allSchemes)
            {
                try
                {
                    await HttpContext.SignOutAsync(scheme.Name);
                }
                catch
                {
                }
            }
            */

            /*
            // Explicitly delete the cookie with the correct path.
            Response.Cookies.Delete(".AspNetCore.Identity.Application", new CookieOptions
            {
                Path = "/TightWiki", // Must match the cookie's path.
                HttpOnly = true,
                Secure = true // Use this if the cookie is secure.
            });
            */

            /*
            if (HttpContext.Request.Cookies.Count > 0)
            {
                var siteCookies = HttpContext.Request.Cookies
                    .Where(c => c.Key.Contains(".AspNetCore.")
                        || c.Key.Contains("Microsoft.Authentication"));
                foreach (var cookie in siteCookies)
                {
                    Response.Cookies.Delete(cookie.Key);
                }
            }

            await HttpContext.SignOutAsync(
                user.AuthenticationScheme);

            HttpContext.Session.Clear();

            await _signInManager.SignOutAsync();
            await HttpContext.SignOutAsync();

            var allSchemes = await _schemeProvider.GetAllSchemesAsync();
            foreach (var scheme in allSchemes)
            {
                try
                {
                    await HttpContext.SignOutAsync(scheme.Name);
                }
                catch
                {
                }
            }

            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }
            */

            _logger.LogInformation("User logged out.");
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                // This needs to be a redirect so that the browser performs a new
                // request and the identity for the user gets updated.
                return RedirectToPage();
            }
        }
    }
}
