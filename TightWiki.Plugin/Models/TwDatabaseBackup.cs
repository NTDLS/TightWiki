namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a zip file containing a snapshot of all of the databases.
    /// </summary>
    public class TwDatabaseBackup
    {
        /// <summary>
        /// The name of the backup zip file.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// The size of the backup zip file in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// The (UTC) date and time that the backup was created.
        /// </summary>
        public DateTime CreatedDate { get; set; }
    }
}
