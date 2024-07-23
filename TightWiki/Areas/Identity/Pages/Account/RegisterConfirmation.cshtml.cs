// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TightWiki.Configuration;
using TightWiki.Library.Interfaces;

namespace TightWiki.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModelBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWikiEmailSender _emailSender;

        public RegisterConfirmationModel(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, IWikiEmailSender emailSender)
                        : base(signInManager)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        public IActionResult OnGetAsync(string email, string returnUrl = null)
        {
            if (GlobalConfiguration.AllowSignup != true)
            {
                return Redirect("/Identity/Account/RegistrationIsNotAllowed");
            }

            return Page();
        }
    }
}
