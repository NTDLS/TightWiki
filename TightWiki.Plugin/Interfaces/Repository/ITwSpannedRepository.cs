namespace TightWiki.Plugin.Interfaces.Repository
{
    /// <summary>
    /// Data access for spanned queries that operate across multiple databases,
    /// such as vacuuming all databases, optimizing all databases, integrity checking all databases,
    /// foreign key checking all databases, and getting database versions, page counts, and page sizes.
    /// These are operations that are not specific to a single database, but rather operate across all
    /// databases used by the plugin. This is intended to be used by the plugin's maintenance tools
    /// to perform these operations on all databases at once.
    /// </summary>
    public interface ISpannedRepository
    {
        /// <summary>
        /// Runs a VACUUM on the specified database, reclaiming unused space and defragmenting storage.
        /// Returns a status message describing the result.
        /// </summary>
        public Task<string> VacuumDatabase(string databaseName);

        /// <summary>
        /// Runs an OPTIMIZE on the specified database to improve query performance.
        /// Returns a status message describing the result.
        /// </summary>
        public Task<string> OptimizeDatabase(string databaseName);

        /// <summary>
        /// Runs an integrity check on the specified database to detect corruption or structural issues.
        /// Returns a status message describing the result.
        /// </summary>
        public Task<string> IntegrityCheckDatabase(string databaseName);

        /// <summary>
        /// Runs a foreign key check on the specified database to detect referential integrity violations.
        /// Returns a status message describing the result.
        /// </summary>
        public Task<string> ForeignKeyCheck(string databaseName);

        /// <summary>
        /// Returns the name and schema version for each database used by the plugin.
        /// </summary>
        public Task<List<(string Name, string Version)>> GetDatabaseVersions();

        /// <summary>
        /// Returns the name and page count for each database used by the plugin.
        /// </summary>
        public Task<List<(string Name, int PageCount)>> GetDatabasePageCounts();

        /// <summary>
        /// Returns the name and page size in bytes for each database used by the plugin.
        /// </summary>
        public Task<List<(string Name, int PageSize)>> GetDatabasePageSizes();
    }
}
