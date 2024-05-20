using TightWiki.DataModels;
using TightWiki.DataStorage;
using TightWiki.Library;

namespace TightWiki.Repository
{
    public static class ProfileRepository
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

            return ManagedDataStorage.Default.Query<AccountProfile>("GetAllPublicProfilesPaged", param).ToList();
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

            ManagedDataStorage.Default.Execute("AnonymizeProfile", param);
        }

        public static Role GetRoleByName(string name)
        {
            var param = new
            {
                Name = name
            };

            return ManagedDataStorage.Default.QuerySingle<Role>("GetRoleByName", param);
        }

        public static List<Role> GetAllRoles()
        {
            return ManagedDataStorage.Default.Query<Role>("GetAllRoles").ToList();
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

            return ManagedDataStorage.Default.Query<AccountProfile>("GetProfilesByRoleIdPaged", param).ToList();
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

            return ManagedDataStorage.Default.Query<AccountProfile>("GetAllUsersPaged", param).ToList();
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

            return ManagedDataStorage.Default.ExecuteScalar<int>("CreateProfile", param);
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

            ManagedDataStorage.Default.Execute("CreateProfile", param);
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

            return (ManagedDataStorage.Default.ExecuteScalar<int?>("DoesEmailAddressExist", param) ?? 0) != 0;
        }

        public static bool DoesProfileAccountExist(string navigation)
        {
            var param = new
            {
                Navigation = navigation?.ToLower()
            };

            return (ManagedDataStorage.Default.ExecuteScalar<int?>("DoesProfileAccountExist", param) ?? 0) != 0;
        }

        public static AccountProfile GetBasicProfileByUserId(Guid userId)
        {
            var param = new
            {
                UserId = userId
            };

            return ManagedDataStorage.Default.QuerySingle<AccountProfile>("GetBasicProfileByUserId", param);
        }

        public static AccountProfile GetAccountProfileByUserId(Guid userId)
        {
            var param = new
            {
                UserId = userId
            };

            return ManagedDataStorage.Default.QuerySingle<AccountProfile>("GetAccountProfileByUserId", param);
        }

        public static void SetProfileUserId(string navigation, Guid userId)
        {
            var param = new
            {
                Navigation = navigation,
                UserId = userId
            };

            ManagedDataStorage.Default.Execute("SetProfileUserId", param);
        }

        public static Guid? GetUserAccountIdByNavigation(string navigation)
        {
            var param = new
            {
                Navigation = navigation
            };

            return ManagedDataStorage.Default.QueryFirstOrDefault<Guid>("GetUserAccountIdByNavigation", param);
        }

        public static AccountProfile GetAccountProfileByNavigation(string? navigation)
        {
            var param = new
            {
                Navigation = navigation
            };

            return ManagedDataStorage.Default.QuerySingle<AccountProfile>("GetAccountProfileByNavigation", param);
        }

        public static AccountProfile? GetProfileByAccountNameOrEmailAndPasswordHash(string accountNameOrEmail, string passwordHash)
        {
            var param = new
            {
                AccountNameOrEmail = accountNameOrEmail,
                PasswordHash = passwordHash
            };

            return ManagedDataStorage.Default.QuerySingle<AccountProfile>("GetProfileByAccountNameOrEmailAndPasswordHash", param);
        }

        public static AccountProfile? GetProfileByAccountNameOrEmailAndPassword(string accountNameOrEmail, string password)
        {
            string passwordHash = Library.Security.Sha256(password);
            var param = new
            {
                AccountNameOrEmail = accountNameOrEmail,
                PasswordHash = passwordHash
            };

            return ManagedDataStorage.Default.QuerySingle<AccountProfile>("GetProfileByAccountNameOrEmailAndPasswordHash", param);
        }

        public static byte[]? GetProfileAvatarByNavigation(string navigation)
        {
            var param = new
            {
                Navigation = navigation
            };

            return ManagedDataStorage.Default.QuerySingle<byte[]>("GetProfileAvatarByNavigation", param);
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

            ManagedDataStorage.Default.Execute("UpdateProfile", param);
        }

        public static void UpdateProfileAvatar(Guid userId, byte[] imageData)
        {
            var param = new
            {
                UserId = userId,
                Avatar = imageData,
            };

            ManagedDataStorage.Default.Execute("UpdateProfileAvatar", param);
        }
    }
}
