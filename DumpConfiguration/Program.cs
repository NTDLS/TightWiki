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
                sb.AppendLine("INSERT INTO ConfigurationGroup(Id, Name, Description)"
                    + $"SELECT {cg.Id}, '{cg.Name}', '{cg.Description}'"
                    + $" WHERE NOT EXISTS(SELECT Id FROM ConfigurationGroup WHERE Id = {cg.Id});");
            }

            var ConfigurationEntries = dbConnection.Query<ConfigurationEntry>("SELECT * FROM ConfigurationEntry");
            foreach (var ce in ConfigurationEntries)
            {
                sb.AppendLine("INSERT INTO ConfigurationEntry(Id, ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)"
                    + $"SELECT {ce.Id}, {ce.ConfigurationGroupId}, '{ce.Name}', '{ce.Value.Replace("'", "''")}', {ce.DataTypeId}, '{ce.Description}', {(ce.IsEncrypted ? 1 : 0)}, {(ce.IsRequired ? 1 : 0)}"
                    + $" WHERE NOT EXISTS(SELECT Id FROM ConfigurationEntry WHERE Id = {ce.Id});");
            }

            foreach (var ce in ConfigurationEntries)
            {
                sb.AppendLine($"UPDATE ConfigurationEntry SET Name = '{ce.Name}', DataTypeId = {ce.DataTypeId}, Description = '{ce.Description}', IsEncrypted = '{(ce.IsEncrypted ? 1 : 0)}', IsRequired = '{(ce.IsRequired ? 1 : 0)}'"
                    + $" WHERE Id = {ce.Id};");
            }

            Console.WriteLine(sb.ToString());
        }
    }
}

