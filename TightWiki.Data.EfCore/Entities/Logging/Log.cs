namespace TightWiki.Data.EfCore.Entities.Logging
{
    /// <summary>
    /// A single application log entry (Logging.Log).
    /// </summary>
    public class Log
    {
        /// <summary>
        /// The unique identifier for this log entry.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The identifier of the <see cref="Logging.Severity"/> of this log entry. Optional - the real schema
        /// allows a null severity.
        /// </summary>
        public int? SeverityId { get; set; }

        /// <summary>
        /// The primary log message text.
        /// </summary>
        public string? Text { get; set; }

        /// <summary>
        /// The associated exception message text, if any.
        /// </summary>
        public string? ExceptionText { get; set; }

        /// <summary>
        /// The associated stack trace text, if any.
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// The date/time this log entry was created.
        /// </summary>
        public DateTime? CreatedDate { get; set; }

        /// <summary>
        /// The severity level of this log entry.
        /// </summary>
        public Severity? Severity { get; set; }
    }
}
