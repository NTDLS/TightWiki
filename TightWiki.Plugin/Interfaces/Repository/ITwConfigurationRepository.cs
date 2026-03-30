using NTDLS.SqliteDapperWrapper;
using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Interfaces.Repository
{
    public interface ITwConfigurationRepository
    {
        SqliteManagedFactory ConfigFactory { get; }

        Task<TwConfigurationEntries> GetConfigurationEntryValuesByGroupName(string groupName);

        Task<List<TwTheme>> GetAllThemes();

        Task<TwWikiDatabaseStatistics> GetWikiDatabaseMetrics();

        /// <summary>
        /// Determines if this is the first time the wiki has run. Returns true if it is the first time.
        /// </summary>
        Task<bool> IsFirstRun();

        /// <summary>
        /// Reads an encrypted value from the database so we can determine if encryption is setup.
        /// If the value is missing then we are NOT setup.
        /// If the value is present but we cant decrypt it, then we are NOT setup.
        /// If the value is present and we can decrypt it, then we are setup and good to go!
        /// </summary>
        Task<bool> GetCryptoCheck();

        /// <summary>
        /// Writes an encrypted value to the database so we can test at a later time to ensure that encryption is setup.
        /// </summary>
        Task SetCryptoCheck();

        Task SaveConfigurationEntryValueByGroupAndEntry(string groupName, string entryName, string value);
        Task<List<TwConfigurationNest>> GetConfigurationNest();

        Task<List<TwConfigurationFlat>> GetFlatConfiguration();

        Task<string?> GetConfigurationEntryValuesByGroupNameAndEntryName(string groupName, string entryName);

        Task<T?> Get<T>(string groupName, string entryName);

        Task<T> Get<T>(string groupName, string entryName, T defaultValue);

        #region Menu Items.

        Task<List<TwMenuItem>> GetAllMenuItems(string? orderBy = null, string? orderByDirection = null);

        Task<TwMenuItem> GetMenuItemById(int id);

        Task DeleteMenuItemById(int id);

        Task<int> UpdateMenuItemById(TwMenuItem menuItem);

        Task<int> InsertMenuItem(TwMenuItem menuItem);

        #endregion
    }
}
