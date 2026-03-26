using NTDLS.Helpers;
using TightWiki.Caching;
using TightWiki.Library;
using TightWiki.Models;
using TightWiki.Models.DataModels;

namespace TightWiki.Repository
{
    public static class UsersRepository
    {
        public static async Task<bool> IsAccountAMemberOfRole(Guid userId, int roleId, bool forceReCache = false)
        {
            var param = new
            {
                UserId = userId,
                RoleId = roleId
            };

            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Security, [userId, roleId]);

            return await WikiCache.AddOrGetAsync(cacheKey, forceReCache, async () =>
                await ManagedDataStorage.Users.QueryFirstOrDefaultAsync<bool?>("IsAccountAMemberOfRole.sql", param) ?? false
            );
        }

        public static async Task DeleteRole(int roleId)
            => await ManagedDataStorage.Users.ExecuteAsync("DeleteRole.sql", new { Id = roleId });

        public static async Task<bool> InsertRole(string name, string? description)
            => await ManagedDataStorage.Users.ExecuteScalarAsync<bool?>("InsertRole.sql", new { Name = name, Description = description }) ?? false;

        public static async Task<bool> DoesRoleExist(string name)
            => await ManagedDataStorage.Users.ExecuteScalarAsync<bool?>("DoesRoleExist.sql", new { Name = name }) ?? false;

        public static async Task<bool> IsAccountPermissionDefined(Guid userId, int permissionId, string permissionDispositionId, string? ns, string? pageId, bool forceReCache = true)
        {
            var param = new
            {
                UserId = userId,
                PermissionId = permissionId,
                PermissionDispositionId = permissionDispositionId,
                Namespace = ns,
                PageId = pageId
            };

            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Security, [userId, permissionId, permissionDispositionId, ns, pageId]);

