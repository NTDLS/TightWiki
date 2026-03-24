using Microsoft.Extensions.Logging;
using TightWiki.Models;
using TightWiki.Models.DataModels;

namespace TightWiki.Repository
{
    public static class LoggingRepository
    {
        public static void PurgeLogs()
        {
            ManagedDataStorage.Logging.Execute("PurgeLogs.sql");
        }

        public static void CreateTablesIfNotExist()
        {
            if (!ManagedDataStorage.Logging.DoesTableExist("Severity"))
            {
                ManagedDataStorage.Logging.Execute(@"Scripts\CreateSeverityTable.sql");
            }

            if (!ManagedDataStorage.Logging.DoesTableExist("Log"))
            {
                ManagedDataStorage.Logging.Execute(@"Scripts\CreateLogTable.sql");
            }
        }

        public static void WriteException(string? text = null, string? exceptionText = null, string? stackTrace = null)
            => WriteLog(LogLevel.Error, text, exceptionText, stackTrace);

        public static void WriteLog(LogLevel severity, string? text = null, string? exceptionText = null, string? stackTrace = null)
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

            ManagedDataStorage.Logging.Execute("InsertLog.sql", param);
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

        public static int GetExceptionCount()
        {
            return ManagedDataStorage.Logging.ExecuteScalar<int>("GetExceptionCount.sql");
        }

        public static List<WikiLogEntry> GetLogEntriesPaged(int pageNumber,
            string? orderBy = null, string? orderByDirection = null)
        {
            var param = new
            {
                PageNumber = pageNumber,
                PageSize = GlobalConfiguration.PaginationSize
            };

            var query = RepositoryHelper.TransposeOrderby("GetLogEntriesPaged.sql", orderBy, orderByDirection);
            return ManagedDataStorage.Logging.Query<WikiLogEntry>(query, param);
        }

        public static WikiLogEntry GetLogEntryById(int id)
        {
            var param = new
            {
                Id = id
            };

            return ManagedDataStorage.Logging.QuerySingle<WikiLogEntry>("GetLogEntryById.sql", param);
        }
    }
}
