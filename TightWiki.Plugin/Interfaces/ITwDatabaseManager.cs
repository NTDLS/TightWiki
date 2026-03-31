using Microsoft.AspNetCore.Identity;
using NTDLS.SqliteDapperWrapper;
using TightWiki.Plugin.Interfaces.Repository;

namespace TightWiki.Plugin.Interfaces
{
    public interface ITwDatabaseManager
    {
        (string Name, SqliteManagedFactory Factory)[] Databases { get; }

        ITwConfigurationRepository ConfigurationRepository { get; }
        ITwDefaultsRepository DefaultsRepository { get; }
        ITwEmojiRepository EmojiRepository { get; }
        ITwLoggingRepository LoggingRepository { get; }
        ITwPageRepository PageRepository { get; }
        ITwStatisticsRepository StatisticsRepository { get; }
        ITwUsersRepository UsersRepository { get; }

        Task ApplyAllSeedData(ITwSharedLocalizationText localizer, UserManager<IdentityUser> userManager, ITwEngine tightEngine, TwDefaultDataType[] defaultDataTypes);

        #region Database admin.

        Task<string> VacuumDatabase(string databaseName);
        Task<string> OptimizeDatabase(string databaseName);
        Task<string> IntegrityCheckDatabase(string databaseName);
        Task<string> ForeignKeyCheck(string databaseName);
        Task<List<(string Name, string Version)>> GetDatabaseVersions();
        Task<List<(string Name, int PageCount)>> GetDatabasePageCounts();
        Task<List<(string Name, int PageSize)>> GetDatabasePageSizes();

        #endregion
    }
}