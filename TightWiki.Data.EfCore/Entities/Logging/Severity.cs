namespace TightWiki.Data.EfCore.Entities.Logging
{
    /// <summary>
    /// A log severity level (Logging.Severity), e.g. "Trace", "Debug", "Information", "Warning", "Error",
    /// "Critical", "None".
    /// </summary>
    public class Severity
    {
        /// <summary>
        /// The unique identifier for this severity level.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The unique, case-insensitive name of this severity level.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The log entries recorded at this severity level.
        /// </summary>
        public ICollection<Log> Logs { get; set; } = new List<Log>();
    }
}
