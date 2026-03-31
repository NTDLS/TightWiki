using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NTDLS.Helpers;
using System.Security.Claims;
using TightWiki.Library;
using TightWiki.Library.Caching;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;
using TightWiki.RequestModels;
using TightWiki.ViewModels.AdminSecurity;
using TightWiki.ViewModels.Shared;
using TightWiki.ViewModels.Utility;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class AdminSecurityController(
            ILogger<ITwEngine> logger,
            ITwConfigurationRepository configurationRepository,
            ITwPageRepository pageRepository,
            ITwSharedLocalizationText localizer,
            ITwUsersRepository usersRepository,
            SignInManager<IdentityUser> signInManager,
            TwConfiguration wikiConfiguration,
            UserManager<IdentityUser> userManager,
            ITwDatabaseManager databaseManager
        )
        : TwController<AdminSecurityController>(logger, signInManager, userManager, localizer, wikiConfiguration, databaseManager)
    {
        #region Roles.

        [Authorize]
        [HttpPost("DeleteRole/{roleId:int}")]
        public async Task<ActionResult> DeleteRole(ConfirmActionViewModel model, int roleId)
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                if (model.UserSelection == true)
                {
                    await usersRepository.DeleteRole(roleId);
                    MemCache.ClearCategory(MemCache.Category.Security);
                    return NotifyOfSuccess(Localize("The specified role has been deleted."), model.YesRedirectURL);
                }

                return Redirect($"{WikiConfiguration.BasePath}{model.NoRedirectURL}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error deleting role with id {RoleId}.", roleId);
                throw;
            }
        }

        /// <summary>
        /// This is called by ajax/jquery and does not redirect when authorization fails.
        /// </summary>
        [Authorize]
        [HttpPost("AddAccountMembership")]
        public async Task<ActionResult> AddAccountMembership([FromBody] AddAccountMembershipRequest request)
        {
            try
            {
                await SessionState.RequireAdminPermission();

                TwAddAccountMembershipResult? result = null;

                bool alreadyExists = await usersRepository.IsAccountAMemberOfRole(request.UserId, request.RoleId, true);
                if (!alreadyExists)
                {
                    result = await usersRepository.AddAccountMembership(request.UserId, request.RoleId);
                }
                MemCache.ClearCategory(MemCache.Category.Security);

                return Ok(new { success = true, alreadyExists = alreadyExists, membership = result, message = (string?)null });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error adding account membership for user {UserId} to role {RoleId}.", request.UserId, request.RoleId);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// This is called by ajax/jquery and does not redirect when authorization fails.
        /// </summary>
        [Authorize]
        [HttpPost("AddRoleMember")]
        public async Task<ActionResult> AddRoleMember([FromBody] AddRoleMemberRequest request)
        {
            try
            {
                await SessionState.RequireAdminPermission();

                TwAddRoleMemberResult? result = null;

                bool alreadyExists = await usersRepository.IsAccountAMemberOfRole(request.UserId, request.RoleId, true);
                if (!alreadyExists)
                {
                    result = await usersRepository.AddRoleMember(request.UserId, request.RoleId);
                }
                MemCache.ClearCategory(MemCache.Category.Security);

                return Ok(new { success = true, alreadyExists = alreadyExists, membership = result, message = (string?)null });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error adding role member for user {UserId} to role {RoleId}.", request.UserId, request.RoleId);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// This is called by ajax/jquery and does not redirect when authorization fails.
        /// </summary>
        [Authorize]
        [HttpPost("RemoveRoleMember/{roleId:int}/{userId:Guid}")]
        public async Task<ActionResult> RemoveRoleMember(int roleId, Guid userId)
        {
            try
            {
                await SessionState.RequireAdminPermission();
                await usersRepository.RemoveRoleMember(roleId, userId);
                MemCache.ClearCategory(MemCache.Category.Security);

                return Ok(new { success = true, message = (string?)null });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error removing role member for user {UserId} from role {RoleId}.", userId, roleId);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// This is called by ajax/jquery and does not redirect when authorization fails.
        /// </summary>
        [Authorize]
        [HttpPost("RemoveRolePermission/{id:int}")]
        public async Task<ActionResult> RemoveRolePermission(int id)
        {
            try
            {
                await SessionState.RequireAdminPermission();
                await usersRepository.RemoveRolePermission(id);
                MemCache.ClearCategory(MemCache.Category.Security);

                return Ok(new { success = true, message = (string?)null });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error removing role permission with id {PermissionId}.", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// This is called by ajax/jquery and does not redirect when authorization fails.
        /// </summary>
        [Authorize]
        [HttpPost("AddRolePermission")]
        public async Task<ActionResult> AddRolePermission([FromBody] AddRolePermissionRequest request)
        {
            try
            {
                await SessionState.RequireAdminPermission();

                TwInsertRolePermissionResult? result = null;

                bool alreadyExists = await usersRepository.IsRolePermissionDefined(
                    request.RoleId, request.PermissionId, request.PermissionDispositionId, request.Namespace, request.PageId, true);
                if (!alreadyExists)
                {
                    result = await usersRepository.InsertRolePermission(
                        request.RoleId, request.PermissionId, request.PermissionDispositionId, request.Namespace, request.PageId);
                }
                MemCache.ClearCategory(MemCache.Category.Security);

                return Ok(new { success = true, alreadyExists = alreadyExists, permission = result, message = (string?)null });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error adding role permission for role {RoleId} with permission {PermissionId}.", request.RoleId, request.PermissionId);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("AddRole")]
        public async Task<ActionResult> AddRole()
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                SessionState.Page.Name = Localize("Add Role");

                return View(new AddRoleViewModel());
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error displaying Add Role page.");
                throw;
            }
        }

        [Authorize]
        [HttpPost("AddRole")]
        public async Task<ActionResult> AddRole(AddRoleViewModel model)
        {
            try
            {
                await SessionState.RequireAdminPermission();
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

                if (await usersRepository.DoesRoleExist(model.Name))
                {
                    ModelState.AddModelError("Name", Localize("Role name is already in use."));
                    return View(model);
                }

                await usersRepository.InsertRole(model.Name, model.Description);
                MemCache.ClearCategory(MemCache.Category.Security);

                return Redirect($"{WikiConfiguration.BasePath}/AdminSecurity/Roles");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error adding new role with name {RoleName}.", model.Name);
                throw;
            }
        }

        [Authorize]
        [HttpGet("Role/{navigation}")]
        public async Task<ActionResult> Role(string navigation)
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                SessionState.Page.Name = Localize("Roles");

                navigation = TwNavigation.Clean(navigation);

                var role = await usersRepository.GetRoleByName(navigation);

                var model = new RoleViewModel()
                {
                    IsBuiltIn = role.IsBuiltIn,
                    Id = role.Id,
                    Name = role.Name,

                    Members = await usersRepository.GetRoleMembersPaged(role.Id,
                        GetQueryValue("Page_Members", 1), GetQueryValue<string>("OrderBy_Members"), GetQueryValue<string>("OrderByDirection_Members")),

                    AssignedPermissions = await usersRepository.GetRolePermissionsPaged(role.Id,
                        GetQueryValue("Page_Permissions", 1), GetQueryValue<string>("OrderBy_Permission"), GetQueryValue<string>("OrderByDirection_Permissions")),

                    PermissionDispositions = await usersRepository.GetAllPermissionDispositions(),
                    Permissions = await usersRepository.GetAllPermissions()
                };

                model.PaginationPageCount_Members = (model.Members.FirstOrDefault()?.PaginationPageCount ?? 0);
                model.PaginationPageCount_Permissions = (model.AssignedPermissions.FirstOrDefault()?.PaginationPageCount ?? 0);

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error displaying role page for role with navigation {Navigation}.", navigation);
                throw;
            }
        }

        [Authorize]
        [HttpGet("Roles")]
        public async Task<ActionResult> Roles()
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var orderBy = GetQueryValue<string>("OrderBy");
                var orderByDirection = GetQueryValue<string>("OrderByDirection");

                var model = new RolesViewModel()
                {
                    Roles = await usersRepository.GetAllRoles(orderBy, orderByDirection)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error displaying roles page.");
                throw;
            }
        }

        #endregion

        #region Account Roles.

        /// <summary>
        /// This is called by ajax/jquery and does not redirect when authorization fails.
        /// </summary>
        [Authorize]
        [HttpPost("AddAccountPermission")]
        public async Task<ActionResult> AddAccountPermission([FromBody] AddAccountPermissionRequest request)
        {
            try
            {
                await SessionState.RequireAdminPermission();

                TwInsertAccountPermissionResult? result = null;

                bool alreadyExists = await usersRepository.IsAccountPermissionDefined(
                    request.UserId, request.PermissionId, request.PermissionDispositionId, request.Namespace, request.PageId, true);
                if (!alreadyExists)
                {
                    result = await usersRepository.InsertAccountPermission(
                        request.UserId, request.PermissionId, request.PermissionDispositionId, request.Namespace, request.PageId);
                }
                MemCache.ClearCategory(MemCache.Category.Security);

                return Ok(new { success = true, alreadyExists = alreadyExists, permission = result, message = (string?)null });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error adding account permission for user {UserId} with permission {PermissionId}.", request.UserId, request.PermissionId);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// This is called by ajax/jquery and does not redirect when authorization fails.
        /// </summary>
        [Authorize]
        [HttpPost("RemoveAccountPermission/{id:int}")]
        public async Task<ActionResult> RemoveAccountPermission(int id)
        {
            try
            {
                await SessionState.RequireAdminPermission();
                await usersRepository.RemoveAccountPermission(id);
                MemCache.ClearCategory(MemCache.Category.Security);

                return Ok(new { success = true, message = (string?)null });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error removing account permission with id {PermissionId}.", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("AccountRoles/{navigation}")]
        public async Task<ActionResult> AccountRoles(string navigation)
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                SessionState.Page.Name = Localize("Roles");

                navigation = TwNavigation.Clean(navigation);

                var profile = await usersRepository.GetAccountProfileByNavigation(navigation);

                var model = new AccountRolesViewModel()
                {
                    Id = profile.UserId,
                    AccountName = profile.AccountName,

                    Memberships = await usersRepository.GetAccountRoleMembershipPaged(profile.UserId,
                        GetQueryValue("Page_Memberships", 1), GetQueryValue<string>("OrderBy_Members"), GetQueryValue<string>("OrderByDirection_Memberships")),

                    AssignedPermissions = await usersRepository.GetAccountPermissionsPaged(profile.UserId,
                        GetQueryValue("Page_Permissions", 1), GetQueryValue<string>("OrderBy_Permissions"), GetQueryValue<string>("OrderByDirection_Permissions")),

                    PermissionDispositions = await usersRepository.GetAllPermissionDispositions(),
                    Permissions = await usersRepository.GetAllPermissions()
                };

                model.PaginationPageCount_Members = (model.Memberships.FirstOrDefault()?.PaginationPageCount ?? 0);
                model.PaginationPageCount_Permissions = (model.AssignedPermissions.FirstOrDefault()?.PaginationPageCount ?? 0);

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error displaying account roles page for account with navigation {Navigation}.", navigation);
                throw;
            }
        }

        #endregion

        #region Accounts

        [Authorize]
        [HttpGet("Account/{navigation}")]
        public async Task<ActionResult> Account(string navigation)
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var model = new AccountProfileViewModel()
                {
                    AccountProfile = AccountProfileAccountViewModel.FromDataModel(
                        await usersRepository.GetAccountProfileByNavigation(TwNavigation.Clean(navigation))),

                    Credential = new CredentialViewModel(),
                    Themes = await configurationRepository.GetAllThemes(),
                    TimeZones = TimeZoneItem.GetAll(),
                    Countries = CountryItem.GetAll(),
                    Languages = LanguageItem.GetAll(),
                    Roles = await usersRepository.GetAllRoles()
                };

                model.AccountProfile.CreatedDate = SessionState.LocalizeDateTime(model.AccountProfile.CreatedDate);
                model.AccountProfile.ModifiedDate = SessionState.LocalizeDateTime(model.AccountProfile.ModifiedDate);

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error displaying account page for account with navigation {Navigation}.", navigation);
                throw;
            }
        }

        /// <summary>
        /// Save user profile.
        /// </summary>
        [Authorize]
        [HttpPost("Account/{navigation}")]
        public async Task<ActionResult> Account(string navigation, AccountProfileViewModel model)
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                model.Themes = await configurationRepository.GetAllThemes();
                model.TimeZones = TimeZoneItem.GetAll();
                model.Countries = CountryItem.GetAll();
                model.Languages = LanguageItem.GetAll();
                model.Roles = await usersRepository.GetAllRoles();
                model.AccountProfile.Navigation = TwNamespaceNavigation.CleanAndValidate(model.AccountProfile.AccountName.ToLowerInvariant());

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
                            await usersRepository.SetAdminPasswordIsChanged();
                        }
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("Credential.Password", ex.Message);
                        return View(model);
                    }
                }

                var profile = await usersRepository.GetAccountProfileByUserId(model.AccountProfile.UserId, true);
                if (!profile.Navigation.Equals(model.AccountProfile.Navigation, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (await usersRepository.DoesProfileAccountExist(model.AccountProfile.AccountName))
                    {
                        ModelState.AddModelError("AccountProfile.AccountName", Localize("Account name is already in use."));
                        return View(model);
                    }
                }

                if (!profile.EmailAddress.Equals(model.AccountProfile.EmailAddress, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (await usersRepository.DoesEmailAddressExist(model.AccountProfile.EmailAddress))
                    {
                        ModelState.AddModelError("AccountProfile.EmailAddress", Localize("Email address is already in use."));
                        return View(model);
                    }
                }

                var file = Request.Form.Files["Avatar"];
                if (file != null && file.Length > 0)
                {
                    if (WikiConfiguration.AllowableImageTypes.Contains(file.ContentType.ToLowerInvariant()) == false)
                    {
                        model.ErrorMessage += Localize("Could not save the attached image, type not allowed.") + "\r\n";
                    }
                    else if (file.Length > WikiConfiguration.MaxAvatarFileSize)
                    {
                        model.ErrorMessage += Localize("Could not save the attached image, too large.") + "\r\n";
                    }
                    else
                    {
                        try
                        {
                            var imageBytes = Utility.ConvertHttpFileToBytes(file);
                            var image = Utility.CropImageToCenteredSquare(new MemoryStream(imageBytes));
                            await usersRepository.UpdateProfileAvatar(profile.UserId, image, "image/webp");
                        }
                        catch
                        {
                            model.ErrorMessage += Localize("Could not save the attached image.") + "\r\n";
                        }
                    }
                }

                profile.AccountName = model.AccountProfile.AccountName;
                profile.Navigation = TwNamespaceNavigation.CleanAndValidate(model.AccountProfile.AccountName);
                profile.Biography = model.AccountProfile.Biography;
                profile.ModifiedDate = DateTime.UtcNow;
                await usersRepository.UpdateProfile(profile);

                var claims = new List<Claim>
                    {
                        new ("timezone", model.AccountProfile.TimeZone),
                        new (ClaimTypes.Country, model.AccountProfile.Country),
                        new ("language", model.AccountProfile.Language),
                        new ("firstname", model.AccountProfile.FirstName ?? ""),
                        new ("lastname", model.AccountProfile.LastName ?? ""),
                        new ("theme", model.AccountProfile.Theme ?? ""),
                    };
                await usersRepository.UpsertUserClaims(UserManager, user, claims);

                //If we are changing the currently logged in user, then make sure we take some extra actions so we can see the changes immediately.
                if (SessionState.Profile?.UserId == model.AccountProfile.UserId)
                {
                    await SignInManager.RefreshSignInAsync(user);

                    MemCache.ClearCategory(MemCacheKey.Build(MemCache.Category.User, [profile.Navigation]));
                    MemCache.ClearCategory(MemCacheKey.Build(MemCache.Category.User, [profile.UserId]));

                    //This is not 100% necessary, I just want to prevent the user from needing to refresh to view the new theme.
                    SessionState.UserTheme = (await configurationRepository.GetAllThemes())
                        .SingleOrDefault(o => o.Name == model.AccountProfile.Theme) ?? WikiConfiguration.SystemTheme;
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

                model.SuccessMessage = Localize("The profile has been saved successfully!");
                MemCache.ClearCategory(MemCache.Category.Security);

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error saving account page for account with navigation {Navigation}.", navigation);
                throw;
            }
        }

        [Authorize]
        [HttpGet("AddAccount")]
        public async Task<ActionResult> AddAccount()
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var membershipConfig = await configurationRepository.GetConfigurationEntryValuesByGroupName(TwConfigGroup.Membership);
                var defaultSignupRole = membershipConfig.Value<string>("Default Signup Role").EnsureNotNull();
                var customizationConfig = await configurationRepository.GetConfigurationEntryValuesByGroupName(TwConfigGroup.Customization);

                var model = new AccountProfileViewModel()
                {
                    AccountProfile = new AccountProfileAccountViewModel
                    {
                        AccountName = string.Empty,
                        Country = customizationConfig.Value<string>("Default Country", string.Empty),
                        TimeZone = customizationConfig.Value<string>("Default TimeZone", string.Empty),
                        Language = customizationConfig.Value<string>("Default Language", string.Empty)
                    },
                    DefaultRole = defaultSignupRole,
                    Themes = await configurationRepository.GetAllThemes(),
                    Credential = new CredentialViewModel(),
                    TimeZones = TimeZoneItem.GetAll(),
                    Countries = CountryItem.GetAll(),
                    Languages = LanguageItem.GetAll(),
                    Roles = await usersRepository.GetAllRoles()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error displaying Add Account page.");
                throw;
            }
        }

        /// <summary>
        /// Create a new user profile.
        /// </summary>
        [Authorize]
        [HttpPost("AddAccount")]
        public async Task<ActionResult> AddAccount(AccountProfileViewModel model)
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                model.Themes = await configurationRepository.GetAllThemes();
                model.TimeZones = TimeZoneItem.GetAll();
                model.Countries = CountryItem.GetAll();
                model.Languages = LanguageItem.GetAll();
                model.Roles = await usersRepository.GetAllRoles();
                model.AccountProfile.Navigation = TwNamespaceNavigation.CleanAndValidate(model.AccountProfile.AccountName?.ToLowerInvariant());

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                if (string.IsNullOrWhiteSpace(model.AccountProfile.AccountName))
                {
                    ModelState.AddModelError("AccountProfile.AccountName", Localize("Account name is required."));
                    return View(model);
                }

                if (await usersRepository.DoesProfileAccountExist(model.AccountProfile.AccountName))
                {
                    ModelState.AddModelError("AccountProfile.AccountName", Localize("Account name is already in use."));
                    return View(model);
                }

                if (await usersRepository.DoesEmailAddressExist(model.AccountProfile.EmailAddress))
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
                    await usersRepository.UpsertUserClaims(UserManager, identityUser, claims);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.Message);
                }

                await usersRepository.CreateProfile(userId.Value, model.AccountProfile.AccountName);
                await usersRepository.AddRoleMemberByname(userId.Value, model.DefaultRole);

                var profile = await usersRepository.GetAccountProfileByUserId(userId.Value);

                profile.AccountName = model.AccountProfile.AccountName;
                profile.Navigation = TwNamespaceNavigation.CleanAndValidate(model.AccountProfile.AccountName);
                profile.Biography = model.AccountProfile.Biography;
                profile.ModifiedDate = DateTime.UtcNow;
                await usersRepository.UpdateProfile(profile);

                var file = Request.Form.Files["Avatar"];
                if (file != null && file.Length > 0)
                {
                    if (WikiConfiguration.AllowableImageTypes.Contains(file.ContentType.ToLowerInvariant()) == false)
                    {
                        model.ErrorMessage += Localize("Could not save the attached image, type not allowed.") + "\r\n";
                    }
                    else if (file.Length > WikiConfiguration.MaxAvatarFileSize)
                    {
                        model.ErrorMessage += Localize("Could not save the attached image, too large.") + "\r\n";
                    }
                    else
                    {
                        try
                        {
                            var imageBytes = Utility.ConvertHttpFileToBytes(file);
                            var image = Utility.CropImageToCenteredSquare(new MemoryStream(imageBytes));
                            await usersRepository.UpdateProfileAvatar(profile.UserId, image, "image/webp");
                        }
                        catch
                        {
                            model.ErrorMessage += Localize("Could not save the attached image.");
                        }
                    }
                }
                MemCache.ClearCategory(MemCache.Category.Security);

                return NotifyOf(Localize("The account has been created."), model.ErrorMessage, $"/AdminSecurity/Account/{profile.Navigation}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating account with name {AccountName}.", model.AccountProfile.AccountName);
                throw;
            }
        }

        [Authorize]
        [HttpGet("Accounts")]
        public async Task<ActionResult> Accounts()
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var pageNumber = GetQueryValue("page", 1);
                var orderBy = GetQueryValue<string>("OrderBy");
                var orderByDirection = GetQueryValue<string>("OrderByDirection");
                var searchString = GetQueryValue("SearchString", string.Empty);

                var model = new AccountsViewModel()
                {
                    Users = await usersRepository.GetAllUsersPaged(pageNumber, orderBy, orderByDirection, searchString),
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
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error displaying accounts page.");
                throw;
            }
        }

        [Authorize]
        [HttpPost("DeleteAccount/{navigation}")]
        public async Task<ActionResult> DeleteAccount(ConfirmActionViewModel model, string navigation)
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                if (model.UserSelection == true)
                {
                    var profile = await usersRepository.GetAccountProfileByNavigation(navigation);

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

                    await usersRepository.AnonymizeProfile(profile.UserId);
                    MemCache.ClearCategory(MemCacheKey.Build(MemCache.Category.User, [profile.Navigation]));

                    if (profile.UserId == SessionState.Profile?.UserId)
                    {
                        //We're deleting our own account. Oh boy...
                        await SignInManager.SignOutAsync();

                        MemCache.ClearCategory(MemCache.Category.Security);
                        return NotifyOfSuccess(Localize("Your account has been deleted."), $"/Profile/Deleted");
                    }
                    MemCache.ClearCategory(MemCache.Category.Security);

                    return NotifyOfSuccess(Localize("The account has been deleted."), $"/AdminSecurity/Accounts");
                }

                return Redirect($"{WikiConfiguration.BasePath}{model.NoRedirectURL}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error deleting account with navigation {Navigation}.", navigation);
                throw;
            }
        }

        #endregion

        #region AutoComplete.

        [Authorize]
        [HttpGet("AutoCompleteRole")]
        public async Task<ActionResult> AutoCompleteRole([FromQuery] string? q = null)
        {
            try
            {
                var roles = await usersRepository.AutoCompleteRole(q);

                return Json(roles.Select(o => new
                {
                    text = o.Name,
                    id = o.Id.ToString()
                }));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during AutoCompleteRole with query {Query}.", q);
                throw;
            }
        }

        [Authorize]
        [HttpGet("AutoCompleteAccount")]
        public async Task<ActionResult> AutoCompleteAccount([FromQuery] string? q = null)
        {
            try
            {
                var accounts = await usersRepository.AutoCompleteAccount(q);

                return Json(accounts.Select(o => new
                {
                    text = string.IsNullOrWhiteSpace(o.EmailAddress) ? o.AccountName : $"{o.AccountName} ({o.EmailAddress})",
                    id = o.UserId.ToString()
                }));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during AutoCompleteAccount with query {Query}.", q);
                throw;
            }
        }

        [Authorize]
        [HttpGet("AutoCompletePage")]
        public async Task<ActionResult> AutoCompletePage([FromQuery] string? q = null, [FromQuery] bool? showCatchAll = false)
        {
            try
            {
                var pages = await pageRepository.AutoCompletePage(q);

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
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during AutoCompletePage with query {Query}.", q);
                throw;
            }
        }

        [Authorize]
        [HttpGet("AutoCompleteNamespace")]
        public async Task<ActionResult> AutoCompleteNamespace([FromQuery] string? q = null, [FromQuery] bool? showCatchAll = false)
        {
            try
            {
                var namespaces = await pageRepository.AutoCompleteNamespace(q);

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
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during AutoCompleteNamespace with query {Query}.", q);
                throw;
            }
        }

        #endregion
    }
}
