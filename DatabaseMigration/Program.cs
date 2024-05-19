using Dapper;
using Microsoft.Data.Sqlite;

namespace DatabaseMigration
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MigrateTable("ConfigurationGroup");
            MigrateTable("CryptoCheck");
            MigrateTable("DataType");
            MigrateTable("Emoji");
            MigrateTable("Exception");
            MigrateTable("MenuItem");
            MigrateTable("Role");
            MigrateTable("ConfigurationEntry");
            MigrateTable("EmojiCategory");
            MigrateTable("User");
            MigrateTable("Page");
            MigrateTable("PageComment");
            MigrateTable("PageFile");
            MigrateTable("PageProcessingInstruction");
            MigrateTable("PageReference");
            MigrateTable("PageRevision");
            MigrateTable("PageStatistics");
            MigrateTable("PageTag");
            MigrateTable("PageToken");
            MigrateTable("PageFileRevision");
            MigrateTable("PageRevisionAttachment");
        }

        static void MigrateTable(string tableName)
        {
            string sqlServerConnectionString = "Data Source=.;Initial Catalog=TightWiki;Integrated Security=True;TrustServerCertificate=True";
            string sqliteConnectionString = "Data Source=C:\\NTDLS\\TightWikiV2\\TightWiki\\Data\\tightwikidata.db";

            using var sqlServerConnection = new Microsoft.Data.SqlClient.SqlConnection(sqlServerConnectionString);

            var data = sqlServerConnection.Query<dynamic>($"SELECT * FROM [{tableName}]").ToList();
            var columns = sqlServerConnection.Query<string>($"SELECT name from sys.columns where object_id = object_id('{tableName}')").ToList();

            // Insert data into SQLite, preserving identity values
            using (var sqliteConnection = new SqliteConnection(sqliteConnectionString))
            {
                sqliteConnection.Open();

                // Enable writing of identity columns in SQLite
                sqliteConnection.Execute("PRAGMA foreign_keys=OFF;");
                sqliteConnection.Execute("BEGIN TRANSACTION;");
                sqliteConnection.Execute("PRAGMA defer_foreign_keys=ON;");

                foreach (var item in data)
                {
                    var insertQuery = $"INSERT INTO {tableName} ({string.Join(",", columns)}) VALUES (@{string.Join(",@", columns)})";
                    sqliteConnection.Execute(insertQuery, (object?)item);
                }

                sqliteConnection.Execute("COMMIT;");
                sqliteConnection.Execute("PRAGMA defer_foreign_keys=OFF;");
                sqliteConnection.Execute("PRAGMA foreign_keys=ON;");
            }

            Console.WriteLine("Data transfer complete.");
        }
    }
}
