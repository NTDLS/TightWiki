// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using System.Text;
using TightWiki.Models;
using TightWiki.Repository;

namespace TightWiki.Areas.Identity.Pages.Account
{
    public class ConfirmEmailModel : PageModelBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IStringLocalizer<ConfirmEmailModel> _localizer;
        private readonly ILogger<ConfirmEmailModel> _logger;

        public ConfirmEmailModel(
            ILogger<ConfirmEmailModel> logger,
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            IStringLocalizer<ConfirmEmailModel> localizer)
            : base(signInManager)
        {
            _logger = logger;
            _userManager = userManager;
            _localizer = localizer;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }
        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            try
            {
                if (userId == null || code == null)
                {
                    return Redirect($"{GlobalConfiguration.BasePath}/");
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound($"Unable to load user with ID '{userId}'.");
                }

                code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
                var result = await _userManager.ConfirmEmailAsync(user, code);
                StatusMessage = result.Succeeded ? _localizer["Thank you for confirming your email."] : _localizer["Error confirming your email."];

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
