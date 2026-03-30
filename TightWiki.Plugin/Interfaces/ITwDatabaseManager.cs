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
    }
}