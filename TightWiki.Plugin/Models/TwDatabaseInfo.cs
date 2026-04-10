namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents diagnostic information about a single SQLite database,
    /// including its name, schema version, and storage statistics.
    /// </summary>
    public class TwDatabaseInfo
    {
        /// <summary>
        /// The name of the database.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The schema version of the database.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// The number of pages allocated in the database file.
        /// </summary>
        public int PageCount { get; set; }

        /// <summary>
        /// The size of each database page in bytes.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// The total size of the database in bytes, calculated as PageCount multiplied by PageSize.
        /// </summary>
        public int DatabaseSize { get; set; }
    }
}