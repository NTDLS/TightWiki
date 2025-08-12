using System.Diagnostics.CodeAnalysis;
using TightWiki.Caching;
using TightWiki.Library;
using TightWiki.Models;
using TightWiki.Models.DataModels;
using static TightWiki.Library.Constants;

namespace TightWiki.Repository
{
    public static class UsersRepository
    {
        public static List<AccountProfile> GetAllPublicProfilesPaged(int pageNumber, int? pageSize = null, string? searchToken = null)
        {
            pageSize ??= GlobalConfiguration.PaginationSize;

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchToken = searchToken
            };

            return ManagedDataStorage.Users.Query<AccountProfile>("GetAllPublicProfilesPaged.sql", param).ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        public static void AnonymizeProfile(Guid userId)
        {
            string anonymousName = "DeletedUser_" + Utility.SanitizeAccountName($"{DateTime.UtcNow}", [' ']).Replace("_", "");

            var param = new
            {
                UserId = userId,
                ModifiedDate = DateTime.UtcNow,
                StandinName = anonymousName,
                Navigation = Navigation.Clean(anonymousName)
            };

            ManagedDataStorage.Users.Execute("AnonymizeProfile.sql", param);
        }

        public static Role GetRoleByName(string name)
        {
            var param = new
            {
                Name = name
            };

            return ManagedDataStorage.Users.QuerySingle<Role>("GetRoleByName.sql", param);
        }

        public static List<Role> GetAllRoles(string? orderBy = null, string? orderByDirection = null)
        {
            var query = RepositoryHelper.TransposeOrderby("GetAllRoles.sql", orderBy, orderByDirection);
            return ManagedDataStorage.Users.Query<Role>(query).ToList();
        }

        public static List<AccountProfile> GetProfilesByRoleIdPaged(int roleId, int pageNumber)
        {
            var param = new
            {
                RoleId = roleId,
                PageNumber = pageNumber,
                PageSize = GlobalConfiguration.PaginationSize
            };

            return ManagedDataStorage.Users.Query<AccountProfile>("GetProfilesByRoleIdPaged.sql", param).ToList();
        }

        public static List<AccountProfile> GetAllUsers()
            => ManagedDataStorage.Users.Query<AccountProfile>("GetAllUsers.sql").ToList();

        public static List<AccountProfile> GetAllUsersPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, string? searchToken = null)
        {
            var param = new
            {
                PageNumber = pageNumber,
                PageSize = GlobalConfiguration.PaginationSize,
                SearchToken = searchToken
            };

            var query = RepositoryHelper.TransposeOrderby("GetAllUsersPaged.sql", orderBy, orderByDirection);
            return ManagedDataStorage.Users.Query<AccountProfile>(query, param).ToList();
        }

        public static void CreateProfile(Guid userId, string accountName)
        {
            if (DoesProfileAccountExist(Navigation.Clean(accountName)))
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

            ManagedDataStorage.Users.Execute("CreateProfile.sql", param);
        }

        public static bool DoesEmailAddressExist(string? emailAddress)
        {
            var param = new
            {
                EmailAddress = emailAddress?.ToLowerInvariant()
            };

            return (ManagedDataStorage.Users.ExecuteScalar<int?>("DoesEmailAddressExist.sql", param) ?? 0) != 0;
        }

        public static bool DoesProfileAccountExist(string navigation)
        {
            var param = new
            {
                Navigation = navigation?.ToLowerInvariant()
            };

            return (ManagedDataStorage.Users.ExecuteScalar<int?>("DoesProfileAccountExist.sql", param) ?? 0) != 0;
        }

        public static bool TryGetBasicProfileByUserId(Guid userId,
            [NotNullWhen(true)] out AccountProfile? accountProfile, bool allowCache = true)
        {
            if (allowCache)
            {
                var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.User, [userId]);
                if (!WikiCache.TryGet<AccountProfile>(cacheKey, out accountProfile))
                {
                    if (TryGetBasicProfileByUserId(userId, out accountProfile, false))
                    {
                        WikiCache.Put(cacheKey, accountProfile);
                        return true;
                    }
                }

                if (accountProfile != null)
                {
                    return true;
                }
            }

            var param = new
            {
                UserId = userId
            };

