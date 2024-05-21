using Dapper;
using Microsoft.Data.Sqlite;

namespace TightWiki.DataStorage
{
    /// <summary>
    /// Creates a session scoped temp table that contains the values from the supplied list, the temp table is dropped on dispose.
    /// This allows for a simple replacement implementation of STRING_SPLIT.
    /// </summary>
    public class DisposableValueListTable : IDisposable
    {
        public SqliteConnection NativeConnection { get; private set; }
        public string TableName { get; private set; }

        internal DisposableValueListTable(SqliteConnection nativeConnection, string tableName)
        {
            NativeConnection = nativeConnection;
            TableName = tableName;
        }

        public void Dispose()
        {
            NativeConnection.Execute($"DROP TABLE {TableName}");
        }
    }
}
