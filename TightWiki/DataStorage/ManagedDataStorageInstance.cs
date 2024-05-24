using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Reflection;
using System.Runtime.Caching;
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
        public string Directory { get; private set; }

        public SqliteConnection NativeConnection { get; private set; }

        private static readonly Dictionary<string, string> _scriptCache = new();

        private static readonly MemoryCache _reflectionCache = new MemoryCache("ManagedDataStorageInstance");

        public delegate void UseAndDisposeProc(ManagedDataStorageInstance connection);
        public delegate T UseAndDisposeProc<T>(ManagedDataStorageInstance connection);

        public ManagedDataStorageInstance(string connectionString)
        {
            NativeConnection = new SqliteConnection(connectionString);

            Directory = Path.GetFullPath(Path.GetDirectoryName(NativeConnection.DataSource) ?? string.Empty);

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

        public DisposableValueListTable CreateTempTableFrom(string tableName, IEnumerable<string> values, SqliteTransaction transaction)
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

        public DisposableValueListTable CreateTempTableFrom(string tableName, IEnumerable<string> values)
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

        public DisposableValueListTable CreateTempTableFrom(string tableName, IEnumerable<int> values)
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

        public DisposableValueListTable CreateTempTableFrom<T>(string tableName, IEnumerable<T> values)
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

        public DisposableAttachment Attach(string databaseFileName, string alias)
        {
            NativeConnection.Execute($"ATTACH DATABASE '{Directory}\\{databaseFileName}' AS {alias};");
            return new DisposableAttachment(NativeConnection, alias);
        }


        /// <summary>
        /// Returns the given text, or if the script ends with ".sql", the script will be
        /// located and laoded form the executing assembly (assuming it is an embedded resource).
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static string TranslateSqlScript(string script)
        {
            string cacheKey = script.ToLower();

            if (cacheKey.EndsWith(".sql"))
            {
                if (_scriptCache.TryGetValue(cacheKey, out var cachedScriptText))
                {
                    return cachedScriptText;
                }

                var assembly = Assembly.GetExecutingAssembly();

                var allScriptNames = _reflectionCache.Get("TranslateSqlScript:Names") as List<string>;
                if (allScriptNames == null)
                {
                    allScriptNames = assembly.GetManifestResourceNames().Where(o => o.ToLower().EndsWith(".sql")).ToList();
                    _reflectionCache.Add("TranslateSqlScript:Names", allScriptNames, new CacheItemPolicy
                    {
                        SlidingExpiration = new TimeSpan(1, 0, 0)
                    });
                }

                var scripts = allScriptNames.Where(o => o.ToLower().EndsWith(cacheKey)).ToList();
                if (scripts.Count == 0)
                {
                    throw new Exception($"The embedded script resource could not be found: '{script}'");
                }
                else if (scripts.Count > 1)
                {
                    throw new Exception($"The embedded script resource is ambigious. Either make the script name unique or qualifiy it with a namespace: '{script}'");
                }

                using var stream = assembly.GetManifestResourceStream(scripts.First())
                    ?? throw new InvalidOperationException("Script not found: " + script);

                using var reader = new StreamReader(stream);
                var scriptText = reader.ReadToEnd();
                _scriptCache.TryAdd(cacheKey, scriptText);
                return scriptText;
            }

            return script;
        }

        public IEnumerable<T> Query<T>(string scriptName)
            => NativeConnection.Query<T>(TranslateSqlScript(scriptName));
        public IEnumerable<T> Query<T>(string scriptName, object param)
            => NativeConnection.Query<T>(TranslateSqlScript(scriptName), param);

        public T ExecuteScalar<T>(string scriptName, T defaultValue)
            => NativeConnection.ExecuteScalar<T>(TranslateSqlScript(scriptName)) ?? defaultValue;
        public T ExecuteScalar<T>(string scriptName, object param, T defaultValue)
            => NativeConnection.ExecuteScalar<T>(TranslateSqlScript(scriptName), param) ?? defaultValue;

        public T QueryFirst<T>(string scriptName)
            => NativeConnection.QueryFirst<T>(TranslateSqlScript(scriptName));
        public T QueryFirst<T>(string scriptName, object param)
            => NativeConnection.QueryFirst<T>(TranslateSqlScript(scriptName), param);

        public T QueryFirstOrDefault<T>(string scriptName, T defaultValue)
            => NativeConnection.QueryFirstOrDefault<T>(TranslateSqlScript(scriptName)) ?? defaultValue;
        public T QueryFirstOrDefault<T>(string scriptName, object param, T defaultValue)
            => NativeConnection.QueryFirstOrDefault<T>(TranslateSqlScript(scriptName), param) ?? defaultValue;

        public T QuerySingle<T>(string scriptName)
            => NativeConnection.QuerySingle<T>(TranslateSqlScript(scriptName));
        public T QuerySingle<T>(string scriptName, object param)
            => NativeConnection.QuerySingle<T>(TranslateSqlScript(scriptName), param);

        public T QuerySingleOrDefault<T>(string scriptName, T defaultValue)
            => NativeConnection.QuerySingleOrDefault<T>(TranslateSqlScript(scriptName)) ?? defaultValue;
        public T QuerySingleOrDefault<T>(string scriptName, object param, T defaultValue)
            => NativeConnection.QuerySingleOrDefault<T>(TranslateSqlScript(scriptName), param) ?? defaultValue;

        public T? ExecuteScalar<T>(string scriptName)
            => NativeConnection.ExecuteScalar<T>(TranslateSqlScript(scriptName));
        public T? ExecuteScalar<T>(string scriptName, object param)
            => NativeConnection.ExecuteScalar<T>(TranslateSqlScript(scriptName), param);

        public T? QueryFirstOrDefault<T>(string scriptName)
            => NativeConnection.QueryFirstOrDefault<T>(TranslateSqlScript(scriptName));
        public T? QueryFirstOrDefault<T>(string scriptName, object param)
            => NativeConnection.QueryFirstOrDefault<T>(TranslateSqlScript(scriptName), param);

        public T? QuerySingleOrDefault<T>(string scriptName)
            => NativeConnection.QuerySingleOrDefault<T>(TranslateSqlScript(scriptName));
        public T? QuerySingleOrDefault<T>(string scriptName, object param)
            => NativeConnection.QuerySingleOrDefault<T>(TranslateSqlScript(scriptName), param);

        public void Execute(string scriptName)
            => NativeConnection.Execute(TranslateSqlScript(scriptName));
        public void Execute(string scriptName, object param)
            => NativeConnection.Execute(TranslateSqlScript(scriptName), param);
    }
}
