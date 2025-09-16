// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Net;
using TightWiki.Extensions;
using TightWiki.Library;
using TightWiki.Models;
using TightWiki.Repository;
using TightWiki.Security;
using TightWiki.Static;

namespace TightWiki.Areas.Identity.Pages.Account
{
    public class LoginInputModel
    {
        [Required(ErrorMessageResourceName = "RequiredAttribute_ValidationError", ErrorMessageResourceType = typeof(Models.Resources.ValTexts))]
        public string Username { get; set; }

        [Required(ErrorMessageResourceName = "RequiredAttribute_ValidationError", ErrorMessageResourceType = typeof(Models.Resources.ValTexts))]
        [DataType(DataType.Password, ErrorMessageResourceName = "DataTypeAttribute_EmptyDataTypeString", ErrorMessageResourceType = typeof(Models.Resources.ValTexts))]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public class LoginModel : PageModelBase
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly IStringLocalizer<ConfirmEmailModel> _localizer;

        public LoginModel(ILogger<LoginModel> logger, SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, IStringLocalizer<ConfirmEmailModel> localizer)
                        : base(signInManager)
        {
            _localizer = localizer;
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public LoginInputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        public async Task OnGetAsync(string returnUrl = null)
        {
            try
            {
                ReturnUrl = WebUtility.UrlDecode(returnUrl ?? $"{GlobalConfiguration.BasePath}/");

                if (!string.IsNullOrEmpty(ErrorMessage))
                {
                    ModelState.AddModelError(string.Empty, ErrorMessage);
                }

                // Clear the existing external cookie to ensure a clean login process
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

                ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {Message}", ex.Message);
                ExceptionRepository.InsertException(ex, "LDAP authentication error");
            }
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            try
            {
                var ldapAuthenticationConfiguration = ConfigurationRepository.GetConfigurationEntryValuesByGroupName(Constants.ConfigurationGroup.LDAPAuthentication);

                ReturnUrl = WebUtility.UrlDecode(returnUrl ?? $"{GlobalConfiguration.BasePath}/");

                ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

                if (ModelState.IsValid)
                {
                    // This doesn't count login failures towards account lockout
                    // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                    var result = await _signInManager.PasswordSignInAsync(Input.Username, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User logged in.");
                        return Redirect(ReturnUrl);
                    }
                    if (result.RequiresTwoFactor)
                    {
                        return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/LoginWith2fa?ReturnUrl={WebUtility.UrlEncode(ReturnUrl)}&RememberMe={Input.RememberMe}");
                    }
                    if (result.IsLockedOut)
                    {
                        _logger.LogWarning("User account locked out.");
                        return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/Lockout");
                    }
                    else
                    {
                        #region Fallback to LDAP authentication if enabled.

                        if (GlobalConfiguration.EnableLDAPAuthentication)
                        {
                            if (LDAPUtility.LdapCredentialChallenge(ldapAuthenticationConfiguration, StaticLocalizer.Localizer,
                                Input.Username, Input.Password, out var samAccountName, out var objectGuid))
                            {
                                //We successfully authenticated against LDAP.

                                var loginInfo = new UserLoginInfo("LDAP", objectGuid.ToString(), "Active Directory");

                                var foundUser = await _userManager.FindByLoginAsync(loginInfo.LoginProvider, loginInfo.ProviderKey);

                                if (foundUser != null && UsersRepository.TryGetBasicProfileByUserId(Guid.Parse(foundUser.Id), out _))
                                {
                                    await SignInManager.SignInAsync(foundUser, Input.RememberMe);
                                    return Redirect(returnUrl);
                                }
                                else
                                {
                                    if (GlobalConfiguration.AllowSignup != true)
                                    {
                                        return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/RegistrationIsNotAllowed");
                                    }

                                    var newUser = new IdentityUser()
                                    {
                                        UserName = samAccountName
                                    };

                                    //If the user does not already exist, create them:
                                    if (foundUser == null)
                                    {
                                        var createResult = await _userManager.CreateAsync(newUser);

                                        if (createResult.Succeeded)
                                        {
                                            _logger.LogInformation(_localizer["User created a new account with LDAP."]);

                                            // Link the stable AD identity to this user
                                            var addLogin = await _userManager.AddLoginAsync(newUser, loginInfo);
                                            if (!addLogin.Succeeded)
                                            {
                                                throw new Exception(_localizer["Failed to add login info for LDAP stub account: {0}."]
                                                    .Format(string.Join("; ", addLogin.Errors.Select(e => $"{e.Code}:{e.Description}"))));
                                            }

                                            foundUser = await _userManager.FindByNameAsync(samAccountName);
                                            if (foundUser == null)
                                            {
                                                throw new Exception(_localizer["Failed to locate the user account for the LDAP credential."]);
                                            }

                                        }
                                        else
                                        {
                                            throw new Exception(_localizer["Failed to create stub account for the LDAP credential: {0}."]
                                                .Format(string.Join("; ", createResult.Errors.Select(e => $"{e.Code}:{e.Description}"))));
                                        }
                                    }

                                    // Check if the user has a profile, if not, redirect to the supplemental info page.
                                    if (UsersRepository.TryGetBasicProfileByUserId(Guid.Parse(foundUser.Id), out _) == false)
                                    {
                                        if (GlobalConfiguration.AllowSignup != true)
                                        {
                                            return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/RegistrationIsNotAllowed");
                                        }

                                        //User exits but does not have a profile.
                                        //This means that the user has authenticated with LDSP, but has yet to complete the signup process.
                                        return RedirectToPage($"{GlobalConfiguration.BasePath}/Account/LdapLoginSupplemental", new { UserId = foundUser.Id, ReturnUrl = returnUrl });
                                    }
                                }

                                return Redirect(returnUrl);
                            }
                        }

                        #endregion

                        ModelState.AddModelError(string.Empty, _localizer["Invalid login attempt."]);
                        return Page();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {Message}", ex.Message);
                ExceptionRepository.InsertException(ex);
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
