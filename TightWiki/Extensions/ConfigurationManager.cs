using TightWiki.Repository;

namespace TightWiki.Extensions
{
    public static class ConfigurationManagerExtensions
    {
        public static string GetDatabaseConnectionString(this ConfigurationManager configuration, string sectionName, string databaseName)
        {
            //We either have to have a connection string for the database...
            var connectionString = configuration.GetConnectionString(sectionName);
            if (!string.IsNullOrEmpty(connectionString))
            {
                return connectionString;
            }

            //..or we have to have a "DatabasePath".... 
            var databasePath = configuration.GetConnectionString("DatabasePath");
            if (!string.IsNullOrEmpty(databasePath))
            {
                return Path.Combine(databasePath, databaseName);
            }

            //...or we have to have a valid configuration database path that we can derive the database paths from.
            //This is all in an effort to support "legacy" appsettings.json configurations.
            var configDatabase = ManagedDataStorage.Config.Ephemeral(o => o.NativeConnection.DataSource);
            var configDatabasePath = Path.GetDirectoryName(configDatabase)
                ?? throw new Exception("Could not determine the directory for the config database.");

            return Path.Combine(configDatabasePath, databaseName);
        }
    }
}
