using System;
using System.Data;
using System.Data.SqlClient;

namespace SharpWiki.Shared.ADO
{
    class SqlConnectionHandler : IDisposable
    {
        private bool disposed = false;
        private SqlConnection connection = null;

        public SqlConnection Connection
        {
            get
            {
                return connection;
            }
        }

        public SqlConnectionHandler(SqlConnectionStringBuilder builder)
        {
            connection = new SqlConnection(builder.ToString());
            Connection.Open();
        }

        public SqlConnectionHandler(string connectionString)
        {
            connection = new SqlConnection(connectionString);
            Connection.Open();
        }

        public SqlConnectionHandler()
        {
            connection = new SqlConnection(Singletons.ConnectionString);
            Connection.Open();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;
                if (disposing)
                {
                    try
                    {
                        if (connection != null && connection.State == ConnectionState.Open)
                        {
                            connection.Close();
                        }
                    }
                    catch
                    {
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}