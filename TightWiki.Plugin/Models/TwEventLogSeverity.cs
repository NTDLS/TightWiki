namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a Severity table row.
    /// </summary>
    public class TwEventLogSeverity
    {
        /// <summary>
        /// Severity Id column.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Severity Name column.
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }
}