            accountProfile = ManagedDataStorage.Users.QuerySingleOrDefault<AccountProfile>("GetBasicProfileByUserId.sql", param);
            return accountProfile != null;
        }

        public static AccountProfile GetBasicProfileByUserId(Guid userId, bool allowCache = true)
        {
            if (allowCache)
            {
                var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.User, [userId]);
                if (!WikiCache.TryGet<AccountProfile>(cacheKey, out var result))
                {
                    result = GetBasicProfileByUserId(userId, false);
                    WikiCache.Put(cacheKey, result);
                }

                return result;
            }

            var param = new
            {
                UserId = userId
            };

            return ManagedDataStorage.Users.QuerySingle<AccountProfile>("GetBasicProfileByUserId.sql", param);
        }

        public static AccountProfile GetAccountProfileByUserId(Guid userId, bool allowCache = true)
        {
            if (allowCache)
            {
                var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.User, [userId]);
                if (!WikiCache.TryGet<AccountProfile>(cacheKey, out var result))
                {
                    result = GetAccountProfileByUserId(userId, false);
                    WikiCache.Put(cacheKey, result);
                }

                return result;
            }

            var param = new
            {
                UserId = userId
            };

            return ManagedDataStorage.Users.QuerySingle<AccountProfile>("GetAccountProfileByUserId.sql", param);
        }

        public static void SetProfileUserId(string navigation, Guid userId)
        {
            var param = new
            {
                Navigation = navigation,
                UserId = userId
            };

            ManagedDataStorage.Users.Execute("SetProfileUserId.sql", param);
        }

        public static Guid? GetUserAccountIdByNavigation(string navigation)
        {
            var param = new
            {
                Navigation = navigation
            };

            return ManagedDataStorage.Users.QueryFirstOrDefault<Guid>("GetUserAccountIdByNavigation.sql", param);
        }

        public static AccountProfile GetAccountProfileByNavigation(string? navigation)
        {
            var param = new
            {
                Navigation = navigation
            };

            return ManagedDataStorage.Users.QuerySingle<AccountProfile>("GetAccountProfileByNavigation.sql", param);
        }

        public static bool TryGetAccountProfileByNavigation(string? navigation, [NotNullWhen(true)] out AccountProfile? accountProfile)
        {
            var param = new
            {
                Navigation = navigation
            };
            accountProfile = ManagedDataStorage.Users.QuerySingleOrDefault<AccountProfile>("GetAccountProfileByNavigation.sql", param);

            return accountProfile != null;
        }

        public static AccountProfile? GetProfileByAccountNameOrEmailAndPasswordHash(string accountNameOrEmail, string passwordHash)
        {
            var param = new
            {
                AccountNameOrEmail = accountNameOrEmail,
                PasswordHash = passwordHash
            };

            return ManagedDataStorage.Users.QuerySingle<AccountProfile>("GetProfileByAccountNameOrEmailAndPasswordHash.sql", param);
        }

        public static AccountProfile? GetProfileByAccountNameOrEmailAndPassword(string accountNameOrEmail, string password)
        {
            string passwordHash = Security.Helpers.Sha256(password);
            var param = new
            {
                AccountNameOrEmail = accountNameOrEmail,
                PasswordHash = passwordHash
            };

            return ManagedDataStorage.Users.QuerySingle<AccountProfile>("GetProfileByAccountNameOrEmailAndPasswordHash.sql", param);
        }

        public static ProfileAvatar GetProfileAvatarByNavigation(string navigation)
        {
            var param = new
            {
                Navigation = navigation
            };

            return ManagedDataStorage.Users.QuerySingle<ProfileAvatar>("GetProfileAvatarByNavigation.sql", param);
        }

        public static void UpdateProfile(AccountProfile item)
        {
            var param = new
            {
                UserId = item.UserId,
                AccountName = item.AccountName,
                Navigation = item.Navigation,
                Biography = item.Biography,
                ModifiedDate = item.ModifiedDate
            };

            ManagedDataStorage.Users.Execute("UpdateProfile.sql", param);
        }

        public static void UpdateProfileAvatar(Guid userId, byte[] imageData, string contentType)
        {
            var param = new
            {
                UserId = userId,
                Avatar = imageData,
                ContentType = contentType
            };

            ManagedDataStorage.Users.Execute("UpdateProfileAvatar.sql", param);
        }

        public static AdminPasswordChangeState AdminPasswordStatus()
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Configuration);

            if (WikiCache.Get<bool?>(cacheKey) == true)
            {
                return AdminPasswordChangeState.HasBeenChanged;
            }

            var result = ManagedDataStorage.Users.ExecuteScalar<bool?>("IsAdminPasswordChanged.sql");
            if (result == true)
            {
                WikiCache.Put(cacheKey, true);
                return AdminPasswordChangeState.HasBeenChanged;
            }
            if (result == null)
            {
                return AdminPasswordChangeState.NeedsToBeSet;
            }

            return AdminPasswordChangeState.IsDefault;
        }

        public static void SetAdminPasswordClear()
            => ManagedDataStorage.Users.ExecuteScalar<bool>("SetAdminPasswordClear.sql");

        public static void SetAdminPasswordIsChanged()
            => ManagedDataStorage.Users.ExecuteScalar<bool>("SetAdminPasswordIsChanged.sql");


        public static void SetAdminPasswordIsDefault()
            => ManagedDataStorage.Users.ExecuteScalar<bool>("SetAdminPasswordIsDefault.sql");
    }
}
