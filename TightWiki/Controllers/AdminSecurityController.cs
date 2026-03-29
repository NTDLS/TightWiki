using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NTDLS.Helpers;
using System.Security.Claims;
using TightWiki.Caching;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Library;
using TightWiki.Models;
using TightWiki.Models.DataModels;
using TightWiki.Models.Requests;
using TightWiki.Models.ViewModels.AdminSecurity;
using TightWiki.Models.ViewModels.Shared;
using TightWiki.Models.ViewModels.Utility;
using TightWiki.Repository;
using static TightWiki.Library.Constants;
using Constants = TightWiki.Library.Constants;

namespace TightWiki.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class AdminSecurityController(ILogger<ITightEngine> logger, SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager, ISharedLocalizationText localizer, TightWikiConfiguration wikiConfiguration)
        : WikiControllerBase<AdminSecurityController>(logger, signInManager, userManager, localizer, wikiConfiguration)
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
                    await UsersRepository.DeleteRole(roleId);
                    WikiCache.ClearCategory(WikiCache.Category.Security);
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

                AddAccountMembershipResult? result = null;

                bool alreadyExists = await UsersRepository.IsAccountAMemberOfRole(request.UserId, request.RoleId, true);
                if (!alreadyExists)
                {
                    result = await UsersRepository.AddAccountMembership(request.UserId, request.RoleId);
                }
                WikiCache.ClearCategory(WikiCache.Category.Security);

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

                AddRoleMemberResult? result = null;

                bool alreadyExists = await UsersRepository.IsAccountAMemberOfRole(request.UserId, request.RoleId, true);
                if (!alreadyExists)
                {
                    result = await UsersRepository.AddRoleMember(request.UserId, request.RoleId);
                }
                WikiCache.ClearCategory(WikiCache.Category.Security);

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
                await UsersRepository.RemoveRoleMember(roleId, userId);
                WikiCache.ClearCategory(WikiCache.Category.Security);

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
                await UsersRepository.RemoveRolePermission(id);
                WikiCache.ClearCategory(WikiCache.Category.Security);

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

                InsertRolePermissionResult? result = null;

                bool alreadyExists = await UsersRepository.IsRolePermissionDefined(
                    request.RoleId, request.PermissionId, request.PermissionDispositionId, request.Namespace, request.PageId, true);
                if (!alreadyExists)
                {
                    result = await UsersRepository.InsertRolePermission(
                        request.RoleId, request.PermissionId, request.PermissionDispositionId, request.Namespace, request.PageId);
                }
                WikiCache.ClearCategory(WikiCache.Category.Security);

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

                if (await UsersRepository.DoesRoleExist(model.Name))
                {
                    ModelState.AddModelError("Name", Localize("Role name is already in use."));
                    return View(model);
                }

                await UsersRepository.InsertRole(model.Name, model.Description);
                WikiCache.ClearCategory(WikiCache.Category.Security);

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

                navigation = Navigation.Clean(navigation);

                var role = await UsersRepository.GetRoleByName(navigation);

                var model = new RoleViewModel()
                {
                    IsBuiltIn = role.IsBuiltIn,
                    Id = role.Id,
                    Name = role.Name,

                    Members = await UsersRepository.GetRoleMembersPaged(role.Id,
                        GetQueryValue("Page_Members", 1), GetQueryValue<string>("OrderBy_Members"), GetQueryValue<string>("OrderByDirection_Members")),

                    AssignedPermissions = await UsersRepository.GetRolePermissionsPaged(role.Id,
                        GetQueryValue("Page_Permissions", 1), GetQueryValue<string>("OrderBy_Permission"), GetQueryValue<string>("OrderByDirection_Permissions")),

                    PermissionDispositions = await UsersRepository.GetAllPermissionDispositions(),
                    Permissions = await UsersRepository.GetAllPermissions()
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
                    Roles = await UsersRepository.GetAllRoles(orderBy, orderByDirection)
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

                InsertAccountPermissionResult? result = null;

                bool alreadyExists = await UsersRepository.IsAccountPermissionDefined(
                    request.UserId, request.PermissionId, request.PermissionDispositionId, request.Namespace, request.PageId, true);
                if (!alreadyExists)
                {
                    result = await UsersRepository.InsertAccountPermission(
                        request.UserId, request.PermissionId, request.PermissionDispositionId, request.Namespace, request.PageId);
                }
                WikiCache.ClearCategory(WikiCache.Category.Security);

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
                await UsersRepository.RemoveAccountPermission(id);
                WikiCache.ClearCategory(WikiCache.Category.Security);

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

                navigation = Navigation.Clean(navigation);

                var profile = await UsersRepository.GetAccountProfileByNavigation(navigation);

                var model = new AccountRolesViewModel()
                {
                    Id = profile.UserId,
                    AccountName = profile.AccountName,

                    Memberships = await UsersRepository.GetAccountRoleMembershipPaged(profile.UserId,
                        GetQueryValue("Page_Memberships", 1), GetQueryValue<string>("OrderBy_Members"), GetQueryValue<string>("OrderByDirection_Memberships")),

                    AssignedPermissions = await UsersRepository.GetAccountPermissionsPaged(profile.UserId,
                        GetQueryValue("Page_Permissions", 1), GetQueryValue<string>("OrderBy_Permissions"), GetQueryValue<string>("OrderByDirection_Permissions")),

                    PermissionDispositions = await UsersRepository.GetAllPermissionDispositions(),
                    Permissions = await UsersRepository.GetAllPermissions()
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
                        await UsersRepository.GetAccountProfileByNavigation(Navigation.Clean(navigation))),

                    Credential = new CredentialViewModel(),
                    Themes = await ConfigurationRepository.GetAllThemes(),
                    TimeZones = TimeZoneItem.GetAll(),
                    Countries = CountryItem.GetAll(),
                    Languages = LanguageItem.GetAll(),
                    Roles = await UsersRepository.GetAllRoles()
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
        public async Task<ActionResult> Account(string navigation, Models.ViewModels.AdminSecurity.AccountProfileViewModel model)
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
                model.Themes = await ConfigurationRepository.GetAllThemes();
                model.TimeZones = TimeZoneItem.GetAll();
                model.Countries = CountryItem.GetAll();
                model.Languages = LanguageItem.GetAll();
                model.Roles = await UsersRepository.GetAllRoles();
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
                            await UsersRepository.SetAdminPasswordIsChanged();
                        }
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("Credential.Password", ex.Message);
                        return View(model);
                    }
                }

                var profile = await UsersRepository.GetAccountProfileByUserId(model.AccountProfile.UserId, true);
                if (!profile.Navigation.Equals(model.AccountProfile.Navigation, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (await UsersRepository.DoesProfileAccountExist(model.AccountProfile.AccountName))
                    {
                        ModelState.AddModelError("AccountProfile.AccountName", Localize("Account name is already in use."));
                        return View(model);
                    }
                }

                if (!profile.EmailAddress.Equals(model.AccountProfile.EmailAddress, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (await UsersRepository.DoesEmailAddressExist(model.AccountProfile.EmailAddress))
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
                            await UsersRepository.UpdateProfileAvatar(profile.UserId, image, "image/webp");
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
                await UsersRepository.UpdateProfile(profile);

                var claims = new List<Claim>
                    {
                        new ("timezone", model.AccountProfile.TimeZone),
                        new (ClaimTypes.Country, model.AccountProfile.Country),
                        new ("language", model.AccountProfile.Language),
                        new ("firstname", model.AccountProfile.FirstName ?? ""),
                        new ("lastname", model.AccountProfile.LastName ?? ""),
                        new ("theme", model.AccountProfile.Theme ?? ""),
                    };
                await SecurityRepository.UpsertUserClaims(UserManager, user, claims);

                //If we are changing the currently logged in user, then make sure we take some extra actions so we can see the changes immediately.
                if (SessionState.Profile?.UserId == model.AccountProfile.UserId)
                {
                    await SignInManager.RefreshSignInAsync(user);

                    WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.User, [profile.Navigation]));
                    WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.User, [profile.UserId]));

                    //This is not 100% necessary, I just want to prevent the user from needing to refresh to view the new theme.
                    SessionState.UserTheme = (await ConfigurationRepository.GetAllThemes())
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
                WikiCache.ClearCategory(WikiCache.Category.Security);

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
                var membershipConfig = await ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Membership);
                var defaultSignupRole = membershipConfig.Value<string>("Default Signup Role").EnsureNotNull();
                var customizationConfig = await ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Customization);

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
                    Themes = await ConfigurationRepository.GetAllThemes(),
                    Credential = new CredentialViewModel(),
                    TimeZones = TimeZoneItem.GetAll(),
                    Countries = CountryItem.GetAll(),
                    Languages = LanguageItem.GetAll(),
                    Roles = await UsersRepository.GetAllRoles()
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
        public async Task<ActionResult> AddAccount(Models.ViewModels.AdminSecurity.AccountProfileViewModel model)
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
                model.Themes = await ConfigurationRepository.GetAllThemes();
                model.TimeZones = TimeZoneItem.GetAll();
                model.Countries = CountryItem.GetAll();
                model.Languages = LanguageItem.GetAll();
                model.Roles = await UsersRepository.GetAllRoles();
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

                if (await UsersRepository.DoesProfileAccountExist(model.AccountProfile.AccountName))
                {
                    ModelState.AddModelError("AccountProfile.AccountName", Localize("Account name is already in use."));
                    return View(model);
                }

                if (await UsersRepository.DoesEmailAddressExist(model.AccountProfile.EmailAddress))
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
                    await SecurityRepository.UpsertUserClaims(UserManager, identityUser, claims);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.Message);
                }

                await UsersRepository.CreateProfile(userId.Value, model.AccountProfile.AccountName);
                await UsersRepository.AddRoleMemberByname(userId.Value, model.DefaultRole);

                var profile = await UsersRepository.GetAccountProfileByUserId(userId.Value);

                profile.AccountName = model.AccountProfile.AccountName;
                profile.Navigation = NamespaceNavigation.CleanAndValidate(model.AccountProfile.AccountName);
                profile.Biography = model.AccountProfile.Biography;
                profile.ModifiedDate = DateTime.UtcNow;
                await UsersRepository.UpdateProfile(profile);

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
                            await UsersRepository.UpdateProfileAvatar(profile.UserId, image, "image/webp");
                        }
                        catch
                        {
                            model.ErrorMessage += Localize("Could not save the attached image.");
                        }
                    }
                }
                WikiCache.ClearCategory(WikiCache.Category.Security);

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
                    Users = await UsersRepository.GetAllUsersPaged(pageNumber, orderBy, orderByDirection, searchString),
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
                    var profile = await UsersRepository.GetAccountProfileByNavigation(navigation);

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

                    await UsersRepository.AnonymizeProfile(profile.UserId);
                    WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.User, [profile.Navigation]));

                    if (profile.UserId == SessionState.Profile?.UserId)
                    {
                        //We're deleting our own account. Oh boy...
                        await SignInManager.SignOutAsync();

                        WikiCache.ClearCategory(WikiCache.Category.Security);
                        return NotifyOfSuccess(Localize("Your account has been deleted."), $"/Profile/Deleted");
                    }
                    WikiCache.ClearCategory(WikiCache.Category.Security);

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
                var roles = await UsersRepository.AutoCompleteRole(q);

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
                var accounts = await UsersRepository.AutoCompleteAccount(q);

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
                var pages = await PageRepository.AutoCompletePage(q);

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
                var namespaces = await PageRepository.AutoCompleteNamespace(q);

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
