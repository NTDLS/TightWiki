using TightWiki.DataStorage;

namespace TightWiki.Repository
{
    public static class ExceptionRepository
    {
        public static void InsertException(string? text = null, string? exceptionText = null, string? stackTrace = null)
        {
            var param = new
            {
                Text = text,
                ExceptionText = exceptionText,
                StackTrace = stackTrace,
                CreatedDate = DateTime.UtcNow,
            };

            ManagedDataStorage.Exceptions.Execute("InsertException", param);
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

            ManagedDataStorage.Exceptions.Execute("InsertException", param);
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

            ManagedDataStorage.Exceptions.Execute("InsertException", param);
        }

        public static int GetExceptionCount()
        {
            return ManagedDataStorage.Exceptions.ExecuteScalar<int>("GetExceptionCount");
        }
    }
}
