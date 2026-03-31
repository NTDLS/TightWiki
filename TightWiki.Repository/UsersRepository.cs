using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using NTDLS.Helpers;
using NTDLS.SqliteDapperWrapper;
using System.Security.Claims;
using TightWiki.Library;
using TightWiki.Library.Caching;
using TightWiki.Library.Extensions;
using TightWiki.Library.Security;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;
using TightWiki.Repository.Helpers;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Repository
{
    public class UsersRepository
        : ITwUsersRepository
    {
        readonly private ITwConfigurationRepository _configurationRepository;
        public SqliteManagedFactory UsersFactory { get; private set; }

        public UsersRepository(IConfiguration configuration, ITwConfigurationRepository configurationRepository)
        {
            _configurationRepository = configurationRepository;

            var configDatabaseFile = configurationRepository.ConfigFactory.Ephemeral(o => o.NativeConnection.DataSource);

            UsersFactory = new SqliteManagedFactory(configuration.GetDatabaseConnectionString("ConfigConnection", "users.db", configDatabaseFile));
        }

        public async Task<bool> IsAccountAMemberOfRole(Guid userId, int roleId, bool forceReCache = false)
        {
            var param = new
            {
                UserId = userId,
                RoleId = roleId
            };

            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Security, [userId, roleId]);

            return await TwCache.AddOrGetAsync(cacheKey, forceReCache, async () =>
                await UsersFactory.QueryFirstOrDefaultAsync<bool?>("IsAccountAMemberOfRole.sql", param) ?? false
            );
        }

        public async Task DeleteRole(int roleId)
            => await UsersFactory.ExecuteAsync("DeleteRole.sql", new { Id = roleId });

        public async Task<bool> InsertRole(string name, string? description)
            => await UsersFactory.ExecuteScalarAsync<bool?>("InsertRole.sql", new { Name = name, Description = description }) ?? false;

        public async Task<bool> DoesRoleExist(string name)
            => await UsersFactory.ExecuteScalarAsync<bool?>("DoesRoleExist.sql", new { Name = name }) ?? false;

        public async Task<bool> IsAccountPermissionDefined(Guid userId, int permissionId, string permissionDispositionId, string? ns, string? pageId, bool forceReCache = true)
        {
            var param = new
            {
                UserId = userId,
                PermissionId = permissionId,
                PermissionDispositionId = permissionDispositionId,
                Namespace = ns,
                PageId = pageId
            };

            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Security, [userId, permissionId, permissionDispositionId, ns, pageId]);

            return await TwCache.AddOrGetAsync(cacheKey, forceReCache, async () =>
                await UsersFactory.QueryFirstOrDefaultAsync<bool?>("IsAccountPermissionDefined.sql", param) ?? false
            );
        }

        public async Task<TwInsertAccountPermissionResult?> InsertAccountPermission(
            Guid userId, int permissionId, string permissionDisposition, string? ns, string? pageId)
        {
            var param = new
            {
                UserId = userId,
                PermissionId = permissionId,
                PermissionDispositionId = permissionDisposition,
                @Namespace = ns,
                PageId = pageId
            };

            return await UsersFactory.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("pages.db", "pages_db");
                return await o.QueryFirstOrDefaultAsync<TwInsertAccountPermissionResult>("InsertAccountPermission.sql", param);
            });
        }

        public async Task<bool> IsRolePermissionDefined(int roleId, int permissionId, string permissionDispositionId, string? ns, string? pageId, bool forceReCache = false)
        {
            var param = new
            {
                RoleId = roleId,
                PermissionId = permissionId,
                PermissionDispositionId = permissionDispositionId,
                Namespace = ns,
                PageId = pageId
            };

            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Security, [roleId, permissionId, permissionDispositionId, ns, pageId]);

            return await TwCache.AddOrGetAsync(cacheKey, forceReCache, async () =>
                await UsersFactory.QueryFirstOrDefaultAsync<bool?>("IsRolePermissionDefined.sql", param) ?? false
            );
        }

        public async Task<List<TwRole>> AutoCompleteRole(string? searchText)
            => await UsersFactory.QueryAsync<TwRole>("AutoCompleteRole.sql", new { SearchText = searchText ?? string.Empty });

        public async Task<List<TwAccountProfile>> AutoCompleteAccount(string? searchText)
            => await UsersFactory.QueryAsync<TwAccountProfile>("AutoCompleteAccount.sql", new { SearchText = searchText ?? string.Empty });

        public async Task<TwAddRoleMemberResult?> AddRoleMemberByname(Guid userId, string roleName)
            => await UsersFactory.QueryFirstOrDefaultAsync<TwAddRoleMemberResult>("AddRoleMemberByname.sql", new { UserId = userId, RoleName = roleName });

        public async Task<TwAddRoleMemberResult?> AddRoleMember(Guid userId, int roleId)
            => await UsersFactory.QueryFirstOrDefaultAsync<TwAddRoleMemberResult>("AddRoleMember.sql", new { UserId = userId, RoleId = roleId });

        public async Task<TwAddAccountMembershipResult?> AddAccountMembership(Guid userId, int roleId)
            => await UsersFactory.QueryFirstOrDefaultAsync<TwAddAccountMembershipResult>("AddAccountMembership.sql", new { UserId = userId, RoleId = roleId });

        public async Task RemoveRoleMember(int roleId, Guid userId)
            => await UsersFactory.ExecuteAsync("RemoveRoleMember.sql", new { RoleId = roleId, UserId = userId });

        public async Task RemoveRolePermission(int id)
            => await UsersFactory.ExecuteAsync("RemoveRolePermission.sql", new { Id = id });

        public async Task RemoveAccountPermission(int id)
            => await UsersFactory.ExecuteAsync("RemoveAccountPermission.sql", new { Id = id });

        public async Task<TwInsertRolePermissionResult?> InsertRolePermission(
            int roleId, int permissionId, string permissionDisposition, string? ns, string? pageId)
        {
            var param = new
            {
                RoleId = roleId,
                PermissionId = permissionId,
                PermissionDispositionId = permissionDisposition,
                @Namespace = ns,
                PageId = pageId
            };

            return await UsersFactory.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("pages.db", "pages_db");
                return await o.QueryFirstOrDefaultAsync<TwInsertRolePermissionResult>("InsertRolePermission.sql", param);
            });
        }

        /// <summary>
        /// Gets the apparent account permissions for a user combined with the permissions of all roles that user is a member of.
        /// </summary>
        public async Task<List<TwApparentPermission>> GetApparentAccountPermissions(Guid userId)
        {
            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Security, [userId]);

            return await TwCache.AddOrGet(cacheKey, async () =>
            {
                return await UsersFactory.QueryAsync<TwApparentPermission>(@"Scripts\GetApparentAccountPermissions.sql",
                new
                {
                    UserId = userId
                });
            }).EnsureNotNull();
        }

        public async Task<List<TwApparentPermission>> GetApparentRolePermissions(WikiRoles role)
            => await GetApparentRolePermissions(role.ToString());

        public async Task<List<TwApparentPermission>> GetApparentRolePermissions(string roleName)
        {
            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Security, [roleName]);

            return (await TwCache.AddOrGetAsync(cacheKey, async () =>
            {
                return await UsersFactory.QueryAsync<TwApparentPermission>(@"Scripts\GetApparentRolePermissions.sql",
                new
                {
                    RoleName = roleName
                });
            })).EnsureNotNull();
        }

        public async Task<List<TwPermissionDisposition>> GetAllPermissionDispositions()
        {
            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Security);

            return (await TwCache.AddOrGetAsync(cacheKey, async () =>
            {
                return await UsersFactory.QueryAsync<TwPermissionDisposition>(@"Scripts\GetAllPermissionDispositions.sql");
            })).EnsureNotNull();
        }

        public async Task<List<TwPermission>> GetAllPermissions()
        {
            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Security);

            return (await TwCache.AddOrGetAsync(cacheKey, async () =>
            {
                return await UsersFactory.QueryAsync<TwPermission>(@"Scripts\GetAllPermissions.sql");
            })).EnsureNotNull();
        }

        public async Task<List<TwRolePermission>> GetRolePermissionsPaged(int roleId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            return await UsersFactory.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("pages.db", "pages_db");

                pageSize ??= await _configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

                var param = new
                {
                    PageSize = pageSize,
                    PageNumber = pageNumber,
                    RoleId = roleId
                };

                var query = RepositoryHelpers.TransposeOrderby("GetRolePermissionsPaged.sql", orderBy, orderByDirection);
                return await o.QueryAsync<TwRolePermission>(query, param);
            });
        }

        public async Task<List<TwAccountProfile>> GetAllPublicProfilesPaged(int pageNumber, int? pageSize = null, string? searchToken = null)
        {
            pageSize ??= await _configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchToken = searchToken
            };

            return await UsersFactory.QueryAsync<TwAccountProfile>("GetAllPublicProfilesPaged.sql", param);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        public async Task AnonymizeProfile(Guid userId)
        {
            string anonymousName = "DeletedUser_" + Utility.SanitizeAccountName($"{DateTime.UtcNow}", [' ']).Replace("_", "");

            var param = new
            {
                UserId = userId,
                ModifiedDate = DateTime.UtcNow,
                StandinName = anonymousName,
                Navigation = TwNavigation.Clean(anonymousName)
            };

            await UsersFactory.ExecuteAsync("AnonymizeProfile.sql", param);
        }

        public async Task<bool> IsUserMemberOfAdministrators(Guid userId)
        {
            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.User, [userId]);

            return await TwCache.AddOrGetAsync(cacheKey, async () =>
            {
                var result = await UsersFactory.ExecuteScalarAsync<int?>("IsUserMemberOfAdministrators.sql",
                new
                {
                    UserId = userId
                });
                return result == 1;
            });
        }

        public async Task<TwRole> GetRoleByName(string name)
        {
            var param = new
            {
                Name = name
            };

            return UsersFactory.QuerySingle<TwRole>("GetRoleByName.sql", param);
        }

        public async Task<List<TwRole>> GetAllRoles(string? orderBy = null, string? orderByDirection = null)
        {
            var query = RepositoryHelpers.TransposeOrderby("GetAllRoles.sql", orderBy, orderByDirection);
            return await UsersFactory.QueryAsync<TwRole>(query);
        }

        public async Task<List<TwAccountProfile>> GetRoleMembersPaged(int roleId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                RoleId = roleId,
                PageNumber = pageNumber,
                PageSize = paginationSize
            };

            var query = RepositoryHelpers.TransposeOrderby("GetRoleMembersPaged.sql", orderBy, orderByDirection);
            return await UsersFactory.QueryAsync<TwAccountProfile>(query, param);
        }

        public async Task<List<TwAccountPermission>> GetAccountPermissionsPaged(Guid userId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            return await UsersFactory.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("pages.db", "pages_db");

                pageSize ??= await _configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

                var param = new
                {
                    PageSize = pageSize,
                    PageNumber = pageNumber,
                    UserId = userId
                };

                var query = RepositoryHelpers.TransposeOrderby("GetAccountPermissionsPaged.sql", orderBy, orderByDirection);
                return await o.QueryAsync<TwAccountPermission>(query, param);
            });
        }

        public async Task<List<TwAccountRoleMembership>> GetAccountRoleMembershipPaged(Guid userId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                UserId = userId,
                PageNumber = pageNumber,
                PageSize = paginationSize
            };

            var query = RepositoryHelpers.TransposeOrderby("GetAccountRoleMembershipPaged.sql", orderBy, orderByDirection);
            return await UsersFactory.QueryAsync<TwAccountRoleMembership>(query, param);
        }

        public async Task<List<TwAccountProfile>> GetAllUsers()
            => await UsersFactory.QueryAsync<TwAccountProfile>("GetAllUsers.sql");

        public async Task<List<TwAccountProfile>> GetAllUsersPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, string? searchToken = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = paginationSize,
                SearchToken = searchToken
            };

            var query = RepositoryHelpers.TransposeOrderby("GetAllUsersPaged.sql", orderBy, orderByDirection);
            return await UsersFactory.QueryAsync<TwAccountProfile>(query, param);
        }

        public async Task CreateProfile(Guid userId, string accountName)
        {
            if (await DoesProfileAccountExist(TwNavigation.Clean(accountName)))
            {
                throw new Exception("An account with that name already exists");
            }

            var param = new
            {
                UserId = userId,
                AccountName = accountName,
                Navigation = TwNavigation.Clean(accountName),
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            await UsersFactory.ExecuteAsync("CreateProfile.sql", param);
        }

        public async Task<bool> DoesEmailAddressExist(string? emailAddress)
        {
            var param = new
            {
                EmailAddress = emailAddress?.ToLowerInvariant()
            };

            return (await UsersFactory.ExecuteScalarAsync<int?>("DoesEmailAddressExist.sql", param) ?? 0) != 0;
        }

        public async Task<bool> DoesProfileAccountExist(string navigation)
        {
            var param = new
            {
                Navigation = navigation?.ToLowerInvariant()
            };

            return ((await UsersFactory.ExecuteScalarAsync<int?>("DoesProfileAccountExist.sql", param)) ?? 0) != 0;
        }

        public async Task<TwAccountProfile?> GetBasicProfileByUserId(Guid userId)
        {
            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.User, [userId]);
            return await TwCache.AddOrGetAsync(cacheKey, async () =>
            {
                var param = new
                {
                    UserId = userId
                };

                return await UsersFactory.QuerySingleOrDefaultAsync<TwAccountProfile?>("GetBasicProfileByUserId.sql", param);
            });
        }

        public async Task<TwAccountProfile> GetAccountProfileByUserId(Guid userId, bool forceReCache = false)
        {
            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.User, [userId]);

            return (await TwCache.AddOrGetAsync(cacheKey, forceReCache, async () =>
            {
                var param = new
                {
                    UserId = userId
                };

                return await UsersFactory.QuerySingleAsync<TwAccountProfile>("GetAccountProfileByUserId.sql", param);
            })).EnsureNotNull();
        }

        public async Task SetProfileUserId(string navigation, Guid userId)
        {
            var param = new
            {
                Navigation = navigation,
                UserId = userId
            };

            await UsersFactory.ExecuteAsync("SetProfileUserId.sql", param);
        }

        public async Task<Guid?> GetUserAccountIdByNavigation(string navigation)
        {
            var param = new
            {
                Navigation = navigation
            };

            return await UsersFactory.QueryFirstOrDefaultAsync<Guid>("GetUserAccountIdByNavigation.sql", param);
        }

        public async Task<TwAccountProfile> GetAccountProfileByNavigation(string? navigation)
        {
            var param = new
            {
                Navigation = navigation
            };

            return await UsersFactory.QuerySingleAsync<TwAccountProfile>("GetAccountProfileByNavigation.sql", param);
        }

        public async Task<TwAccountProfile?> GetProfileByAccountNameOrEmailAndPasswordHash(string accountNameOrEmail, string passwordHash)
        {
            var param = new
            {
                AccountNameOrEmail = accountNameOrEmail,
                PasswordHash = passwordHash
            };

            return await UsersFactory.QuerySingleAsync<TwAccountProfile>("GetProfileByAccountNameOrEmailAndPasswordHash.sql", param);
        }

        public async Task<TwAccountProfile?> GetProfileByAccountNameOrEmailAndPassword(string accountNameOrEmail, string password)
        {
            string passwordHash = SecurityUtility.Sha256(password);
            var param = new
            {
                AccountNameOrEmail = accountNameOrEmail,
                PasswordHash = passwordHash
            };

            return await UsersFactory.QuerySingleAsync<TwAccountProfile>("GetProfileByAccountNameOrEmailAndPasswordHash.sql", param);
        }

        public async Task<TwProfileAvatar?> GetProfileAvatarByNavigation(string navigation)
            => await UsersFactory.QuerySingleOrDefaultAsync<TwProfileAvatar>("GetProfileAvatarByNavigation.sql", new { Navigation = navigation });

        public async Task UpdateProfile(TwAccountProfile item)
        {
            var param = new
            {
                UserId = item.UserId,
                AccountName = item.AccountName,
                Navigation = item.Navigation,
                Biography = item.Biography,
                ModifiedDate = item.ModifiedDate
            };

            await UsersFactory.ExecuteAsync("UpdateProfile.sql", param);
            TwCache.ClearCategory(TwCacheKey.Build(TwCache.Category.User, [item.UserId]));
        }

        public async Task UpdateProfileAvatar(Guid userId, byte[] imageData, string contentType)
        {
            var param = new
            {
                UserId = userId,
                Avatar = imageData,
                ContentType = contentType
            };

            await UsersFactory.ExecuteAsync("UpdateProfileAvatar.sql", param);
            TwCache.ClearCategory(TwCacheKey.Build(TwCache.Category.User, [userId]));
        }

        public async Task<WikiAdminPasswordChangeState> AdminPasswordStatus()
        {
            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Configuration);

            if (TwCache.Get<bool?>(cacheKey) == true)
            {
                return WikiAdminPasswordChangeState.HasBeenChanged;
            }

            var result = await UsersFactory.ExecuteScalarAsync<bool?>("IsAdminPasswordChanged.sql");
            if (result == true)
            {
                TwCache.Set(cacheKey, true);
                return WikiAdminPasswordChangeState.HasBeenChanged;
            }
            if (result == null)
            {
                return WikiAdminPasswordChangeState.NeedsToBeSet;
            }

            return WikiAdminPasswordChangeState.IsDefault;
        }

        public async Task SetAdminPasswordClear()
            => await UsersFactory.ExecuteScalarAsync<bool>("SetAdminPasswordClear.sql");

        public async Task SetAdminPasswordIsChanged()
            => await UsersFactory.ExecuteScalarAsync<bool>("SetAdminPasswordIsChanged.sql");

        public async Task SetAdminPasswordIsDefault()
            => await UsersFactory.ExecuteScalarAsync<bool>("SetAdminPasswordIsDefault.sql");

        #region Security.

        /// <summary>
        /// Detect whether this is the first time the WIKI has ever been run and do some initialization.
        /// Adds the first user with the email and password contained in Constants.DEFAULTUSERNAME and Constants.DEFAULTPASSWORD
        /// </summary>
        public async void ValidateEncryptionAndCreateAdminUser(UserManager<IdentityUser> userManager)
        {
            if (await _configurationRepository.IsFirstRun())
            {
                //If this is the first time the app has run on this machine (based on an encryption key) then clear the admin password status.
                //This will cause the application to set the admin password to the default password and display a warning until it is changed.
                await SetAdminPasswordClear();
            }

            if (await AdminPasswordStatus() == WikiAdminPasswordChangeState.NeedsToBeSet)
            {
                var user = await userManager.FindByNameAsync(TwConstants.DEFAULTUSERNAME);
                if (user == null)
                {
                    var creationResult = await userManager.CreateAsync(new IdentityUser(TwConstants.DEFAULTUSERNAME), TwConstants.DEFAULTPASSWORD);
                    if (!creationResult.Succeeded)
                    {
                        throw new Exception(string.Join("\r\n", creationResult.Errors.Select(o => o.Description)));
                    }

                    user = await userManager.FindByNameAsync(TwConstants.DEFAULTUSERNAME);
                }

                user.EnsureNotNull();

                user.Email = TwConstants.DEFAULTUSERNAME; // Ensure email is set or updated
                user.EmailConfirmed = true;
                var emailUpdateResult = await userManager.UpdateAsync(user);
                if (!emailUpdateResult.Succeeded)
                {
                    throw new Exception(string.Join("\r\n", emailUpdateResult.Errors.Select(o => o.Description)));
                }

                var membershipConfig = await _configurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Membership);

                var claimsToAdd = new List<Claim>
                    {
                        new (ClaimTypes.Role, "Administrator"),
                        new ("timezone", membershipConfig.Value<string>("Default TimeZone").EnsureNotNull()),
                        new (ClaimTypes.Country, membershipConfig.Value<string>("Default Country").EnsureNotNull()),
                        new ("language", membershipConfig.Value<string>("Default Language").EnsureNotNull()),
                    };

                await UpsertUserClaims(userManager, user, claimsToAdd);

                var token = await userManager.GeneratePasswordResetTokenAsync(user.EnsureNotNull());
                var result = await userManager.ResetPasswordAsync(user, token, TwConstants.DEFAULTPASSWORD);
                if (!result.Succeeded)
                {
                    throw new Exception(string.Join("\r\n", emailUpdateResult.Errors.Select(o => o.Description)));
                }

                await SetAdminPasswordIsDefault();

                var existingProfileUserId = GetUserAccountIdByNavigation(TwNavigation.Clean(TwConstants.DEFAULTACCOUNT));
                if (existingProfileUserId == null)
                {
                    await CreateProfile(Guid.Parse(user.Id), TwConstants.DEFAULTACCOUNT);
                }
                else
                {
                    await SetProfileUserId(TwConstants.DEFAULTACCOUNT, Guid.Parse(user.Id));
                }
            }
        }

        public async Task UpsertUserClaims(UserManager<IdentityUser> userManager, IdentityUser user, List<Claim> givenClaims)
        {
            // Get existing claims for the user
            var existingClaims = await userManager.GetClaimsAsync(user);

            foreach (var givenClaim in givenClaims)
            {
                // Remove existing claims if they exist
                var firstNameClaim = existingClaims.FirstOrDefault(c => c.Type == givenClaim.Type);
                if (firstNameClaim != null)
                {
                    await userManager.RemoveClaimAsync(user, firstNameClaim);
                }

                // Add new claim.
                await userManager.AddClaimAsync(user, givenClaim);
            }

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                throw new Exception(string.Join("<br />\r\n", result.Errors.Select(o => o.Description)));
            }
        }

        #endregion
    }
}
