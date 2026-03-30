using Microsoft.Extensions.Logging;
using NTDLS.SqliteDapperWrapper;
using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Interfaces.Repository
{
    public interface ITwLoggingRepository
    {
        SqliteManagedFactory LoggingFactory { get; }

        Task PurgeLogs();
        Task CreateTablesIfNotExist();
        Task WriteException(string? text = null, string? exceptionText = null, string? stackTrace = null);
        Task WriteLog(LogLevel severity, string? text = null, string? exceptionText = null, string? stackTrace = null);
        Task<int> GetExceptionCount();
        Task<List<TwLogEntry>> GetLogEntriesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null);
        Task<TwLogEntry> GetLogEntryById(int id);
    }
}
