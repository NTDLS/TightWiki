// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.DirectoryServices.Protocols;
using System.Net;
using TightWiki.Models;
using TightWiki.Repository;

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

        public LoginModel(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, ILogger<LoginModel> logger)
                        : base(signInManager)
        {
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
            ReturnUrl = WebUtility.UrlDecode(returnUrl ?? $"{GlobalConfiguration.BasePath}/");

            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public static string GetLdapSearchBase(string domainName)
        {
            // domainName like "corp.example.com"
            var parts = domainName.Split('.');
            return string.Join(",", parts.Select(p => $"DC={p}"));
        }

        static string EscapeLdapFilterValue(string value)
        {
            // RFC 4515 escaping: \ * ( ) and NUL
            return value
                .Replace(@"\", @"\5c")
                .Replace("*", @"\2a")
                .Replace("(", @"\28")
                .Replace(")", @"\29")
                .Replace("\0", @"\00");
        }

        static (string domain, string user, string upn) ParseLogin(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException(nameof(input));

            //DOMAIN\user
            var slash = input.IndexOf('\\');
            if (slash >= 0)
            {
                var domain = input.Substring(0, slash);
                var user = input.Substring(slash + 1);
                return (domain, user, null);
            }

            //user@domain.tld (UPN)
            var at = input.IndexOf('@');
            if (at >= 0)
            {
                return (null, input.Substring(0, at), input); // upn = full input
            }

            //plain sAMAccountName
            return (null, input, null);
        }

        public bool TestLdapCredential(string username, string password, [NotNullWhen(true)] out string samAccountName)
        {
            try
            {
                using var conn = new LdapConnection(GlobalConfiguration.LDAPFullyQualifiedDomain);
                conn.SessionOptions.SecureSocketLayer = GlobalConfiguration.LDAPUseSecureSocketLayer;
                conn.AuthType = AuthType.Basic;

                conn.Bind(new NetworkCredential(username, password));

                var ldapSearchBase = GetLdapSearchBase(GlobalConfiguration.LDAPFullyQualifiedDomain);

                var (domain, sam, upn) = ParseLogin(username);

                //Always try sAMAccountName=sam
                var clauses = new List<string> { $"(sAMAccountName={EscapeLdapFilterValue(sam)})" };

                //If we actually received a UPN, also try userPrincipalName
                if (!string.IsNullOrEmpty(upn))
                    clauses.Add($"(userPrincipalName={EscapeLdapFilterValue(upn)})");

                var knownUpnSuffix = GlobalConfiguration.LDAPFullyQualifiedDomain;
                if (domain != null && !string.IsNullOrEmpty(knownUpnSuffix))
                    clauses.Add($"(userPrincipalName={EscapeLdapFilterValue($"{sam}@{knownUpnSuffix}")})");

                var filter = $"(|{string.Join("", clauses)})";

                var request = new SearchRequest(
                    ldapSearchBase,
                    filter,
                    SearchScope.Subtree,
                    "sAMAccountName", "userPrincipalName", "cn", "mail", "distinguishedName"
                );

                var response = (SearchResponse)conn.SendRequest(request);

                if (response.Entries.Count > 0)
                {
                    var entry = response.Entries[0];
                    samAccountName = entry.Attributes["sAMAccountName"][0].ToString();
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LDAP authentication error");
            }

            samAccountName = null;
            return false;

        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
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
                        if (TestLdapCredential(Input.Username, Input.Password, out var samAccountName))
                        {
                            //We successfully authenticated against LDAP.
                            var newUser = new IdentityUser()
                            {
                                UserName = samAccountName
                            };

                            var foundUser = await _userManager.FindByNameAsync(samAccountName);
                            if (foundUser != null)
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

                                //If the user does not already exist, create them:
                                var createResult = await _userManager.CreateAsync(newUser);
                                if (createResult.Succeeded)
                                {
                                    _logger.LogInformation("User created a new account with LDAP.");

                                    foundUser = await _userManager.FindByNameAsync(samAccountName);
                                    if (foundUser == null)
                                    {
                                        return NotifyOfError("Failed to locate the user account for the LDAP credential.");
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

                                    return Redirect(returnUrl);
                                }
                                else
                                {
                                    return NotifyOfError("Failed to create the stub account for the LDAP credential.");
                                }
                            }
                        }
                    }

                    #endregion

                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
