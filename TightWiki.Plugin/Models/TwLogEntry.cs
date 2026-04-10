namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a single entry in the plugin's local log database,
    /// capturing severity, descriptive text, exception details, and the time the event occurred.
    /// </summary>
    public class TwLogEntry
    {
        /// <summary>
        /// The unique identifier for this log entry.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The severity level of this log entry, such as Information, Warning, or Error.
        /// </summary>
        public string Severity { get; set; } = string.Empty;

        /// <summary>
        /// The descriptive message associated with this log entry.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// The exception message associated with this log entry, if applicable.
        /// </summary>
        public string ExceptionText { get; set; } = string.Empty;

        /// <summary>
        /// The stack trace associated with this log entry, if applicable.
        /// </summary>
        public string StackTrace { get; set; } = string.Empty;

        /// <summary>
        /// The date and time this log entry was created.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// The total number of pages available when paginating log entry lists.
        /// </summary>
        public int PaginationPageCount { get; set; }
    }
}
