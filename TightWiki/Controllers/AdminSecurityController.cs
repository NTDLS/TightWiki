using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using NTDLS.Helpers;
using System.Security.Claims;
using TightWiki.Caching;
using TightWiki.Library;
using TightWiki.Models;
using TightWiki.Models.DataModels;
using TightWiki.Models.Requests;
using TightWiki.Models.ViewModels.AdminSecurity;
using TightWiki.Models.ViewModels.Shared;
using TightWiki.Models.ViewModels.Utility;
using TightWiki.Repository;
using Constants = TightWiki.Library.Constants;

namespace TightWiki.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class AdminSecurityController(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IStringLocalizer<AdminSecurityController> localizer)
        : WikiControllerBase<AdminSecurityController>(signInManager, userManager, localizer)
    {
        #region Roles.

        [Authorize]
        [HttpPost("DeleteRole/{roleId:int}")]
        public ActionResult DeleteRole(ConfirmActionViewModel model, int roleId)
        {
            SessionState.RequireAdminPermission();

            if (model.UserSelection == true)
            {
                UsersRepository.DeleteRole(roleId);
                return NotifyOfSuccess(Localize("The specified role has been deleted."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        [Authorize]
        [HttpPost("AddRoleMember")]
        public IActionResult AddRoleMember([FromBody] AddRoleMemberRequest request)
        {
            try
            {
                SessionState.RequireAdminPermission();

                AddRoleMemberResult? result = null;

                bool alreadyExists = UsersRepository.IsAccountAMemberOfRole(request.UserId, request.RoleId, false);
                if (!alreadyExists)
                {
                    result = UsersRepository.AddRoleMember(request.UserId, request.RoleId);
                }

                return Ok(new { success = true, alreadyExists = alreadyExists, membership = result, message = (string?)null });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("RemoveRoleMember/{roleId:int}/{userId:Guid}")]
        public IActionResult RemoveRoleMember(int roleId, Guid userId)
        {
            try
            {
                SessionState.RequireAdminPermission();
                UsersRepository.RemoveRoleMember(roleId, userId);
                return Ok(new { success = true, message = (string?)null });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("RemoveRolePermission/{id:int}")]
        public IActionResult RemoveRolePermission(int id)
        {
            try
            {
                SessionState.RequireAdminPermission();
                UsersRepository.RemoveRolePermission(id);
                return Ok(new { success = true, message = (string?)null });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("AddRolePermission")]
        public IActionResult AddRolePermission([FromBody] AddRolePermissionRequest request)
        {
            try
            {
                SessionState.RequireAdminPermission();

                InsertRolePermissionResult? result = null;

                bool alreadyExists = UsersRepository.IsRolePermissionDefined(
                    request.RoleId, request.PermissionId, request.PermissionDispositionId, request.Namespace, request.PageId, false);
                if (!alreadyExists)
                {
                    result = UsersRepository.InsertRolePermission(
                        request.RoleId, request.PermissionId, request.PermissionDispositionId, request.Namespace, request.PageId);
                }

                return Ok(new { success = true, alreadyExists = alreadyExists, permission = result, message = (string?)null });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("AddRole")]
        public ActionResult AddRole()
        {
            SessionState.RequireAdminPermission();
            SessionState.Page.Name = Localize("Add Role");

            return View(new AddRoleViewModel());
        }

        [Authorize]
        [HttpPost("AddRole")]
        public ActionResult AddRole(AddRoleViewModel model)
        {
            SessionState.RequireAdminPermission();
            SessionState.Page.Name = Localize("Add Role");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", Localize("Role name is required."));
                return View(model);
            }

            if (UsersRepository.DoesRoleExist(model.Name))
            {
                ModelState.AddModelError("Name", Localize("Role name is already in use."));
                return View(model);
            }

            UsersRepository.InsertRole(model.Name, model.Description);

            return Redirect($"{GlobalConfiguration.BasePath}/AdminSecurity/Roles");
        }

        [Authorize]
        [HttpGet("Role/{navigation}")]
        public ActionResult Role(string navigation)
        {
            SessionState.RequireAdminPermission();
            SessionState.Page.Name = Localize("Roles");

            navigation = Navigation.Clean(navigation);

            var role = UsersRepository.GetRoleByName(navigation);

            var model = new RoleViewModel()
            {
                IsBuiltIn = role.IsBuiltIn,
                Id = role.Id,
                Name = role.Name,
                Members = UsersRepository.GetRoleMembersPaged(role.Id,
                    GetQueryValue("usersPage", 1), GetQueryValue("usersOrderBy"), GetQueryValue("usersOrderByDirection")),
                AssignedPermissions = UsersRepository.GetRolePermissionsForDisplay(role.Id,
                    GetQueryValue("rolesPage", 1), GetQueryValue("rolesOrderBy"), GetQueryValue("rolesOrderByDirection")),

                PermissionDispositions = UsersRepository.GetAllPermissionDispositions(),
                Permissions = UsersRepository.GetAllPermissions()
            };

            model.PaginationPageCount = (model.Members.FirstOrDefault()?.PaginationPageCount ?? 0);

            return View(model);
        }

        [Authorize]
        [HttpGet("Roles")]
        public ActionResult Roles()
        {
            SessionState.RequireAdminPermission();

            var orderBy = GetQueryValue("OrderBy");
            var orderByDirection = GetQueryValue("OrderByDirection");

            var model = new RolesViewModel()
            {
                Roles = UsersRepository.GetAllRoles(orderBy, orderByDirection)
            };

            return View(model);
        }

        #endregion

        #region Accounts

        [Authorize]
        [HttpGet("Account/{navigation}")]
        public ActionResult Account(string navigation)
        {
            SessionState.RequireAdminPermission();

            var model = new Models.ViewModels.AdminSecurity.AccountProfileViewModel()
            {
                AccountProfile = Models.ViewModels.AdminSecurity.AccountProfileAccountViewModel.FromDataModel(
                    UsersRepository.GetAccountProfileByNavigation(Navigation.Clean(navigation))),
                Credential = new CredentialViewModel(),
                Themes = ConfigurationRepository.GetAllThemes(),
                TimeZones = TimeZoneItem.GetAll(),
                Countries = CountryItem.GetAll(),
                Languages = LanguageItem.GetAll(),
                Roles = UsersRepository.GetAllRoles()
            };

            model.AccountProfile.CreatedDate = SessionState.LocalizeDateTime(model.AccountProfile.CreatedDate);
            model.AccountProfile.ModifiedDate = SessionState.LocalizeDateTime(model.AccountProfile.ModifiedDate);

            return View(model);
        }

        /// <summary>
        /// Save user profile.
        /// </summary>
        [Authorize]
        [HttpPost("Account/{navigation}")]
        public ActionResult Account(string navigation, Models.ViewModels.AdminSecurity.AccountProfileViewModel model)
        {
            SessionState.RequireAdminPermission();

            model.Themes = ConfigurationRepository.GetAllThemes();
            model.TimeZones = TimeZoneItem.GetAll();
            model.Countries = CountryItem.GetAll();
            model.Languages = LanguageItem.GetAll();
            model.Roles = UsersRepository.GetAllRoles();
            model.AccountProfile.Navigation = NamespaceNavigation.CleanAndValidate(model.AccountProfile.AccountName.ToLowerInvariant());

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = UserManager.FindByIdAsync(model.AccountProfile.UserId.ToString()).Result.EnsureNotNull();

            if (model.Credential.Password != CredentialViewModel.NOTSET && model.Credential.Password == model.Credential.ComparePassword)
            {
                try
                {
                    var token = UserManager.GeneratePasswordResetTokenAsync(user).Result.EnsureNotNull();
                    var result = UserManager.ResetPasswordAsync(user, token, model.Credential.Password).Result.EnsureNotNull();
                    if (!result.Succeeded)
                    {
                        throw new Exception(string.Join("<br />\r\n", result.Errors.Select(o => o.Description)));
                    }

                    if (model.AccountProfile.AccountName.Equals(Constants.DEFAULTACCOUNT, StringComparison.InvariantCultureIgnoreCase))
                    {
                        UsersRepository.SetAdminPasswordIsChanged();
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Credential.Password", ex.Message);
                    return View(model);
                }
            }

            var profile = UsersRepository.GetAccountProfileByUserId(model.AccountProfile.UserId);
            if (!profile.Navigation.Equals(model.AccountProfile.Navigation, StringComparison.InvariantCultureIgnoreCase))
            {
                if (UsersRepository.DoesProfileAccountExist(model.AccountProfile.AccountName))
                {
                    ModelState.AddModelError("AccountProfile.AccountName", Localize("Account name is already in use."));
                    return View(model);
                }
            }

            if (!profile.EmailAddress.Equals(model.AccountProfile.EmailAddress, StringComparison.InvariantCultureIgnoreCase))
            {
                if (UsersRepository.DoesEmailAddressExist(model.AccountProfile.EmailAddress))
                {
                    ModelState.AddModelError("AccountProfile.EmailAddress", Localize("Email address is already in use."));
                    return View(model);
                }
            }

            var file = Request.Form.Files["Avatar"];
            if (file != null && file.Length > 0)
            {
                if (GlobalConfiguration.AllowableImageTypes.Contains(file.ContentType.ToLowerInvariant()) == false)
                {
                    model.ErrorMessage += Localize("Could not save the attached image, type not allowed.") + "\r\n";
                }
                else if (file.Length > GlobalConfiguration.MaxAvatarFileSize)
                {
                    model.ErrorMessage += Localize("Could not save the attached image, too large.") + "\r\n";
                }
                else
                {
                    try
                    {
                        var imageBytes = Utility.ConvertHttpFileToBytes(file);
                        var image = SixLabors.ImageSharp.Image.Load(new MemoryStream(imageBytes));
                        UsersRepository.UpdateProfileAvatar(profile.UserId, imageBytes, file.ContentType.ToLowerInvariant());
                    }
                    catch
                    {
                        model.ErrorMessage += Localize("Could not save the attached image.") + "\r\n";
                    }
                }
            }

            profile.AccountName = model.AccountProfile.AccountName;
            profile.Navigation = NamespaceNavigation.CleanAndValidate(model.AccountProfile.AccountName);
            profile.Biography = model.AccountProfile.Biography;
            profile.ModifiedDate = DateTime.UtcNow;
            UsersRepository.UpdateProfile(profile);

            var claims = new List<Claim>
                    {
                        new ("timezone", model.AccountProfile.TimeZone),
                        new (ClaimTypes.Country, model.AccountProfile.Country),
                        new ("language", model.AccountProfile.Language),
                        new ("firstname", model.AccountProfile.FirstName ?? ""),
                        new ("lastname", model.AccountProfile.LastName ?? ""),
                        new ("theme", model.AccountProfile.Theme ?? ""),
                    };
            SecurityRepository.UpsertUserClaims(UserManager, user, claims);

            //If we are changing the currently logged in user, then make sure we take some extra actions so we can see the changes immediately.
            if (SessionState.Profile?.UserId == model.AccountProfile.UserId)
            {
                SignInManager.RefreshSignInAsync(user);

                WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.User, [profile.Navigation]));
                WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.User, [profile.UserId]));

                //This is not 100% necessary, I just want to prevent the user from needing to refresh to view the new theme.
                SessionState.UserTheme = ConfigurationRepository.GetAllThemes().SingleOrDefault(o => o.Name == model.AccountProfile.Theme) ?? GlobalConfiguration.SystemTheme;
            }

            //Allow the administrator to confirm/unconfirm the email address.
            bool emailConfirmChanged = profile.EmailConfirmed != model.AccountProfile.EmailConfirmed;
            if (emailConfirmChanged)
            {
                user.EmailConfirmed = model.AccountProfile.EmailConfirmed;
                var updateResult = UserManager.UpdateAsync(user).Result;
                if (!updateResult.Succeeded)
                {
                    throw new Exception(string.Join("<br />\r\n", updateResult.Errors.Select(o => o.Description)));
                }
            }

            if (!profile.EmailAddress.Equals(model.AccountProfile.EmailAddress, StringComparison.InvariantCultureIgnoreCase))
            {
                bool wasEmailAlreadyConfirmed = user.EmailConfirmed;

                var setEmailResult = UserManager.SetEmailAsync(user, model.AccountProfile.EmailAddress).Result;
                if (!setEmailResult.Succeeded)
                {
                    throw new Exception(string.Join("<br />\r\n", setEmailResult.Errors.Select(o => o.Description)));
                }

                var setUserNameResult = UserManager.SetUserNameAsync(user, model.AccountProfile.EmailAddress).Result;
                if (!setUserNameResult.Succeeded)
                {
                    throw new Exception(string.Join("<br />\r\n", setUserNameResult.Errors.Select(o => o.Description)));
                }

                //If the email address was already confirmed, just keep the status. Afterall, this is an admin making the change.
                if (wasEmailAlreadyConfirmed && emailConfirmChanged == false)
                {
                    user.EmailConfirmed = true;
                    var updateResult = UserManager.UpdateAsync(user).Result;
                    if (!updateResult.Succeeded)
                    {
                        throw new Exception(string.Join("<br />\r\n", updateResult.Errors.Select(o => o.Description)));
                    }
                }
            }

            model.SuccessMessage = Localize("Your profile has been saved successfully!");

            return View(model);
        }

        [Authorize]
        [HttpGet("AddAccount")]
        public ActionResult AddAccount()
        {
            SessionState.RequireAdminPermission();

            var membershipConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName(Constants.ConfigurationGroup.Membership);
            var defaultSignupRole = membershipConfig.Value<string>("Default Signup Role").EnsureNotNull();
            var customizationConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName(Constants.ConfigurationGroup.Customization);

            var model = new Models.ViewModels.AdminSecurity.AccountProfileViewModel()
            {
                AccountProfile = new Models.ViewModels.AdminSecurity.AccountProfileAccountViewModel
                {
                    AccountName = string.Empty,
                    Country = customizationConfig.Value<string>("Default Country", string.Empty),
                    TimeZone = customizationConfig.Value<string>("Default TimeZone", string.Empty),
                    Language = customizationConfig.Value<string>("Default Language", string.Empty)
                    //Role = defaultSignupRole
                },
                Themes = ConfigurationRepository.GetAllThemes(),
                Credential = new CredentialViewModel(),
                TimeZones = TimeZoneItem.GetAll(),
                Countries = CountryItem.GetAll(),
                Languages = LanguageItem.GetAll(),
                Roles = UsersRepository.GetAllRoles()
            };

            return View(model);
        }

        /// <summary>
        /// Create a new user profile.
        /// </summary>
        [Authorize]
        [HttpPost("AddAccount")]
        public ActionResult AddAccount(Models.ViewModels.AdminSecurity.AccountProfileViewModel model)
        {
            SessionState.RequireAdminPermission();

            model.Themes = ConfigurationRepository.GetAllThemes();
            model.TimeZones = TimeZoneItem.GetAll();
            model.Countries = CountryItem.GetAll();
            model.Languages = LanguageItem.GetAll();
            model.Roles = UsersRepository.GetAllRoles();
            model.AccountProfile.Navigation = NamespaceNavigation.CleanAndValidate(model.AccountProfile.AccountName?.ToLowerInvariant());

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.AccountProfile.AccountName))
            {
                ModelState.AddModelError("AccountProfile.AccountName", Localize("Account name is required."));
                return View(model);
            }

            if (UsersRepository.DoesProfileAccountExist(model.AccountProfile.AccountName))
            {
                ModelState.AddModelError("AccountProfile.AccountName", Localize("Account name is already in use."));
                return View(model);
            }

            if (UsersRepository.DoesEmailAddressExist(model.AccountProfile.EmailAddress))
            {
                ModelState.AddModelError("AccountProfile.EmailAddress", Localize("Email address is already in use."));
                return View(model);
            }

            Guid? userId;

            try
            {
                //Define the new user:
                var identityUser = new IdentityUser(model.AccountProfile.EmailAddress)
                {
                    Email = model.AccountProfile.EmailAddress,
                    EmailConfirmed = true
                };

                //Create the new user:
                var creationResult = UserManager.CreateAsync(identityUser, model.Credential.Password).Result;
                if (!creationResult.Succeeded)
                {
                    model.ErrorMessage = string.Join("<br />\r\n", creationResult.Errors.Select(o => o.Description));
                    return View(model);
                }
                identityUser = UserManager.FindByEmailAsync(model.AccountProfile.EmailAddress).Result.EnsureNotNull();

                userId = Guid.Parse(identityUser.Id);

                //Insert the claims.
                var claims = new List<Claim>
                    {
                        new ("timezone", model.AccountProfile.TimeZone),
                        new (ClaimTypes.Country, model.AccountProfile.Country),
                        new ("language", model.AccountProfile.Language),
                        new ("firstname", model.AccountProfile.FirstName ?? ""),
                        new ("lastname", model.AccountProfile.LastName ?? ""),
                        new ("theme", model.AccountProfile.Theme ?? ""),
                    };
                SecurityRepository.UpsertUserClaims(UserManager, identityUser, claims);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.Message);
            }

            UsersRepository.CreateProfile((Guid)userId, model.AccountProfile.AccountName);
            var profile = UsersRepository.GetAccountProfileByUserId((Guid)userId);

            profile.AccountName = model.AccountProfile.AccountName;
            profile.Navigation = NamespaceNavigation.CleanAndValidate(model.AccountProfile.AccountName);
            profile.Biography = model.AccountProfile.Biography;
            profile.ModifiedDate = DateTime.UtcNow;
            UsersRepository.UpdateProfile(profile);

            var file = Request.Form.Files["Avatar"];
            if (file != null && file.Length > 0)
            {
                if (GlobalConfiguration.AllowableImageTypes.Contains(file.ContentType.ToLowerInvariant()) == false)
                {
                    model.ErrorMessage += Localize("Could not save the attached image, type not allowed.") + "\r\n";
                }
                else if (file.Length > GlobalConfiguration.MaxAvatarFileSize)
                {
                    model.ErrorMessage += Localize("Could not save the attached image, too large.") + "\r\n";
                }
                else
                {
                    try
                    {
                        var imageBytes = Utility.ConvertHttpFileToBytes(file);
                        var image = SixLabors.ImageSharp.Image.Load(new MemoryStream(imageBytes));
                        UsersRepository.UpdateProfileAvatar(profile.UserId, imageBytes, file.ContentType.ToLowerInvariant());
                    }
                    catch
                    {
                        model.ErrorMessage += Localize("Could not save the attached image.");
                    }
                }
            }

            return NotifyOf(Localize("The account has been created."), model.ErrorMessage, $"/AdminSecurity/Account/{profile.Navigation}");
        }

        [Authorize]
        [HttpGet("Accounts")]
        public ActionResult Accounts()
        {
            SessionState.RequireAdminPermission();

            var pageNumber = GetQueryValue("page", 1);
            var orderBy = GetQueryValue("OrderBy");
            var orderByDirection = GetQueryValue("OrderByDirection");
            var searchString = GetQueryValue("SearchString") ?? string.Empty;

            var model = new AccountsViewModel()
            {
                Users = UsersRepository.GetAllUsersPaged(pageNumber, orderBy, orderByDirection, searchString),
                SearchString = searchString
            };

            model.PaginationPageCount = (model.Users.FirstOrDefault()?.PaginationPageCount ?? 0);

            if (model.Users != null && model.Users.Count > 0)
            {
                model.Users.ForEach(o =>
                {
                    o.CreatedDate = SessionState.LocalizeDateTime(o.CreatedDate);
                    o.ModifiedDate = SessionState.LocalizeDateTime(o.ModifiedDate);
                });
            }

            return View(model);
        }

        [Authorize]
        [HttpPost("DeleteAccount/{navigation}")]
        public ActionResult DeleteAccount(ConfirmActionViewModel model, string navigation)
        {
            SessionState.RequireAdminPermission();

            if (model.UserSelection == true)
            {
                var profile = UsersRepository.GetAccountProfileByNavigation(navigation);

                var user = UserManager.FindByIdAsync(profile.UserId.ToString()).Result;
                if (user == null)
                {
                    return NotFound(Localize("User not found."));
                }

                var result = UserManager.DeleteAsync(user).Result;
                if (!result.Succeeded)
                {
                    throw new Exception(string.Join("<br />\r\n", result.Errors.Select(o => o.Description)));
                }

                UsersRepository.AnonymizeProfile(profile.UserId);
                WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.User, [profile.Navigation]));

                if (profile.UserId == SessionState.Profile?.UserId)
                {
                    //We're deleting our own account. Oh boy...
                    SignInManager.SignOutAsync();

                    return NotifyOfSuccess(Localize("Your account has been deleted."), $"/Profile/Deleted");
                }

                return NotifyOfSuccess(Localize("The account has been deleted."), $"/AdminSecurity/Accounts");
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        #endregion

        #region AutoComplete.

        [Authorize]
        [HttpGet("AutoCompleteAccount")]
        public ActionResult AutoCompleteAccount([FromQuery] string? q = null)
        {
            var accounts = UsersRepository.AutoCompleteAccount(q).ToList();

            return Json(accounts.Select(o => new
            {
                text = string.IsNullOrWhiteSpace(o.EmailAddress) ? o.AccountName : $"{o.AccountName} ({o.EmailAddress})",
                id = o.UserId.ToString()
            }));
        }

        [Authorize]
        [HttpGet("AutoCompletePage")]
        public ActionResult AutoCompletePage([FromQuery] string? q = null, [FromQuery] bool? showCatchAll = false)
        {
            var pages = PageRepository.AutoCompletePage(q).ToList();

            var results = pages.Select(o => new
            {
                text = o.Name,
                id = o.Id.ToString()
            }).ToList();

            if (showCatchAll == true)
            {
                results.Insert(0,
                new
                {
                    text = "*",
                    id = "*"
                });
            }

            return Json(results);
        }

        [Authorize]
        [HttpGet("AutoCompleteNamespace")]
        public ActionResult AutoCompleteNamespace([FromQuery] string? q = null, [FromQuery] bool? showCatchAll = false)
        {
            var namespaces = PageRepository.AutoCompleteNamespace(q).ToList();

            if (showCatchAll == true)
            {
                namespaces.Insert(0, "*");
            }

            return Json(namespaces.Select(o => new
            {
                text = o,
                id = o
            }));
        }

        #endregion
    }
}
