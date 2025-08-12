using TightWiki.Models;
using TightWiki.Models.DataModels;

namespace TightWiki.Repository
{
    public static class ExceptionRepository
    {
        public static void PurgeExceptions()
        {
            ManagedDataStorage.Exceptions.Execute("PurgeExceptions.sql");
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

            ManagedDataStorage.Exceptions.Execute("InsertException.sql", param);
        }

        public static void InsertException(Exception ex)
        {
            var param = new
            {
                Text = string.Empty,
                ExceptionText = ex.Message,
                StackTrace = ex.StackTrace,
                CreatedDate = DateTime.UtcNow,
            };

            ManagedDataStorage.Exceptions.Execute("InsertException.sql", param);
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

            ManagedDataStorage.Exceptions.Execute("InsertException.sql", param);
        }

        public static int GetExceptionCount()
        {
            return ManagedDataStorage.Exceptions.ExecuteScalar<int>("GetExceptionCount.sql");
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
            return ManagedDataStorage.Exceptions.Query<WikiException>(query, param).ToList();
        }

        public static WikiException GetExceptionById(int id)
        {
            var param = new
            {
                Id = id
            };

            return ManagedDataStorage.Exceptions.QuerySingle<WikiException>("GetExceptionById.sql", param);
        }
    }
}
