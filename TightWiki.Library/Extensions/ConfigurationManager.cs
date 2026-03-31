using Microsoft.Extensions.Configuration;

namespace TightWiki.Library.Extensions
{
    public static class ConfigurationManagerExtensions
    {
        public static string GetDatabaseConnectionString(this IConfiguration configuration,
            string sectionName, string databaseName, string? deriveFromOnFailback = null)
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

            if (string.IsNullOrEmpty(deriveFromOnFailback))
            {
                throw new Exception(
                    $"No connection string found for section '{sectionName}', no 'DatabasePath' connection string found, and no fallback path provided to derive the database path from.");
            }

            //...or we have to have a valid configuration database path that we can derive the database paths from.
            //This is all in an effort to support "legacy" appsettings.json configurations.
            var deriveFromOnFailbackPath = Path.GetDirectoryName(deriveFromOnFailback)
                ?? throw new Exception("Could not determine the directory for the config database.");

            return Path.Combine(deriveFromOnFailbackPath, databaseName);
        }
    }
}
