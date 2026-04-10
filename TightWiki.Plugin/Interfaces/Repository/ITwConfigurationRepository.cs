using NTDLS.SqliteDapperWrapper;
using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Interfaces.Repository
{
    /// <summary>
    /// Data access for configuration entries, themes, and wiki database metrics, etc.
    /// </summary>
    public interface ITwConfigurationRepository
    {
        /// <summary>
        /// SQLite factory that is used to access the database.
        /// </summary>
        SqliteManagedFactory ConfigFactory { get; }

        /// <summary>
        /// Gets the configuration entry values for a specific group name.
        /// </summary>
        /// <param name="groupName">The name of the configuration group.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the configuration entries for the specified group.</returns>
        Task<TwConfigurationEntries> GetConfigurationEntryValuesByGroupName(string groupName);

        /// <summary>
        /// Gets all themes.
        /// </summary>
        Task<List<Models.TwTheme>> GetAllThemes();

        /// <summary>
        /// Retrieves statistical metrics for the wiki database.
        /// </summary>
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

        /// <summary>
        /// Saves the specified configuration entry value for a given group and entry name asynchronously.
        /// </summary>
        /// <param name="groupName">The name of the configuration group to which the entry belongs. Cannot be null or empty.</param>
        /// <param name="entryName">The name of the configuration entry to update or create. Cannot be null or empty.</param>
        /// <param name="value">The value to assign to the configuration entry. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous save operation.</returns>
        Task SaveConfigurationEntryValueByGroupAndEntry(string groupName, string entryName, string value);

        /// <summary>
        /// Retrieves a all configuration groups and their associated entries from the database.
        /// </summary>
        Task<List<TwConfigurationNest>> GetConfigurationNest();

        /// <summary>
        /// Retrieves a all configuration groups and their associated entries from the database.
        /// </summary>
        Task<List<TwConfigurationFlat>> GetFlatConfiguration();

        /// <summary>
        /// Retrieves the value of a configuration entry specified by group and entry name.
        /// </summary>
        /// <param name="groupName">The name of the configuration group containing the entry. Cannot be null or empty.</param>
        /// <param name="entryName">The name of the configuration entry to retrieve. Cannot be null or empty.</param>
        Task<string?> GetConfigurationEntryValuesByGroupNameAndEntryName(string groupName, string entryName);

        /// <summary>
        /// Retrieves the value of a configuration entry specified by group and entry name.
        /// </summary>
        /// <typeparam name="T">The type of the configuration entry value.</typeparam>
        /// <param name="groupName">The name of the configuration group containing the entry. Cannot be null or empty.</param>
        /// <param name="entryName">The name of the configuration entry to retrieve. Cannot be null or empty.</param>
        Task<T?> Get<T>(string groupName, string entryName);

        /// <summary>
        /// Retrieves the value of a configuration entry specified by group and entry name.
        /// </summary>
        /// <typeparam name="T">The type of the configuration entry value.</typeparam>
        /// <param name="groupName">The name of the configuration group containing the entry. Cannot be null or empty.</param>
        /// <param name="entryName">The name of the configuration entry to retrieve. Cannot be null or empty.</param>
        /// <param name="defaultValue">The default value to return if the configuration entry is not found or cannot be converted to the specified type.</param>
        Task<T> Get<T>(string groupName, string entryName, T defaultValue);

        /// <summary>
        /// Gets all menu items, optionally ordered by a specified column and direction.
        /// </summary>
        Task<List<TwMenuItem>> GetAllMenuItems(string? orderBy = null, string? orderByDirection = null);

        /// <summary>
        /// Retrieves a menu item by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the menu item to retrieve.</param>
        Task<TwMenuItem> GetMenuItemById(int id);

        /// <summary>
        /// Deletes a menu item by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the menu item to delete.</param>
        Task DeleteMenuItemById(int id);

        /// <summary>
        /// Updates an existing menu item in the database based on its unique identifier.
        /// </summary>
        /// <param name="menuItem">The menu item to update.</param>
        Task<int> UpdateMenuItemById(TwMenuItem menuItem);

        /// <summary>
        /// Inserts a new menu item into the data store asynchronously.
        /// </summary>
        /// <param name="menuItem">The menu item to insert. Cannot be null.</param>
        Task<int> InsertMenuItem(TwMenuItem menuItem);
    }
}
