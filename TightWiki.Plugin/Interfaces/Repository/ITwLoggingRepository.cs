using Microsoft.Extensions.Logging;
using NTDLS.SqliteDapperWrapper;
using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Interfaces.Repository
{
    /// <summary>
    ///  Data access for logs and exceptions. This is used by the plugin to log exceptions
    ///  and other information to a local database, which can then be viewed by the user in the UI.
    ///  This is not intended to be a full featured logging system, but rather a simple way for the
    ///  plugin to log information that can be viewed by the user in the UI.
    /// </summary>
    public interface ITwLoggingRepository
    {
        SqliteManagedFactory LoggingFactory { get; }

        Task PurgeLogs();
        Task CreateTablesIfNotExist();
        Task WriteException(string? text = null, string? exceptionText = null, string? stackTrace = null);
        Task WriteLog(LogLevel severity, string? text = null, string? exceptionText = null, string? stackTrace = null);
        Task<int> GetExceptionCount();
        Task<List<TwLogEntry>> GetLogEntriesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, string? severity = null);
        Task<TwLogEntry> GetLogEntryById(int id);
        Task<List<TwEventLogSeverity>> GetSeverities();
    }
}
