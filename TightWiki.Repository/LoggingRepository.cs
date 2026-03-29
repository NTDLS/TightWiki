using Microsoft.Extensions.Logging;
using TightWiki.Models.DataModels;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Repository
{
    public static class LoggingRepository
    {
        public static async Task PurgeLogs()
        {
            await ManagedDataStorage.Logging.ExecuteAsync("PurgeLogs.sql");
        }

        public static async Task CreateTablesIfNotExist()
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

        public static async Task WriteException(string? text = null, string? exceptionText = null, string? stackTrace = null)
            => await WriteLog(LogLevel.Error, text, exceptionText, stackTrace);

        public static async Task WriteLog(LogLevel severity, string? text = null, string? exceptionText = null, string? stackTrace = null)
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
        public static void WriteException(Exception ex)
        {
            var stackTrace = new StackTrace();
            var method = stackTrace.GetFrame(1)?.GetMethod();
            var message = string.IsNullOrEmpty(method?.Name) ? string.Empty : $"Error in {method?.Name}";
            WriteLog(WikiSeverity.Error, message, ex.Message, ex.StackTrace);
        }

        public static void WriteException(Exception ex, string text)
        {
            WriteLog(WikiSeverity.Error, text, ex.Message, ex.StackTrace);
        }
        */

        public static async Task<int> GetExceptionCount()
        {
            return await ManagedDataStorage.Logging.ExecuteScalarAsync<int>("GetExceptionCount.sql");
        }

        public static async Task<List<WikiLogEntry>> GetLogEntriesPaged(int pageNumber,
            string? orderBy = null, string? orderByDirection = null)
        {
            var paginationSize = await ConfigurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = paginationSize
            };

            var query = RepositoryHelpers.TransposeOrderby("GetLogEntriesPaged.sql", orderBy, orderByDirection);
            return await ManagedDataStorage.Logging.QueryAsync<WikiLogEntry>(query, param);
        }

        public static async Task<WikiLogEntry> GetLogEntryById(int id)
        {
            var param = new
            {
                Id = id
            };

            return await ManagedDataStorage.Logging.QuerySingleAsync<WikiLogEntry>("GetLogEntryById.sql", param);
        }
    }
}
