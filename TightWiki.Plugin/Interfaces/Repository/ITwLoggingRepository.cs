using Microsoft.Extensions.Logging;
using NTDLS.SqliteDapperWrapper;
using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Interfaces.Repository
{
    /// <summary>
    /// Data access for logs and exceptions. This is used by the plugin to log exceptions
    /// and other information to a local database, which can then be viewed by the user in the UI.
    /// This is not intended to be a full featured logging system, but rather a simple way for the
    /// plugin to log information that can be viewed by the user in the UI.
    /// </summary>
    public interface ITwLoggingRepository
    {
        /// <summary>
        /// SQLite factory used to access the logging database.
        /// </summary>
        SqliteManagedFactory LoggingFactory { get; }

        /// <summary>
        /// Deletes all log entries from the database.
        /// </summary>
        Task PurgeLogs();

        /// <summary>
        /// Creates the required logging tables if they do not already exist.
        /// </summary>
        Task CreateTablesIfNotExist();

        /// <summary>
        /// Writes an exception entry to the log with optional descriptive text, exception message, and stack trace.
        /// </summary>
        Task WriteException(string? text = null, string? exceptionText = null, string? stackTrace = null);

        /// <summary>
        /// Writes a log entry at the specified severity level, with optional descriptive text, exception message, and stack trace.
        /// </summary>
        Task WriteLog(LogLevel severity, string? text = null, string? exceptionText = null, string? stackTrace = null);

        /// <summary>
        /// Returns the total number of exception entries in the log.
        /// </summary>
        Task<int> GetExceptionCount();

        /// <summary>
        /// Returns a paged list of log entries, with optional sorting and severity filtering.
        /// </summary>
        Task<List<TwLogEntry>> GetLogEntriesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, string? severity = null);

        /// <summary>
        /// Returns a single log entry by its unique identifier.
        /// </summary>
        Task<TwLogEntry> GetLogEntryById(int id);

        /// <summary>
        /// Returns all available log severity levels.
        /// </summary>
        Task<List<TwEventLogSeverity>> GetSeverities();
    }
}