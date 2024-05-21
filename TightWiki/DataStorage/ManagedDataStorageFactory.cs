namespace TightWiki.DataStorage
{
    //TODO: Make Nuget package called "DapperWrapper"

    /// <summary>
    /// An instance that creates ManagedDataStorageInstances based off of the connection string stored in this class.
    /// </summary>
    public class ManagedDataStorageFactory
    {
        public string DefaultConnectionString { get; private set; } = string.Empty;

        public delegate void EphemeralProc(ManagedDataStorageInstance connection);
        public delegate T EphemeralProc<T>(ManagedDataStorageInstance connection);

        public ManagedDataStorageFactory(string connectionString)
        {
            DefaultConnectionString = connectionString;
        }

        public ManagedDataStorageFactory()
        {
        }

        public void SetConnectionString(string? connectionString)
        {
            DefaultConnectionString = connectionString ?? string.Empty;
        }

        /// <summary>
        /// Instantiates/opens a SQL connectionusing the default connection string, executes the given delegate and then closed/disposes the connection.
        /// </summary>
        /// <param name="func"></param>
        public void Ephemeral(EphemeralProc func)
        {
            using var connection = new ManagedDataStorageInstance(DefaultConnectionString);
            func(connection);
        }

        /// <summary>
        /// Instantiates/opens a SQL connection using the default connection string, executes the given delegate and then closed/disposes the connection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        public T Ephemeral<T>(EphemeralProc<T> func)
        {
            using var connection = new ManagedDataStorageInstance(DefaultConnectionString);
            return func(connection);
        }

        /// <summary>
        /// Instantiates/opens a SQL connection using the given connection string, executes the given delegate and then closed/disposes the connection.
        /// </summary>
        /// <param name="func"></param>
        public void Ephemeral(string connectionString, EphemeralProc func)
        {
            using var connection = new ManagedDataStorageInstance(connectionString);
            func(connection);
        }

        /// <summary>
        /// Instantiates/opens a SQL connection using the given connection string, executes the given delegate and then closed/disposes the connection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        public T Ephemeral<T>(string connectionString, EphemeralProc<T> func)
        {
            using var connection = new ManagedDataStorageInstance(connectionString);
            return func(connection);
        }

        public IEnumerable<T> Query<T>(string scriptName)
            => Ephemeral(DefaultConnectionString, o => o.Query<T>(scriptName));
        public IEnumerable<T> Query<T>(string scriptName, object param)
            => Ephemeral(DefaultConnectionString, o => o.Query<T>(scriptName, param));

        public T QueryFirst<T>(string scriptName)
            => Ephemeral(DefaultConnectionString, o => o.QueryFirst<T>(scriptName));
        public T QueryFirst<T>(string scriptName, T defaultValue)
            => Ephemeral(DefaultConnectionString, o => o.QueryFirstOrDefault<T>(scriptName)) ?? defaultValue;
        public T QueryFirst<T>(string scriptName, object param)
            => Ephemeral(DefaultConnectionString, o => o.QueryFirst<T>(scriptName, param));
        public T QueryFirst<T>(string scriptName, object param, T defaultValue)
            => Ephemeral(DefaultConnectionString, o => o.QueryFirstOrDefault<T>(scriptName, param)) ?? defaultValue;

        public T QuerySingle<T>(string scriptName)
            => Ephemeral(DefaultConnectionString, o => o.QuerySingle<T>(scriptName));
        public T QuerySingle<T>(string scriptName, T defaultValue)
            => Ephemeral(DefaultConnectionString, o => o.QuerySingleOrDefault<T>(scriptName)) ?? defaultValue;
        public T QuerySingle<T>(string scriptName, object param)
            => Ephemeral(DefaultConnectionString, o => o.QuerySingle<T>(scriptName, param));
        public T QuerySingle<T>(string scriptName, object param, T defaultValue)
            => Ephemeral(DefaultConnectionString, o => o.QuerySingleOrDefault<T>(scriptName, param)) ?? defaultValue;

        public T QuerySingleOrDefault<T>(string scriptName, T defaultValue)
            => Ephemeral(DefaultConnectionString, o => o.QuerySingleOrDefault<T>(scriptName, defaultValue));
        public T QuerySingleOrDefault<T>(string scriptName, object param, T defaultValue)
            => Ephemeral(DefaultConnectionString, o => o.QuerySingleOrDefault<T>(scriptName, param, defaultValue));
        public T? QuerySingleOrDefault<T>(string scriptName)
            => Ephemeral(DefaultConnectionString, o => o.QuerySingleOrDefault<T>(scriptName));
        public T? QuerySingleOrDefault<T>(string scriptName, object param)
            => Ephemeral(DefaultConnectionString, o => o.QuerySingleOrDefault<T>(scriptName, param));

        public T QueryFirstOrDefault<T>(string scriptName, T defaultValue)
            => Ephemeral(DefaultConnectionString, o => o.QueryFirstOrDefault<T>(scriptName, defaultValue));
        public T QueryFirstOrDefault<T>(string scriptName, object param, T defaultValue)
            => Ephemeral(DefaultConnectionString, o => o.QueryFirstOrDefault<T>(scriptName, param, defaultValue));
        public T? QueryFirstOrDefault<T>(string scriptName)
            => Ephemeral(DefaultConnectionString, o => o.QueryFirstOrDefault<T>(scriptName));
        public T? QueryFirstOrDefault<T>(string scriptName, object param)
            => Ephemeral(DefaultConnectionString, o => o.QueryFirstOrDefault<T>(scriptName, param));

        public void Execute(string scriptName)
            => Ephemeral(DefaultConnectionString, o => o.Execute(scriptName));
        public void Execute(string scriptName, object param)
            => Ephemeral(DefaultConnectionString, o => o.Execute(scriptName, param));

        public T ExecuteScalar<T>(string scriptName, T defaultValue)
            => Ephemeral(DefaultConnectionString, o => o.ExecuteScalar<T>(scriptName)) ?? defaultValue;
        public T ExecuteScalar<T>(string scriptName, object param, T defaultValue)
            => Ephemeral(DefaultConnectionString, o => o.ExecuteScalar<T>(scriptName, param)) ?? defaultValue;
        public T? ExecuteScalar<T>(string scriptName)
            => Ephemeral(DefaultConnectionString, o => o.ExecuteScalar<T>(scriptName));
        public T? ExecuteScalar<T>(string scriptName, object param)
            => Ephemeral(DefaultConnectionString, o => o.ExecuteScalar<T>(scriptName, param));
    }
}