            return await WikiCache.AddOrGetAsync(cacheKey, forceReCache, async () =>
                await ManagedDataStorage.Users.QueryFirstOrDefaultAsync<bool?>("IsAccountPermissionDefined.sql", param) ?? false
            );
        }

        public static async Task<InsertAccountPermissionResult?> InsertAccountPermission(
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
                return await o.QueryFirstOrDefaultAsync<InsertAccountPermissionResult>("InsertAccountPermission.sql", param);
            });
        }

        public static async Task<bool> IsRolePermissionDefined(int roleId, int permissionId, string permissionDispositionId, string? ns, string? pageId, bool forceReCache = false)
        {
            var param = new
            {
                RoleId = roleId,
                PermissionId = permissionId,
                PermissionDispositionId = permissionDispositionId,
                Namespace = ns,
                PageId = pageId
            };

            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Security, [roleId, permissionId, permissionDispositionId, ns, pageId]);

            return await WikiCache.AddOrGetAsync(cacheKey, forceReCache, async () =>
                await ManagedDataStorage.Users.QueryFirstOrDefaultAsync<bool?>("IsRolePermissionDefined.sql", param) ?? false
            );
        }

        public static async Task<List<Role>> AutoCompleteRole(string? searchText)
            => await ManagedDataStorage.Users.QueryAsync<Role>("AutoCompleteRole.sql", new { SearchText = searchText ?? string.Empty });

        public static async Task<List<AccountProfile>> AutoCompleteAccount(string? searchText)
            => await ManagedDataStorage.Users.QueryAsync<AccountProfile>("AutoCompleteAccount.sql", new { SearchText = searchText ?? string.Empty });

        public static async Task<AddRoleMemberResult?> AddRoleMemberByname(Guid userId, string roleName)
            => await ManagedDataStorage.Users.QueryFirstOrDefaultAsync<AddRoleMemberResult>("AddRoleMemberByname.sql", new { UserId = userId, RoleName = roleName });

        public static async Task<AddRoleMemberResult?> AddRoleMember(Guid userId, int roleId)
            => await ManagedDataStorage.Users.QueryFirstOrDefaultAsync<AddRoleMemberResult>("AddRoleMember.sql", new { UserId = userId, RoleId = roleId });

        public static async Task<AddAccountMembershipResult?> AddAccountMembership(Guid userId, int roleId)
            => await ManagedDataStorage.Users.QueryFirstOrDefaultAsync<AddAccountMembershipResult>("AddAccountMembership.sql", new { UserId = userId, RoleId = roleId });

        public static async Task RemoveRoleMember(int roleId, Guid userId)
            => await ManagedDataStorage.Users.ExecuteAsync("RemoveRoleMember.sql", new { RoleId = roleId, UserId = userId });

        public static async Task RemoveRolePermission(int id)
            => await ManagedDataStorage.Users.ExecuteAsync("RemoveRolePermission.sql", new { Id = id });

        public static async Task RemoveAccountPermission(int id)
            => await ManagedDataStorage.Users.ExecuteAsync("RemoveAccountPermission.sql", new { Id = id });

        public static async Task<InsertRolePermissionResult?> InsertRolePermission(
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
                return await o.QueryFirstOrDefaultAsync<InsertRolePermissionResult>("InsertRolePermission.sql", param);
            });
        }

        /// <summary>
        /// Gets the apparent account permissions for a user combined with the permissions of all roles that user is a member of.
        /// </summary>
        public static async Task<List<ApparentPermission>> GetApparentAccountPermissions(Guid userId)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Security, [userId]);

            return await WikiCache.AddOrGet(cacheKey, async () =>
            {
                return await ManagedDataStorage.Users.QueryAsync<ApparentPermission>(@"Scripts\GetApparentAccountPermissions.sql",
                new
                {
                    UserId = userId
                });
            }).EnsureNotNull();
        }

        public static async Task<List<ApparentPermission>> GetApparentRolePermissions(WikiRoles role)
            => await GetApparentRolePermissions(role.ToString());

        public static async Task<List<ApparentPermission>> GetApparentRolePermissions(string roleName)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Security, [roleName]);

            return (await WikiCache.AddOrGetAsync(cacheKey, async () =>
            {
                return await ManagedDataStorage.Users.QueryAsync<ApparentPermission>(@"Scripts\GetApparentRolePermissions.sql",
                new
                {
                    RoleName = roleName
                });
            })).EnsureNotNull();
        }

        public static async Task<List<PermissionDisposition>> GetAllPermissionDispositions()
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Security);

            return (await WikiCache.AddOrGetAsync(cacheKey, async () =>
            {
                return await ManagedDataStorage.Users.QueryAsync<PermissionDisposition>(@"Scripts\GetAllPermissionDispositions.sql");
            })).EnsureNotNull();
        }

        public static async Task<List<Permission>> GetAllPermissions()
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Security);

            return (await WikiCache.AddOrGetAsync(cacheKey, async () =>
            {
                return await ManagedDataStorage.Users.QueryAsync<Permission>(@"Scripts\GetAllPermissions.sql");
            })).EnsureNotNull();
        }

        public static async Task<List<RolePermission>> GetRolePermissionsPaged(int roleId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            return await ManagedDataStorage.Users.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("pages.db", "pages_db");

                pageSize ??= GlobalConfiguration.PaginationSize;

                var param = new
                {
                    PageSize = pageSize,
                    PageNumber = pageNumber,
                    RoleId = roleId
                };

                var query = RepositoryHelper.TransposeOrderby("GetRolePermissionsPaged.sql", orderBy, orderByDirection);
                return await o.QueryAsync<RolePermission>(query, param);
            });
        }

        public static async Task<List<AccountProfile>> GetAllPublicProfilesPaged(int pageNumber, int? pageSize = null, string? searchToken = null)
        {
            pageSize ??= GlobalConfiguration.PaginationSize;

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchToken = searchToken
            };

            return await ManagedDataStorage.Users.QueryAsync<AccountProfile>("GetAllPublicProfilesPaged.sql", param);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        public static async Task AnonymizeProfile(Guid userId)
        {
            string anonymousName = "DeletedUser_" + Utility.SanitizeAccountName($"{DateTime.UtcNow}", [' ']).Replace("_", "");

            var param = new
            {
                UserId = userId,
                ModifiedDate = DateTime.UtcNow,
                StandinName = anonymousName,
                Navigation = Navigation.Clean(anonymousName)
            };

            await ManagedDataStorage.Users.ExecuteAsync("AnonymizeProfile.sql", param);
        }

        public static async Task<bool> IsUserMemberOfAdministrators(Guid userId)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.User, [userId]);

            return await WikiCache.AddOrGetAsync(cacheKey, async () =>
            {
                var result = await ManagedDataStorage.Users.ExecuteScalarAsync<int?>("IsUserMemberOfAdministrators.sql",
                new
                {
                    UserId = userId
                });
                return result == 1;
            });
        }

        public static async Task<Role> GetRoleByName(string name)
        {
            var param = new
            {
                Name = name
            };

            return ManagedDataStorage.Users.QuerySingle<Role>("GetRoleByName.sql", param);
        }

        public static async Task<List<Role>> GetAllRoles(string? orderBy = null, string? orderByDirection = null)
        {
            var query = RepositoryHelper.TransposeOrderby("GetAllRoles.sql", orderBy, orderByDirection);
            return await ManagedDataStorage.Users.QueryAsync<Role>(query);
        }

        public static async Task<List<AccountProfile>> GetRoleMembersPaged(int roleId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            var param = new
            {
                RoleId = roleId,
                PageNumber = pageNumber,
                PageSize = GlobalConfiguration.PaginationSize
            };

            var query = RepositoryHelper.TransposeOrderby("GetRoleMembersPaged.sql", orderBy, orderByDirection);
            return await ManagedDataStorage.Users.QueryAsync<AccountProfile>(query, param);
        }

        public static async Task<List<AccountPermission>> GetAccountPermissionsPaged(Guid userId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            return await ManagedDataStorage.Users.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("pages.db", "pages_db");

                pageSize ??= GlobalConfiguration.PaginationSize;

                var param = new
                {
                    PageSize = pageSize,
                    PageNumber = pageNumber,
                    UserId = userId
                };

                var query = RepositoryHelper.TransposeOrderby("GetAccountPermissionsPaged.sql", orderBy, orderByDirection);
                return await o.QueryAsync<AccountPermission>(query, param);
            });
        }

        public static async Task<List<AccountRoleMembership>> GetAccountRoleMembershipPaged(Guid userId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            var param = new
            {
                UserId = userId,
                PageNumber = pageNumber,
                PageSize = GlobalConfiguration.PaginationSize
            };

            var query = RepositoryHelper.TransposeOrderby("GetAccountRoleMembershipPaged.sql", orderBy, orderByDirection);
            return await ManagedDataStorage.Users.QueryAsync<AccountRoleMembership>(query, param);
        }

        public static async Task<List<AccountProfile>> GetAllUsers()
            => await ManagedDataStorage.Users.QueryAsync<AccountProfile>("GetAllUsers.sql");

        public static async Task<List<AccountProfile>> GetAllUsersPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, string? searchToken = null)
        {
            var param = new
            {
                PageNumber = pageNumber,
                PageSize = GlobalConfiguration.PaginationSize,
                SearchToken = searchToken
            };

            var query = RepositoryHelper.TransposeOrderby("GetAllUsersPaged.sql", orderBy, orderByDirection);
            return await ManagedDataStorage.Users.QueryAsync<AccountProfile>(query, param);
        }

        public static async Task CreateProfile(Guid userId, string accountName)
        {
            if (await DoesProfileAccountExist(Navigation.Clean(accountName)))
            {
                throw new Exception("An account with that name already exists");
            }

            var param = new
            {
                UserId = userId,
                AccountName = accountName,
                Navigation = Navigation.Clean(accountName),
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            await ManagedDataStorage.Users.ExecuteAsync("CreateProfile.sql", param);
        }

        public static async Task<bool> DoesEmailAddressExist(string? emailAddress)
        {
            var param = new
            {
                EmailAddress = emailAddress?.ToLowerInvariant()
            };

            return (await ManagedDataStorage.Users.ExecuteScalarAsync<int?>("DoesEmailAddressExist.sql", param) ?? 0) != 0;
        }

        public static async Task<bool> DoesProfileAccountExist(string navigation)
        {
            var param = new
            {
                Navigation = navigation?.ToLowerInvariant()
            };

            return ((await ManagedDataStorage.Users.ExecuteScalarAsync<int?>("DoesProfileAccountExist.sql", param)) ?? 0) != 0;
        }

        public static async Task<AccountProfile?> GetBasicProfileByUserId(Guid userId)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.User, [userId]);
            return await WikiCache.AddOrGetAsync(cacheKey, async () =>
            {
                var param = new
                {
                    UserId = userId
                };

                return await ManagedDataStorage.Users.QuerySingleOrDefaultAsync<AccountProfile?>("GetBasicProfileByUserId.sql", param);
            });
        }

        public static async Task<AccountProfile> GetAccountProfileByUserId(Guid userId, bool forceReCache = false)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.User, [userId]);

            return (await WikiCache.AddOrGetAsync(cacheKey, forceReCache, async () =>
            {
                var param = new
                {
                    UserId = userId
                };

                return await ManagedDataStorage.Users.QuerySingleAsync<AccountProfile>("GetAccountProfileByUserId.sql", param);
            })).EnsureNotNull();
        }

        public static async Task SetProfileUserId(string navigation, Guid userId)
        {
            var param = new
            {
                Navigation = navigation,
                UserId = userId
            };

            await ManagedDataStorage.Users.ExecuteAsync("SetProfileUserId.sql", param);
        }

        public static async Task<Guid?> GetUserAccountIdByNavigation(string navigation)
        {
            var param = new
            {
                Navigation = navigation
            };

            return await ManagedDataStorage.Users.QueryFirstOrDefaultAsync<Guid>("GetUserAccountIdByNavigation.sql", param);
        }

        public static async Task<AccountProfile> GetAccountProfileByNavigation(string? navigation)
        {
            var param = new
            {
                Navigation = navigation
            };

            return await ManagedDataStorage.Users.QuerySingleAsync<AccountProfile>("GetAccountProfileByNavigation.sql", param);
        }

        public static async Task<AccountProfile?> GetProfileByAccountNameOrEmailAndPasswordHash(string accountNameOrEmail, string passwordHash)
        {
            var param = new
            {
                AccountNameOrEmail = accountNameOrEmail,
                PasswordHash = passwordHash
            };

            return await ManagedDataStorage.Users.QuerySingleAsync<AccountProfile>("GetProfileByAccountNameOrEmailAndPasswordHash.sql", param);
        }

        public static async Task<AccountProfile?> GetProfileByAccountNameOrEmailAndPassword(string accountNameOrEmail, string password)
        {
            string passwordHash = Security.Helpers.Sha256(password);
            var param = new
            {
                AccountNameOrEmail = accountNameOrEmail,
                PasswordHash = passwordHash
            };

            return await ManagedDataStorage.Users.QuerySingleAsync<AccountProfile>("GetProfileByAccountNameOrEmailAndPasswordHash.sql", param);
        }

        public static async Task<ProfileAvatar?> GetProfileAvatarByNavigation(string navigation)
            => await ManagedDataStorage.Users.QuerySingleOrDefaultAsync<ProfileAvatar>("GetProfileAvatarByNavigation.sql", new { Navigation = navigation });

        public static async Task UpdateProfile(AccountProfile item)
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
            WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.User, [item.UserId]));
        }

        public static async Task UpdateProfileAvatar(Guid userId, byte[] imageData, string contentType)
        {
            var param = new
            {
                UserId = userId,
                Avatar = imageData,
                ContentType = contentType
            };

            await ManagedDataStorage.Users.ExecuteAsync("UpdateProfileAvatar.sql", param);
            WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.User, [userId]));
        }

        public static async Task<WikiAdminPasswordChangeState> AdminPasswordStatus()
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Configuration);

            if (WikiCache.Get<bool?>(cacheKey) == true)
            {
                return WikiAdminPasswordChangeState.HasBeenChanged;
            }

            var result = await ManagedDataStorage.Users.ExecuteScalarAsync<bool?>("IsAdminPasswordChanged.sql");
            if (result == true)
            {
                WikiCache.Set(cacheKey, true);
                return WikiAdminPasswordChangeState.HasBeenChanged;
            }
            if (result == null)
            {
                return WikiAdminPasswordChangeState.NeedsToBeSet;
            }

            return WikiAdminPasswordChangeState.IsDefault;
        }

        public static async Task SetAdminPasswordClear()
            => await ManagedDataStorage.Users.ExecuteScalarAsync<bool>("SetAdminPasswordClear.sql");

        public static async Task SetAdminPasswordIsChanged()
            => await ManagedDataStorage.Users.ExecuteScalarAsync<bool>("SetAdminPasswordIsChanged.sql");

        public static async Task SetAdminPasswordIsDefault()
            => await ManagedDataStorage.Users.ExecuteScalarAsync<bool>("SetAdminPasswordIsDefault.sql");
    }
}
