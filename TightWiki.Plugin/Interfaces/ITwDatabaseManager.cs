using Microsoft.AspNetCore.Identity;
using NTDLS.SqliteDapperWrapper;
using TightWiki.Plugin.Interfaces.Repository;

namespace TightWiki.Plugin.Interfaces
{
    /// <summary>
    /// Interface for managing multiple databases in the TightWiki plugin. This interface provides access to various repositories
    /// </summary>
    public interface ITwDatabaseManager
    {
        /// <summary>
        /// Gets the collection of available databases and their associated managed factories.
        /// </summary>
        /// <remarks>Each element in the collection contains the name of the database and the
        /// corresponding factory used to create connections. The order of the elements is not guaranteed.</remarks>
        (string Name, SqliteManagedFactory Factory)[] Databases { get; }

        /// <summary>
        /// Gets the repository used to access configuration settings.
        /// </summary>
        ITwConfigurationRepository ConfigurationRepository { get; }
        /// <summary>
        /// Gets the repository used to access and manage default configuration values.
        /// </summary>
        ITwDefaultsRepository DefaultsRepository { get; }
        /// <summary>
        /// Gets the repository used to access emoji data.
        /// </summary>
        ITwEmojiRepository EmojiRepository { get; }
        /// <summary>
        /// Gets the logging repository used to record application log entries.
        /// </summary>
        ITwLoggingRepository LoggingRepository { get; }
        /// <summary>
        /// Gets the repository used to access and manage page data.
        /// </summary>
        ITwPageRepository PageRepository { get; }
        /// <summary>
        /// Gets the repository used to access statistics data.
        /// </summary>
        ITwStatisticsRepository StatisticsRepository { get; }
        /// <summary>
        /// Gets the repository used to access and manage user data.
        /// </summary>
        ITwUsersRepository UsersRepository { get; }

        /// <summary>
        /// Applies all required seed data to the system using the provided localization, user management, engine, and
        /// data type services.
        /// </summary>
        /// <param name="localizer">The localization service used to provide localized text for seeded data.</param>
        /// <param name="userManager">The user manager responsible for creating and managing user accounts during the seeding process.</param>
        /// <param name="tightEngine">The engine instance used to perform operations required for seeding data.</param>
        /// <param name="defaultDataTypes">An array of default data types to be seeded into the system.</param>
        /// <returns>A task that represents the asynchronous operation of applying all seed data.</returns>
        Task ApplyAllSeedData(ITwSharedLocalizationText localizer, UserManager<IdentityUser> userManager,
            ITwEngine tightEngine, TwDefaultDataType[] defaultDataTypes);

        #region Database admin.

        /// <summary>
        /// Initiates a database vacuum operation to reclaim unused space and optimize the specified database.
        /// </summary>
        /// <remarks>Vacuuming can improve database performance and reduce file size by reorganizing
        /// storage. The operation may be resource-intensive and should be scheduled during periods of low
        /// activity.</remarks>
        /// <param name="databaseName">The name of the database to vacuum. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a status message indicating the
        /// outcome of the vacuum operation.</returns>
        Task<string> VacuumDatabase(string databaseName);
        /// <summary>
        /// Initiates an optimization operation on the specified database asynchronously.
        /// </summary>
        /// <param name="databaseName">The name of the database to optimize. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a status message describing the
        /// outcome of the optimization.</returns>
        Task<string> OptimizeDatabase(string databaseName);
        /// <summary>
        /// Performs an integrity check on the specified database and returns the results as a string.
        /// </summary>
        /// <param name="databaseName">The name of the database to check for integrity issues. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a string with the integrity
        /// check report for the specified database.</returns>
        Task<string> IntegrityCheckDatabase(string databaseName);
        /// <summary>
        /// Checks the foreign key constraints in the specified database and returns the result as a string.
        /// </summary>
        /// <param name="databaseName">The name of the database in which to check foreign key constraints. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a string describing the outcome
        /// of the foreign key check.</returns>
        Task<string> ForeignKeyCheck(string databaseName);

        /// <summary>
        /// Asynchronously retrieves the names and versions of all available databases.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of tuples, each
        /// containing the name and version of a database. The list is empty if no databases are available.</returns>
        Task<List<(string Name, string Version)>> GetDatabaseVersions();
        /// <summary>
        /// Asynchronously retrieves the names and page counts of all databases.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of tuples, each
        /// containing the name of a database and its corresponding page count.</returns>
        Task<List<(string Name, int PageCount)>> GetDatabasePageCounts();
        /// <summary>
        /// Asynchronously retrieves the page sizes for all available databases.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of tuples, each
        /// containing the name of a database and its corresponding page size in bytes.</returns>
        Task<List<(string Name, int PageSize)>> GetDatabasePageSizes();

        #endregion
    }
}