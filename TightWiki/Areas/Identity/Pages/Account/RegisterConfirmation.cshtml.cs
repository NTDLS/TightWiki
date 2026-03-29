// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Library;
using TightWiki.Library.Interfaces;
using TightWiki.Models;

namespace TightWiki.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModelBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWikiEmailSender _emailSender;
        private readonly ILogger<ITightEngine> _logger;

        public RegisterConfirmationModel(ILogger<ITightEngine> logger, SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager, IWikiEmailSender emailSender,
            ISharedLocalizationText localizer, TightWikiConfiguration wikiConfiguration)
                        : base(logger, signInManager, localizer, wikiConfiguration)
        {
            _logger = logger;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        public IActionResult OnGetAsync(string email, string returnUrl = null)
        {
            try
            {
                returnUrl = WebUtility.UrlDecode(returnUrl ?? $"{WikiConfiguration.BasePath}/");

                if (WikiConfiguration.AllowSignup != true)
                {
                    return Redirect($"{WikiConfiguration.BasePath}/Identity/Account/RegistrationIsNotAllowed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {Message}", ex.Message);
            }
            return Page();
        }
    }
}
