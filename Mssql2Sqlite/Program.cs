using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace Mssql2Sqlite
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 4 && args.Length != 6)
            {
                Console.WriteLine("Usage for SQL Server integrated security:");
                Console.WriteLine("Mssql2Sqlite.exe <sqliteFile> <userGuid> <sqlServer> <sqlDatabase>");

                Console.WriteLine("");
                Console.WriteLine("Usage for SQL Server user security:");
                Console.WriteLine("Mssql2Sqlite.exe <sqliteFile> <userGuid> <sqlServer> <sqlDatabase> <SQLServerUser> <SQLServerPassword>");

                Console.WriteLine("sqliteFile: The SQLite file to import the data into, must already exist with proper schema. Existing data will be deleted.");
                Console.WriteLine("userGuid: The UserId from the SQLite users.db that you want to associate with the import. Use: 963f0b81-f2ac-488b-9b21-521852641ec4 for admin@tightwiki.com");

                Console.WriteLine("sqlServer: The SQL Server name to export the data from.");
                Console.WriteLine("SQLServerUser: Optional, is the username to use for connecting to SQL Server.");
                Console.WriteLine("SQLServerPassword: Optional, is the password to use for connecting to SQL Server.");
                return;
            }

            string sqliteFile = args[0];
            string userId = args[1];

            var builder = new SqlConnectionStringBuilder
            {
                DataSource = args[2],
                InitialCatalog = args[3],
                Encrypt = SqlConnectionEncryptOption.Optional,
                TrustServerCertificate = true
            };

            if (args.Length == 4)
            {
                builder.IntegratedSecurity = true;
            }
            else if (args.Length == 6)
            {
                builder.IntegratedSecurity = false;
                builder.UserID = args[3];
                builder.Password = args[4];
            }

            MigrateTable("Page", sqliteFile, builder.ToString(), userId);
            MigrateTable("PageFile", sqliteFile, builder.ToString(), userId);
            MigrateTable("PageRevision", sqliteFile, builder.ToString(), userId);
            MigrateTable("PageFileRevision", sqliteFile, builder.ToString(), userId);
            MigrateTable("PageRevisionAttachment", sqliteFile, builder.ToString(), userId);
        }

        static void MigrateTable(string tableName, string sqliteFile, string sqlServerConnectionString, string userId)
        {
            using var sqlServerConnection = new SqlConnection(sqlServerConnectionString);
            var data = sqlServerConnection.Query<dynamic>($"SELECT * FROM [{tableName}]").ToList();
            var insertColumns = sqlServerConnection.Query<string>($"SELECT name from sys.columns where object_id = object_id('{tableName}')").ToList();

            using (var sqliteConnection = new SqliteConnection($"Data Source={sqliteFile}"))
            {
                sqliteConnection.Open();

                // Enable writing of identity columns in SQLite
                sqliteConnection.Execute("PRAGMA foreign_keys=OFF;");
                sqliteConnection.Execute("BEGIN TRANSACTION;");
                sqliteConnection.Execute("PRAGMA defer_foreign_keys=ON;");

                var selectColumns = new List<string>();

                foreach (var column in insertColumns)
                {
                    switch (column)
                    {
                        case "Namespace":
                            selectColumns.Add("Coalesce(@Namespace, '')");
                            break;
                        case "CreatedByUserId":
                        case "ModifiedByUserId":
                            selectColumns.Add($"'{userId}'");
                            break;
                        default:
                            selectColumns.Add($"@{column}");
                            break;
                    }
                }

                if (tableName == "PageFileRevision")
                {
                    insertColumns.Add("CreatedByUserId");
                    selectColumns.Add("(SELECT P.ModifiedByUserId FROM PageFile as PF INNER JOIN Page as P ON P.Id = PF.PageId WHERE PF.Id = @PageFileId)");
                }

                sqliteConnection.Execute($"DELETE FROM {tableName}");

                foreach (var item in data)
                {
                    var insertQuery = $"INSERT INTO {tableName} ({string.Join(",", insertColumns)}) SELECT {string.Join(",", selectColumns)}";
                    sqliteConnection.Execute(insertQuery, (object?)item);
                }

                sqliteConnection.Execute("COMMIT;");
                sqliteConnection.Execute("PRAGMA defer_foreign_keys=OFF;");
                sqliteConnection.Execute("PRAGMA foreign_keys=ON;");
            }

            Console.WriteLine("Data import complete.");
        }
    }
}
