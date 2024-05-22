using Dapper;
using Microsoft.Data.Sqlite;

namespace TightWiki.DataStorage
{
    /// <summary>
    /// Allows for attaching other databases and performing cross-database-joins. 
    /// </summary>
    public class DisposableAttachment : IDisposable
    {
        public SqliteConnection NativeConnection { get; private set; }
        public string DatabaseAlias { get; private set; }

        internal DisposableAttachment(SqliteConnection nativeConnection, string databaseAlias)
        {
            NativeConnection = nativeConnection;
            DatabaseAlias = databaseAlias;
        }

        public void Dispose()
        {
            NativeConnection.Execute($"DETACH DATABASE {DatabaseAlias}");
        }
    }
}
