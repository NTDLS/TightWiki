using TightWiki.Models.DataModels;

namespace TightWiki.Repository
{
    public static class ExceptionRepository
    {
        public static void ClearExceptions()
        {
            ManagedDataStorage.Exceptions.Execute("ClearExceptions.sql");
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

        public static List<WikiException> GetAllExceptionsPaged(int pageNumber, int? pageSize = null)
        {
            pageSize ??= ConfigurationRepository.Get<int>("Customization", "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
            };

            return ManagedDataStorage.Exceptions.Query<WikiException>("GetAllExceptionsPaged.sql", param).ToList();
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
