// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Models;

namespace TightWiki.Areas.Identity.Pages.Account
{
    public class LogoutModel
        : PageModelBase
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<ITightEngine> _logger;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly UserManager<IdentityUser> _userManager;

        public LogoutModel(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, ILogger<ITightEngine> logger, IAuthenticationSchemeProvider schemeProvider)
            : base(logger, signInManager)
        {
            _schemeProvider = schemeProvider;
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            try
            {
                returnUrl = WebUtility.UrlDecode(returnUrl ?? $"{GlobalConfiguration.BasePath}/");

                await _signInManager.SignOutAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {Message}", ex.Message);
            }

            _logger.LogInformation("User logged out.");
            if (returnUrl != null)
            {
                return Redirect(returnUrl);
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
