using Dapper;
using Microsoft.Data.Sqlite;
using System.Text;

namespace GenerateSeedData
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("GenerateSeedData.exe <sqliteFilesPath> <outputPath>");
                return;
            }

            var sb = new StringBuilder();

            string outputPath = args[1];

            using var configDb = new SqliteConnection(@$"Data Source={args[0]}\config.db");
            configDb.Open();

            using var pagesDb = new SqliteConnection(@$"Data Source={args[0]}\pages.db");
            pagesDb.Open();

            int index = 0;

            #region ConfigurationGroup.
            sb.Clear();
            var configurationGroups = configDb.Query<ConfigurationGroup>("SELECT * FROM ConfigurationGroup");
            Console.WriteLine("Generating: ConfigurationGroup.");
            foreach (var cg in configurationGroups)
            {
                sb.AppendLine("INSERT INTO ConfigurationGroup(Id, Name, Description)");
                sb.AppendLine($"SELECT {cg.Id}, '{ESQ(cg.Name)}', '{ESQ(cg.Description)}'");
                sb.AppendLine($"ON CONFLICT(Name) DO UPDATE SET Description = '{ESQ(cg.Description)}';");
            }
            File.WriteAllText(@$"{outputPath}\^{index++:D3}^Config^UpsertConfigurationGroup.sql", sb.ToString());
            #endregion

            #region ConfigurationEntry.
            sb.Clear();
            var configurationEntries = configDb.Query<ConfigurationEntry>("SELECT CG.Name as ConfigurationGroupName, CE.Id, CE.ConfigurationGroupId, CE.Name, CE.Value, CE.DataTypeId, CE.Description, CE.IsEncrypted, CE.IsRequired FROM ConfigurationEntry as CE INNER JOIN ConfigurationGroup as CG ON CG.Id = CE.ConfigurationGroupId");
            Console.WriteLine("Generating: ConfigurationEntry.");
            foreach (var ce in configurationEntries)
            {
                sb.AppendLine("INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)");
                sb.AppendLine($"SELECT (SELECT Id FROM ConfigurationGroup WHERE Name = '{ce.ConfigurationGroupName}' LIMIT 1), '{ESQ(ce.Name)}', '{ESQ(ce.Value)}', {ce.DataTypeId}, '{ESQ(ce.Description)}', { (ce.IsEncrypted ? 1 : 0)}, {(ce.IsRequired ? 1 : 0)}");
                sb.AppendLine($"ON CONFLICT(ConfigurationGroupId, Name) DO UPDATE SET Name = '{ESQ(ce.Name)}', DataTypeId = {ce.DataTypeId}, Description = '{ESQ(ce.Description)}', IsEncrypted = '{(ce.IsEncrypted ? 1 : 0)}', IsRequired = '{(ce.IsRequired ? 1 : 0)}';");
            }
            File.WriteAllText(@$"{outputPath}\^{index++:D3}^Config^UpsertConfigurationEntry.sql", sb.ToString());
            #endregion

            #region Theme.
            sb.Clear();
            var themes = configDb.Query<Theme>("SELECT * FROM Theme");
            Console.WriteLine("Generating: Theme.");
            foreach (var t in themes)
            {
                sb.AppendLine("INSERT INTO Theme(Name, DelimitedFiles, ClassNavBar, ClassNavLink, ClassDropdown, ClassBranding, EditorTheme)");
                sb.AppendLine($"SELECT '{ESQ(t.Name)}', '{ESQ(t.DelimitedFiles)}', '{ESQ(t.ClassNavBar)}', '{ESQ(t.ClassNavLink)}', '{ESQ(t.ClassDropdown)}', '{ESQ(t.ClassBranding)}', '{ESQ(t.EditorTheme)}'");
                sb.AppendLine($"ON CONFLICT(Name) DO UPDATE SET DelimitedFiles = '{ESQ(t.DelimitedFiles)}', ClassNavBar = '{ESQ(t.ClassNavBar)}', ClassNavLink = '{ESQ(t.ClassNavLink)}', ClassDropdown = '{ESQ(t.ClassDropdown)}', ClassBranding = '{ESQ(t.ClassBranding)}', EditorTheme = '{ESQ(t.EditorTheme)}';");
            }
            File.WriteAllText(@$"{outputPath}\^{index++:D3}^Config^UpsertTheme.sql", sb.ToString());
            #endregion

            #region FeatureTemplate.
            sb.Clear();
            var featureTemplates = pagesDb.Query<FeatureTemplate>("SELECT FT.Name, FT.Type, P.Name as PageName, FT.Description, FT.TemplateText FROM FeatureTemplate as FT INNER JOIN Page as P ON P.Id = FT.PageId");
            Console.WriteLine("Generating: FeatureTemplate.");
            foreach (var t in featureTemplates)
            {
                sb.AppendLine("INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)");
                sb.AppendLine($"SELECT '{ESQ(t.Name)}', '{ESQ(t.Type)}', (SELECT Id FROM Page WHERE Name = '{ESQ(t.PageName)}' LIMIT 1), '{ESQ(t.Description)}', '{ESQ(t.TemplateText)}'");
                sb.AppendLine($"ON CONFLICT(Name, Type) DO UPDATE SET Type = '{ESQ(t.Type)}', Description = '{ESQ(t.Description)}', TemplateText = '{ESQ(t.TemplateText)}', PageId = (SELECT Id FROM Page WHERE Name = '{ESQ(t.PageName)}' LIMIT 1);");
            }
            File.WriteAllText(@$"{outputPath}\^{index++:D3}^Pages^FeatureTemplate.sql", sb.ToString());
            #endregion
        }

        /// <summary>
        /// Escapes single quotes in a string for SQL queries. Crude, I know.
        /// </summary>
        public static string ESQ(string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;
            return str.Replace("'", "''");
        }

    }
}
