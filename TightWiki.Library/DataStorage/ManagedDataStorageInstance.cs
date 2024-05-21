using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;

namespace TightWiki.DataStorage
{
    //TODO: Make Nuget package called "DapperWrapper"

    /// <summary>
    /// A disposable database connection wrapper that functions as an ephemeral instance.
    /// One instance of this class is generally created per query.
    /// </summary>
    public class ManagedDataStorageInstance : IDisposable
    {
        public SqliteConnection NativeConnection { get; private set; }

        private static readonly Dictionary<string, string> _scriptCache = new();

        public delegate void UseAndDisposeProc(ManagedDataStorageInstance connection);
        public delegate T UseAndDisposeProc<T>(ManagedDataStorageInstance connection);

        public ManagedDataStorageInstance(string connectionString)
        {
            NativeConnection = new SqliteConnection(connectionString);
            NativeConnection.Open();
        }

        public SqliteTransaction BeginTransaction()
        {
            return NativeConnection.BeginTransaction();
        }

        public SqliteTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            return NativeConnection.BeginTransaction(isolationLevel);
        }

        public DisposableValueListTable CreateValueListTableFrom(string tableName, IEnumerable<string> values, SqliteTransaction transaction)
        {
            var result = new DisposableValueListTable(NativeConnection, tableName);

            var createTempTableCommand = new SqliteCommand($"CREATE TEMP TABLE {tableName} (Value TEXT COLLATE NOCASE);", NativeConnection, transaction);
            createTempTableCommand.ExecuteNonQuery();

            var insertTagCommand = new SqliteCommand($"INSERT INTO {tableName} (Value) VALUES (@Tag);", NativeConnection, transaction);

            foreach (var tag in values)
            {
                insertTagCommand.Parameters.Clear();
                insertTagCommand.Parameters.AddWithValue("@Tag", tag);
                insertTagCommand.ExecuteNonQuery();
            }

            return result;
        }

        public DisposableValueListTable CreateValueListTableFrom(string tableName, IEnumerable<string> values)
        {
            var result = new DisposableValueListTable(NativeConnection, tableName);

            using var transaction = NativeConnection.BeginTransaction();

            var createTempTableCommand = new SqliteCommand($"CREATE TEMP TABLE {tableName} (Value TEXT COLLATE NOCASE);", NativeConnection, transaction);
            createTempTableCommand.ExecuteNonQuery();

            var insertTagCommand = new SqliteCommand($"INSERT INTO {tableName} (Value) VALUES (@Tag);", NativeConnection, transaction);

            foreach (var tag in values)
            {
                insertTagCommand.Parameters.Clear();
                insertTagCommand.Parameters.AddWithValue("@Tag", tag);
                insertTagCommand.ExecuteNonQuery();
            }

            transaction.Commit();

            return result;
        }

        public DisposableValueListTable CreateValueListTableFrom(string tableName, IEnumerable<int> values)
        {
            var result = new DisposableValueListTable(NativeConnection, tableName);

            using var transaction = NativeConnection.BeginTransaction();

            var createTempTableCommand = new SqliteCommand($"CREATE TEMP TABLE {tableName} (Value TEXT COLLATE NOCASE);", NativeConnection, transaction);
            createTempTableCommand.ExecuteNonQuery();

            var insertTagCommand = new SqliteCommand($"INSERT INTO {tableName} (Value) VALUES (@Tag);", NativeConnection, transaction);

            foreach (var tag in values)
            {
                insertTagCommand.Parameters.Clear();
                insertTagCommand.Parameters.AddWithValue("@Tag", tag);
                insertTagCommand.ExecuteNonQuery();
            }

            transaction.Commit();

            return result;
        }

        public DisposableValueListTable CreateValueListTableFrom<T>(string tableName, IEnumerable<T> values)
        {
            var result = new DisposableValueListTable(NativeConnection, tableName);
            using var transaction = NativeConnection.BeginTransaction();

            // Use reflection to get property names and types of T
            PropertyInfo[] props = typeof(T).GetProperties();
            var columns = new StringBuilder();
            foreach (var prop in props)
            {
                // Creating columns for each property of the class
                // This example assumes all properties are of type string for simplicity
                // Adjust the type based on your specific needs or data types
                columns.Append($"{prop.Name} TEXT COLLATE NOCASE,");
            }

            // Remove the last comma
            columns.Length--;

            // Create the temporary table with dynamic columns
            var createTempTableCommand = new SqliteCommand($"CREATE TEMP TABLE {tableName} ({columns});", NativeConnection, transaction);
            createTempTableCommand.ExecuteNonQuery();

            // Prepare the insert command
            var columnNames = new StringBuilder();
            var valuePlaceholders = new StringBuilder();
            foreach (var prop in props)
            {
                columnNames.Append($"{prop.Name},");
                valuePlaceholders.Append($"@{prop.Name},");
            }
            columnNames.Length--;
            valuePlaceholders.Length--;

            var insertCommandText = $"INSERT INTO {tableName} ({columnNames}) VALUES ({valuePlaceholders});";
            var insertCommand = new SqliteCommand(insertCommandText, NativeConnection, transaction);

            // Insert all values
            foreach (var item in values)
            {
                insertCommand.Parameters.Clear();
                foreach (var prop in props)
                {
                    var val = prop.GetValue(item);
                    insertCommand.Parameters.AddWithValue($"@{prop.Name}", val ?? DBNull.Value); // Handle NULL values
                }
                insertCommand.ExecuteNonQuery();
            }

            transaction.Commit();
            return result;
        }

