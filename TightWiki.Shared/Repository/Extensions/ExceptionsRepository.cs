using System;
using Dapper;
using System.Data;
using TightWiki.Shared.ADO;

namespace TightWiki.Shared.Repository
{
    public static partial class ExceptionRepository
    {
        public static void InsertException(string text = null, string exceptionText = null, string stackTrace = null)
        {
            using (var handler = new SqlConnectionHandler())
            {
                var param = new
                {
                    Text = text,
                    ExceptionText = exceptionText,
                    StackTrace = stackTrace
                };

                handler.Connection.Execute("InsertException",
                    param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
        }

        public static void InsertException(Exception ex)
        {
            using (var handler = new SqlConnectionHandler())
            {
                var param = new
                {
                    ExceptionText = ex.Message,
                    StackTrace = ex.StackTrace
                };

                handler.Connection.Execute("InsertException",
                    param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
        }

        public static void InsertException(Exception ex, string text = null)
        {
            using (var handler = new SqlConnectionHandler())
            {
                var param = new
                {
                    Text = text,
                    ExceptionText = ex.Message,
                    StackTrace = ex.StackTrace
                };

                handler.Connection.Execute("InsertException",
                    param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
        }
    }
}
