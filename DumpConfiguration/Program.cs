using Dapper;
using Microsoft.Data.Sqlite;
using System.Text;

namespace DumpConfiguration
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("DumpConfiguration.exe <sqliteFile>");
                return;
            }

            var sb = new StringBuilder();

            using var dbConnection = new SqliteConnection($"Data Source={args[0]}");
            dbConnection.Open();

            var configurationGroups = dbConnection.Query<ConfigurationGroup>("SELECT * FROM ConfigurationGroup");
            foreach (var cg in configurationGroups)
            {
                sb.AppendLine("INSERT INTO ConfigurationGroup(Id, Name, Description)");
                sb.AppendLine($"SELECT {cg.Id}, '{cg.Name}', '{cg.Description}'");
                sb.AppendLine($"ON CONFLICT(Name) DO UPDATE SET Description = '{cg.Description}';");
            }

            var ConfigurationEntries = dbConnection.Query<ConfigurationEntry>("SELECT * FROM ConfigurationEntry");
            foreach (var ce in ConfigurationEntries)
            {
                sb.AppendLine("INSERT INTO ConfigurationEntry(Id, ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)");
                sb.AppendLine($"SELECT {ce.Id}, {ce.ConfigurationGroupId}, '{ce.Name}', '{ce.Value.Replace("'", "''")}', {ce.DataTypeId}, '{ce.Description}', {(ce.IsEncrypted ? 1 : 0)}, {(ce.IsRequired ? 1 : 0)}");
                sb.AppendLine($"ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = '{ce.Name}', DataTypeId = {ce.DataTypeId}, Description = '{ce.Description}', IsEncrypted = '{(ce.IsEncrypted ? 1 : 0)}', IsRequired = '{(ce.IsRequired ? 1 : 0)}';");
            }

            Console.WriteLine(sb.ToString());
        }
    }
}