        public void Dispose()
        {
            NativeConnection.Close();
            NativeConnection.Dispose();
        }

        /// <summary>
        /// Loads a script from the assembly, this allows us to execute mock stored procedures.
        /// </summary>
        /// <param name="scriptName"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static string LoadScript(string scriptName)
        {
            string cacheKey = scriptName.ToLower();
            if (_scriptCache.TryGetValue(cacheKey, out var cachedScriptText))
            {
                return cachedScriptText;
            }

            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"TightWiki.Repository.Scripts.{scriptName}.sql")
                ?? throw new InvalidOperationException("Script not found: " + scriptName);

            using var reader = new StreamReader(stream);
            var scriptText = reader.ReadToEnd();
            _scriptCache.TryAdd(cacheKey, scriptText);
            return scriptText;
        }

        public IEnumerable<T> Query<T>(string scriptName)
            => NativeConnection.Query<T>(LoadScript(scriptName));
        public IEnumerable<T> Query<T>(string scriptName, object param)
            => NativeConnection.Query<T>(LoadScript(scriptName), param);

        public T ExecuteScalar<T>(string scriptName, T defaultValue)
            => NativeConnection.ExecuteScalar<T>(LoadScript(scriptName)) ?? defaultValue;
        public T ExecuteScalar<T>(string scriptName, object param, T defaultValue)
            => NativeConnection.ExecuteScalar<T>(LoadScript(scriptName), param) ?? defaultValue;

        public T QueryFirst<T>(string scriptName)
            => NativeConnection.QueryFirst<T>(LoadScript(scriptName));
        public T QueryFirst<T>(string scriptName, object param)
            => NativeConnection.QueryFirst<T>(LoadScript(scriptName), param);

        public T QueryFirstOrDefault<T>(string scriptName, T defaultValue)
            => NativeConnection.QueryFirstOrDefault<T>(LoadScript(scriptName)) ?? defaultValue;
        public T QueryFirstOrDefault<T>(string scriptName, object param, T defaultValue)
            => NativeConnection.QueryFirstOrDefault<T>(LoadScript(scriptName), param) ?? defaultValue;

        public T QuerySingle<T>(string scriptName)
            => NativeConnection.QuerySingle<T>(LoadScript(scriptName));
        public T QuerySingle<T>(string scriptName, object param)
            => NativeConnection.QuerySingle<T>(LoadScript(scriptName), param);

        public T QuerySingleOrDefault<T>(string scriptName, T defaultValue)
            => NativeConnection.QuerySingleOrDefault<T>(LoadScript(scriptName)) ?? defaultValue;
        public T QuerySingleOrDefault<T>(string scriptName, object param, T defaultValue)
            => NativeConnection.QuerySingleOrDefault<T>(LoadScript(scriptName), param) ?? defaultValue;

        public T? ExecuteScalar<T>(string scriptName)
            => NativeConnection.ExecuteScalar<T>(LoadScript(scriptName));
        public T? ExecuteScalar<T>(string scriptName, object param)
            => NativeConnection.ExecuteScalar<T>(LoadScript(scriptName), param);

        public T? QueryFirstOrDefault<T>(string scriptName)
            => NativeConnection.QueryFirstOrDefault<T>(LoadScript(scriptName));
        public T? QueryFirstOrDefault<T>(string scriptName, object param)
            => NativeConnection.QueryFirstOrDefault<T>(LoadScript(scriptName), param);

        public T? QuerySingleOrDefault<T>(string scriptName)
            => NativeConnection.QuerySingleOrDefault<T>(LoadScript(scriptName));
        public T? QuerySingleOrDefault<T>(string scriptName, object param)
            => NativeConnection.QuerySingleOrDefault<T>(LoadScript(scriptName), param);

        public void Execute(string scriptName)
            => NativeConnection.Execute(LoadScript(scriptName));
        public void Execute(string scriptName, object param)
            => NativeConnection.Execute(LoadScript(scriptName), param);
    }
}
