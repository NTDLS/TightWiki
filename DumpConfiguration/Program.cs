using Dapper;
using Microsoft.Data.Sqlite;
using System.Text;

namespace DumpConfiguration
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("DumpConfiguration.exe <sqliteFile> <outputPath>");
                return;
            }

            var sb = new StringBuilder();

            string outputPath = args[1];

            using var dbConnection = new SqliteConnection($"Data Source={args[0]}");
            dbConnection.Open();

            int index = 0;

            sb.Clear();
            var configurationGroups = dbConnection.Query<ConfigurationGroup>("SELECT * FROM ConfigurationGroup");
            Console.WriteLine("Generating: ConfigurationGroup.");
            foreach (var cg in configurationGroups)
            {
                sb.AppendLine("INSERT INTO ConfigurationGroup(Id, Name, Description)");
                sb.AppendLine($"SELECT {cg.Id}, '{cg.Name}', '{cg.Description}'");
                sb.AppendLine($"ON CONFLICT(Name) DO UPDATE SET Description = '{cg.Description}';");
            }
            File.WriteAllText(@$"{outputPath}\^{index++:D3}^Config^UpsertConfigurationGroup.sql", sb.ToString());

            sb.Clear();
            var configurationEntries = dbConnection.Query<ConfigurationEntry>("SELECT * FROM ConfigurationEntry");
            Console.WriteLine("Generating: ConfigurationEntry.");
            foreach (var ce in configurationEntries)
            {
                sb.AppendLine("INSERT INTO ConfigurationEntry(Id, ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)");
                sb.AppendLine($"SELECT {ce.Id}, {ce.ConfigurationGroupId}, '{ce.Name}', '{ce.Value.Replace("'", "''")}', {ce.DataTypeId}, '{ce.Description}', {(ce.IsEncrypted ? 1 : 0)}, {(ce.IsRequired ? 1 : 0)}");
                sb.AppendLine($"ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = '{ce.Name}', DataTypeId = {ce.DataTypeId}, Description = '{ce.Description}', IsEncrypted = '{(ce.IsEncrypted ? 1 : 0)}', IsRequired = '{(ce.IsRequired ? 1 : 0)}';");
            }
            File.WriteAllText(@$"{outputPath}\^{index++:D3}^Config^UpsertConfigurationEntry.sql", sb.ToString());

            sb.Clear();
            var themes = dbConnection.Query<Theme>("SELECT * FROM Theme");
            Console.WriteLine("Generating: Theme.");
            foreach (var t in themes)
            {
                sb.AppendLine("INSERT INTO Theme(Name, DelimitedFiles, ClassNavBar, ClassNavLink, ClassDropdown, ClassBranding, EditorTheme)");
                sb.AppendLine($"SELECT '{t.Name}', '{t.DelimitedFiles}', '{t.ClassNavBar}', '{t.ClassNavLink}', '{t.ClassDropdown}', '{t.ClassBranding}', '{t.EditorTheme}'");
                sb.AppendLine($"ON CONFLICT(Name) DO UPDATE SET DelimitedFiles = '{{t.DelimitedFiles}}', ClassNavBar = {t.ClassNavBar}, ClassNavLink = '{t.ClassNavLink}', ClassDropdown = '{t.ClassDropdown}', ClassBranding = '{t.ClassBranding}, EditorTheme = '{t.EditorTheme}';");
            }
            File.WriteAllText(@$"{outputPath}\^{index++:D3}^Config^UpsertTheme.sql", sb.ToString());
        }
    }
}
