using TightWiki.Library;
using TightWiki.Models.DataModels;
using static TightWiki.Library.Constants;

namespace TightWiki.Repository
{
    public static class UsersRepository
    {
        public static List<AccountProfile> GetAllPublicProfilesPaged(int pageNumber, int? pageSize = null, string? searchToken = null)
        {
            pageSize ??= ConfigurationRepository.Get<int>("Customization", "Pagination Size");

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
            string standinName = GetRandomUnusedAccountName();

            var param = new
            {
                UserId = userId,
                ModifiedDate = DateTime.UtcNow,
                StandinName = standinName,
                Navigation = Navigation.Clean(standinName)
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

        public static List<Role> GetAllRoles()
        {
            return ManagedDataStorage.Users.Query<Role>("GetAllRoles.sql").ToList();
        }

        public static List<AccountProfile> GetProfilesByRoleIdPaged(int roleId, int pageNumber, int? pageSize = null)
        {
            pageSize ??= ConfigurationRepository.Get<int>("Customization", "Pagination Size");

            var param = new
            {
                RoleId = roleId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return ManagedDataStorage.Users.Query<AccountProfile>("GetProfilesByRoleIdPaged.sql", param).ToList();
        }

        public static List<AccountProfile> GetAllUsersPaged(int pageNumber, int? pageSize = null, string? searchToken = null)
        {
            pageSize ??= ConfigurationRepository.Get<int>("Customization", "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchToken = searchToken
            };

            return ManagedDataStorage.Users.Query<AccountProfile>("GetAllUsersPaged.sql", param).ToList();
        }

        public static int CreateProfile(Guid userId)
        {
            var randomAccountName = GetRandomUnusedAccountName();

            var param = new
            {
                UserId = userId,
                AccountName = randomAccountName,
                Navigation = Navigation.Clean(randomAccountName),
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            return ManagedDataStorage.Users.ExecuteScalar<int>("CreateProfile.sql", param);
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

        public static string GetRandomUnusedAccountName()
        {
            while (true)
            {
                var randomAccountName = string.Join(" ", WordsRepository.GetRandomWords(2));
                if (DoesProfileAccountExist(Navigation.Clean(randomAccountName)) == false)
                {
                    return randomAccountName;
                }
            }
        }

        public static bool DoesEmailAddressExist(string? emailAddress)
        {
            var param = new
            {
                EmailAddress = emailAddress?.ToLower()
            };

            return (ManagedDataStorage.Users.ExecuteScalar<int?>("DoesEmailAddressExist.sql", param) ?? 0) != 0;
        }

        public static bool DoesProfileAccountExist(string navigation)
        {
            var param = new
            {
                Navigation = navigation?.ToLower()
            };

            return (ManagedDataStorage.Users.ExecuteScalar<int?>("DoesProfileAccountExist.sql", param) ?? 0) != 0;
        }

        public static AccountProfile GetBasicProfileByUserId(Guid userId)
        {
            var param = new
            {
                UserId = userId
            };

            return ManagedDataStorage.Users.QuerySingle<AccountProfile>("GetBasicProfileByUserId.sql", param);
        }

        public static AccountProfile GetAccountProfileByUserId(Guid userId)
        {
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
            string passwordHash = Library.Security.Sha256(password);
            var param = new
            {
                AccountNameOrEmail = accountNameOrEmail,
                PasswordHash = passwordHash
            };

            return ManagedDataStorage.Users.QuerySingle<AccountProfile>("GetProfileByAccountNameOrEmailAndPasswordHash.sql", param);
        }

        public static byte[]? GetProfileAvatarByNavigation(string navigation)
        {
            var param = new
            {
                Navigation = navigation
            };

            return ManagedDataStorage.Users.QuerySingle<byte[]>("GetProfileAvatarByNavigation.sql", param);
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

        public static void UpdateProfileAvatar(Guid userId, byte[] imageData)
        {
            var param = new
            {
                UserId = userId,
                Avatar = imageData,
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
        {
            ManagedDataStorage.Users.ExecuteScalar<bool>("SetAdminPasswordClear.sql");
        }
        public static void SetAdminPasswordIsChanged()
        {
            ManagedDataStorage.Users.ExecuteScalar<bool>("SetAdminPasswordIsChanged.sql");
        }

        public static void SetAdminPasswordIsDefault()
        {
            ManagedDataStorage.Users.ExecuteScalar<bool>("SetAdminPasswordIsDefault.sql");
        }
    }
}
