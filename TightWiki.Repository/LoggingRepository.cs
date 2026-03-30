using Microsoft.Extensions.Logging;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Repository
{
    public class LoggingRepository(ITwConfigurationRepository configurationRepository)
        : ILoggingRepository
    {
        public async Task PurgeLogs()
        {
            await ManagedDataStorage.Logging.ExecuteAsync("PurgeLogs.sql");
        }

        public async Task CreateTablesIfNotExist()
        {
            if (!ManagedDataStorage.Logging.DoesTableExist("Severity"))
            {
                await ManagedDataStorage.Logging.ExecuteAsync(@"Scripts\CreateSeverityTable.sql");
            }

            if (!ManagedDataStorage.Logging.DoesTableExist("Log"))
            {
                await ManagedDataStorage.Logging.ExecuteAsync(@"Scripts\CreateLogTable.sql");
            }
        }

        public async Task WriteException(string? text = null, string? exceptionText = null, string? stackTrace = null)
            => await WriteLog(LogLevel.Error, text, exceptionText, stackTrace);

        public async Task WriteLog(LogLevel severity, string? text = null, string? exceptionText = null, string? stackTrace = null)
        {
            if (severity >= LogLevel.Warning)
            {
                Console.WriteLine($"{text} {exceptionText} {stackTrace}");
            }

            var param = new
            {
                SeverityName = severity.ToString(),
                Text = text,
                ExceptionText = exceptionText,
                StackTrace = stackTrace,
                CreatedDate = DateTime.UtcNow,
            };

            await ManagedDataStorage.Logging.ExecuteAsync("InsertLog.sql", param);
        }

        /*
        public void WriteException(Exception ex)
        {
            var stackTrace = new StackTrace();
            var method = stackTrace.GetFrame(1)?.GetMethod();
            var message = string.IsNullOrEmpty(method?.Name) ? string.Empty : $"Error in {method?.Name}";
            WriteLog(WikiSeverity.Error, message, ex.Message, ex.StackTrace);
        }

        public void WriteException(Exception ex, string text)
        {
            WriteLog(WikiSeverity.Error, text, ex.Message, ex.StackTrace);
        }
        */

        public async Task<int> GetExceptionCount()
        {
            return await ManagedDataStorage.Logging.ExecuteScalarAsync<int>("GetExceptionCount.sql");
        }

        public async Task<List<TwLogEntry>> GetLogEntriesPaged(int pageNumber,
            string? orderBy = null, string? orderByDirection = null)
        {
            var paginationSize = await configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = paginationSize
            };

            var query = RepositoryHelpers.TransposeOrderby("GetLogEntriesPaged.sql", orderBy, orderByDirection);
            return await ManagedDataStorage.Logging.QueryAsync<TwLogEntry>(query, param);
        }

        public async Task<TwLogEntry> GetLogEntryById(int id)
        {
            var param = new
            {
                Id = id
            };

            return await ManagedDataStorage.Logging.QuerySingleAsync<TwLogEntry>("GetLogEntryById.sql", param);
        }
    }
}
