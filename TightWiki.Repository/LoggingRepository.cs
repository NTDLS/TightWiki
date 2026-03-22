using System.Diagnostics;
using TightWiki.Models;
using TightWiki.Models.DataModels;

namespace TightWiki.Repository
{
    public static class LoggingRepository
    {
        public static void PurgeExceptions()
        {
            ManagedDataStorage.Logging.Execute("PurgeExceptions.sql");
        }

        public static void CreateTablesIfNotExist()
        {
            if (ManagedDataStorage.Logging.DoesTableExist("Severity"))
            {
                ManagedDataStorage.Logging.Execute(@"Scripts\CreateSeverityTable.sql");
            }

            if (ManagedDataStorage.Logging.DoesTableExist("Log"))
            {
                ManagedDataStorage.Logging.Execute(@"Scripts\CreateLogTable.sql");
            }
        }

        public static void InsertException(string? text = null, string? exceptionText = null, string? stackTrace = null)
        {
            var param = new
            {
                Text = text,
                ExceptionText = exceptionText,
                StackTrace = stackTrace,
                CreatedDate = DateTime.UtcNow,
            };

            ManagedDataStorage.Logging.Execute("InsertException.sql", param);
        }

        public static void InsertException(Exception ex)
        {
            var stackTrace = new StackTrace();
            var method = stackTrace.GetFrame(1)?.GetMethod();
            var message = string.IsNullOrEmpty(method?.Name) ? string.Empty : $"Error in {method?.Name}";

            var param = new
            {
                Text = message,
                ExceptionText = ex.Message,
                StackTrace = ex.StackTrace,
                CreatedDate = DateTime.UtcNow,
            };

            ManagedDataStorage.Logging.Execute("InsertException.sql", param);
        }

        public static void InsertException(Exception ex, string? text = null)
        {
            var param = new
            {
                Text = text,
                ExceptionText = ex.Message,
                StackTrace = ex.StackTrace,
                CreatedDate = DateTime.UtcNow
            };

            ManagedDataStorage.Logging.Execute("InsertException.sql", param);
        }

        public static int GetExceptionCount()
        {
            return ManagedDataStorage.Logging.ExecuteScalar<int>("GetExceptionCount.sql");
        }

        public static List<WikiException> GetAllExceptionsPaged(int pageNumber,
            string? orderBy = null, string? orderByDirection = null)
        {
            var param = new
            {
                PageNumber = pageNumber,
                PageSize = GlobalConfiguration.PaginationSize
            };

            var query = RepositoryHelper.TransposeOrderby("GetAllExceptionsPaged.sql", orderBy, orderByDirection);
            return ManagedDataStorage.Logging.Query<WikiException>(query, param).ToList();
        }

        public static WikiException GetExceptionById(int id)
        {
            var param = new
            {
                Id = id
            };

            return ManagedDataStorage.Logging.QuerySingle<WikiException>("GetExceptionById.sql", param);
        }
    }
}
