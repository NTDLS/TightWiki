// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using TightWiki.Library.Interfaces;
using TightWiki.Models;
using TightWiki.Repository;

namespace TightWiki.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModelBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWikiEmailSender _emailSender;
        private readonly ILogger<RegisterConfirmationModel> _logger;

        public RegisterConfirmationModel(ILogger<RegisterConfirmationModel> logger, SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, IWikiEmailSender emailSender)
                        : base(signInManager)
        {
            _logger = logger;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        public IActionResult OnGetAsync(string email, string returnUrl = null)
        {
            try
            {
                returnUrl = WebUtility.UrlDecode(returnUrl ?? $"{GlobalConfiguration.BasePath}/");

                if (GlobalConfiguration.AllowSignup != true)
                {
                    return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/RegistrationIsNotAllowed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {Message}", ex.Message);
                ExceptionRepository.InsertException(ex);
            }
            return Page();
        }
    }
}
