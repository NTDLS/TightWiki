using NTDLS.Helpers;
using TightWiki.Plugin;
using TightWiki.Plugin.Caching;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Library;
using TightWiki.Plugin.Models;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Repository
{
    public class UsersRepository(ITwConfigurationRepository configurationRepository)
        : IUsersRepository
    {
        public async Task<bool> IsAccountAMemberOfRole(Guid userId, int roleId, bool forceReCache = false)
        {
            var param = new
            {
                UserId = userId,
                RoleId = roleId
            };

            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Security, [userId, roleId]);

            return await TwCache.AddOrGetAsync(cacheKey, forceReCache, async () =>
                await ManagedDataStorage.Users.QueryFirstOrDefaultAsync<bool?>("IsAccountAMemberOfRole.sql", param) ?? false
            );
        }

        public async Task DeleteRole(int roleId)
            => await ManagedDataStorage.Users.ExecuteAsync("DeleteRole.sql", new { Id = roleId });

        public async Task<bool> InsertRole(string name, string? description)
            => await ManagedDataStorage.Users.ExecuteScalarAsync<bool?>("InsertRole.sql", new { Name = name, Description = description }) ?? false;

        public async Task<bool> DoesRoleExist(string name)
            => await ManagedDataStorage.Users.ExecuteScalarAsync<bool?>("DoesRoleExist.sql", new { Name = name }) ?? false;

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
                await ManagedDataStorage.Users.QueryFirstOrDefaultAsync<bool?>("IsAccountPermissionDefined.sql", param) ?? false
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

            return await ManagedDataStorage.Users.EphemeralAsync(async o =>
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
                await ManagedDataStorage.Users.QueryFirstOrDefaultAsync<bool?>("IsRolePermissionDefined.sql", param) ?? false
            );
        }

        public async Task<List<TwRole>> AutoCompleteRole(string? searchText)
            => await ManagedDataStorage.Users.QueryAsync<TwRole>("AutoCompleteRole.sql", new { SearchText = searchText ?? string.Empty });

        public async Task<List<TwAccountProfile>> AutoCompleteAccount(string? searchText)
            => await ManagedDataStorage.Users.QueryAsync<TwAccountProfile>("AutoCompleteAccount.sql", new { SearchText = searchText ?? string.Empty });

        public async Task<TwAddRoleMemberResult?> AddRoleMemberByname(Guid userId, string roleName)
            => await ManagedDataStorage.Users.QueryFirstOrDefaultAsync<TwAddRoleMemberResult>("AddRoleMemberByname.sql", new { UserId = userId, RoleName = roleName });

        public async Task<TwAddRoleMemberResult?> AddRoleMember(Guid userId, int roleId)
            => await ManagedDataStorage.Users.QueryFirstOrDefaultAsync<TwAddRoleMemberResult>("AddRoleMember.sql", new { UserId = userId, RoleId = roleId });

        public async Task<TwAddAccountMembershipResult?> AddAccountMembership(Guid userId, int roleId)
            => await ManagedDataStorage.Users.QueryFirstOrDefaultAsync<TwAddAccountMembershipResult>("AddAccountMembership.sql", new { UserId = userId, RoleId = roleId });

        public async Task RemoveRoleMember(int roleId, Guid userId)
            => await ManagedDataStorage.Users.ExecuteAsync("RemoveRoleMember.sql", new { RoleId = roleId, UserId = userId });

        public async Task RemoveRolePermission(int id)
            => await ManagedDataStorage.Users.ExecuteAsync("RemoveRolePermission.sql", new { Id = id });

        public async Task RemoveAccountPermission(int id)
            => await ManagedDataStorage.Users.ExecuteAsync("RemoveAccountPermission.sql", new { Id = id });

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

            return await ManagedDataStorage.Users.EphemeralAsync(async o =>
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
                return await ManagedDataStorage.Users.QueryAsync<TwApparentPermission>(@"Scripts\GetApparentAccountPermissions.sql",
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
                return await ManagedDataStorage.Users.QueryAsync<TwApparentPermission>(@"Scripts\GetApparentRolePermissions.sql",
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
                return await ManagedDataStorage.Users.QueryAsync<TwPermissionDisposition>(@"Scripts\GetAllPermissionDispositions.sql");
            })).EnsureNotNull();
        }

        public async Task<List<TwPermission>> GetAllPermissions()
        {
            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Security);

            return (await TwCache.AddOrGetAsync(cacheKey, async () =>
            {
                return await ManagedDataStorage.Users.QueryAsync<TwPermission>(@"Scripts\GetAllPermissions.sql");
            })).EnsureNotNull();
        }

        public async Task<List<TwRolePermission>> GetRolePermissionsPaged(int roleId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            return await ManagedDataStorage.Users.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("pages.db", "pages_db");

                pageSize ??= await configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

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
            pageSize ??= await configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchToken = searchToken
            };

            return await ManagedDataStorage.Users.QueryAsync<TwAccountProfile>("GetAllPublicProfilesPaged.sql", param);
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

            await ManagedDataStorage.Users.ExecuteAsync("AnonymizeProfile.sql", param);
        }

        public async Task<bool> IsUserMemberOfAdministrators(Guid userId)
        {
            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.User, [userId]);

            return await TwCache.AddOrGetAsync(cacheKey, async () =>
            {
                var result = await ManagedDataStorage.Users.ExecuteScalarAsync<int?>("IsUserMemberOfAdministrators.sql",
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

            return ManagedDataStorage.Users.QuerySingle<TwRole>("GetRoleByName.sql", param);
        }

        public async Task<List<TwRole>> GetAllRoles(string? orderBy = null, string? orderByDirection = null)
        {
            var query = RepositoryHelpers.TransposeOrderby("GetAllRoles.sql", orderBy, orderByDirection);
            return await ManagedDataStorage.Users.QueryAsync<TwRole>(query);
        }

        public async Task<List<TwAccountProfile>> GetRoleMembersPaged(int roleId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            var paginationSize = await configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                RoleId = roleId,
                PageNumber = pageNumber,
                PageSize = paginationSize
            };

            var query = RepositoryHelpers.TransposeOrderby("GetRoleMembersPaged.sql", orderBy, orderByDirection);
            return await ManagedDataStorage.Users.QueryAsync<TwAccountProfile>(query, param);
        }

        public async Task<List<TwAccountPermission>> GetAccountPermissionsPaged(Guid userId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            return await ManagedDataStorage.Users.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("pages.db", "pages_db");

                pageSize ??= await configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

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
            var paginationSize = await configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                UserId = userId,
                PageNumber = pageNumber,
                PageSize = paginationSize
            };

            var query = RepositoryHelpers.TransposeOrderby("GetAccountRoleMembershipPaged.sql", orderBy, orderByDirection);
            return await ManagedDataStorage.Users.QueryAsync<TwAccountRoleMembership>(query, param);
        }

        public async Task<List<TwAccountProfile>> GetAllUsers()
            => await ManagedDataStorage.Users.QueryAsync<TwAccountProfile>("GetAllUsers.sql");

        public async Task<List<TwAccountProfile>> GetAllUsersPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, string? searchToken = null)
        {
            var paginationSize = await configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = paginationSize,
                SearchToken = searchToken
            };

            var query = RepositoryHelpers.TransposeOrderby("GetAllUsersPaged.sql", orderBy, orderByDirection);
            return await ManagedDataStorage.Users.QueryAsync<TwAccountProfile>(query, param);
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

            await ManagedDataStorage.Users.ExecuteAsync("CreateProfile.sql", param);
        }

        public async Task<bool> DoesEmailAddressExist(string? emailAddress)
        {
            var param = new
            {
                EmailAddress = emailAddress?.ToLowerInvariant()
            };

            return (await ManagedDataStorage.Users.ExecuteScalarAsync<int?>("DoesEmailAddressExist.sql", param) ?? 0) != 0;
        }

        public async Task<bool> DoesProfileAccountExist(string navigation)
        {
            var param = new
            {
                Navigation = navigation?.ToLowerInvariant()
            };

            return ((await ManagedDataStorage.Users.ExecuteScalarAsync<int?>("DoesProfileAccountExist.sql", param)) ?? 0) != 0;
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

                return await ManagedDataStorage.Users.QuerySingleOrDefaultAsync<TwAccountProfile?>("GetBasicProfileByUserId.sql", param);
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

                return await ManagedDataStorage.Users.QuerySingleAsync<TwAccountProfile>("GetAccountProfileByUserId.sql", param);
            })).EnsureNotNull();
        }

        public async Task SetProfileUserId(string navigation, Guid userId)
        {
            var param = new
            {
                Navigation = navigation,
                UserId = userId
            };

            await ManagedDataStorage.Users.ExecuteAsync("SetProfileUserId.sql", param);
        }

        public async Task<Guid?> GetUserAccountIdByNavigation(string navigation)
        {
            var param = new
            {
                Navigation = navigation
            };

            return await ManagedDataStorage.Users.QueryFirstOrDefaultAsync<Guid>("GetUserAccountIdByNavigation.sql", param);
        }

        public async Task<TwAccountProfile> GetAccountProfileByNavigation(string? navigation)
        {
            var param = new
            {
                Navigation = navigation
            };

            return await ManagedDataStorage.Users.QuerySingleAsync<TwAccountProfile>("GetAccountProfileByNavigation.sql", param);
        }

        public async Task<TwAccountProfile?> GetProfileByAccountNameOrEmailAndPasswordHash(string accountNameOrEmail, string passwordHash)
        {
            var param = new
            {
                AccountNameOrEmail = accountNameOrEmail,
                PasswordHash = passwordHash
            };

            return await ManagedDataStorage.Users.QuerySingleAsync<TwAccountProfile>("GetProfileByAccountNameOrEmailAndPasswordHash.sql", param);
        }

        public async Task<TwAccountProfile?> GetProfileByAccountNameOrEmailAndPassword(string accountNameOrEmail, string password)
        {
            string passwordHash = Security.Helpers.Sha256(password);
            var param = new
            {
                AccountNameOrEmail = accountNameOrEmail,
                PasswordHash = passwordHash
            };

            return await ManagedDataStorage.Users.QuerySingleAsync<TwAccountProfile>("GetProfileByAccountNameOrEmailAndPasswordHash.sql", param);
        }

        public async Task<TwProfileAvatar?> GetProfileAvatarByNavigation(string navigation)
            => await ManagedDataStorage.Users.QuerySingleOrDefaultAsync<TwProfileAvatar>("GetProfileAvatarByNavigation.sql", new { Navigation = navigation });

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

            await ManagedDataStorage.Users.ExecuteAsync("UpdateProfile.sql", param);
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

            await ManagedDataStorage.Users.ExecuteAsync("UpdateProfileAvatar.sql", param);
            TwCache.ClearCategory(TwCacheKey.Build(TwCache.Category.User, [userId]));
        }

        public async Task<WikiAdminPasswordChangeState> AdminPasswordStatus()
        {
            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Configuration);

            if (TwCache.Get<bool?>(cacheKey) == true)
            {
                return WikiAdminPasswordChangeState.HasBeenChanged;
            }

            var result = await ManagedDataStorage.Users.ExecuteScalarAsync<bool?>("IsAdminPasswordChanged.sql");
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
            => await ManagedDataStorage.Users.ExecuteScalarAsync<bool>("SetAdminPasswordClear.sql");

        public async Task SetAdminPasswordIsChanged()
            => await ManagedDataStorage.Users.ExecuteScalarAsync<bool>("SetAdminPasswordIsChanged.sql");

        public async Task SetAdminPasswordIsDefault()
            => await ManagedDataStorage.Users.ExecuteScalarAsync<bool>("SetAdminPasswordIsDefault.sql");
    }
}
