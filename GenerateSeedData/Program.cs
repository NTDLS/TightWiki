using GenerateSeedData.SeedPackage;
using NTDLS.SqliteDapperWrapper;
using System.Text;
using TightWiki.Plugin.Models.Defaults;

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

            string dbPath = args[0];
            string outputPath = args[1];

            File.Delete(Path.Combine(outputPath, "defaults.db"));
            File.Delete(Path.Combine(outputPath, "defaults.db.zip"));

            GenerateDefaultsDatabase(dbPath, outputPath);

            // "Seed\tightwiki.seed.zip" is written next to "Data\" (repo root), by convention, alongside the
            // SQLite defaults.db above - see Database-Providers-Plan.md chapter 4.6b. dbPath's parent is used as
            // the repo root so the existing two-argument invocation (GenerateSeedData.bat) doesn't need to change.
            string repoRoot = Path.GetFullPath(Path.Combine(dbPath, ".."));
            string seedOutputDir = Path.Combine(repoRoot, "Seed");
            Directory.CreateDirectory(seedOutputDir);
            string seedZipPath = Path.Combine(seedOutputDir, "tightwiki.seed.zip");

            SeedPackageGenerator.Generate(dbPath, seedZipPath);
        }


        static void GenerateDefaultsDatabase(string dbPath, string outputPath)
        {
            var sb = new StringBuilder();

            using var configDb = new SqliteManagedInstance(Path.Combine(dbPath, "config.db"));
            using var pagesDb = new SqliteManagedInstance(Path.Combine(dbPath, "pages.db"));
            using var defaults = new SqliteManagedInstance(Path.Combine(outputPath, "defaults.db"));

            #region DefaultConfiguration.

            Console.WriteLine("Generating: DefaultConfiguration.");

            defaults.Execute(@"Scripts\RecreateDefaultConfigurationTable.sql");
            var configurations = configDb.Query<TwDefaultConfiguration>(@"Scripts\GetDefaultConfiguration.sql");
            foreach (var t in configurations)
            {
                sb.Clear();
                sb.AppendLine("INSERT INTO DefaultConfiguration(ConfigurationGroupName, ConfigurationEntryName, Value, DataTypeId, ConfigurationGroupDescription, ConfigurationEntryDescription, IsEncrypted, IsRequired)");
                sb.AppendLine($"SELECT '{ESQ(t.ConfigurationGroupName)}', '{ESQ(t.ConfigurationEntryName)}', '{ESQ(t.Value)}', {t.DataTypeId}, '{ESQ(t.ConfigurationGroupDescription)}','{ESQ(t.ConfigurationEntryDescription)}', {(t.IsEncrypted ? 1 : 0)}, {(t.IsRequired ? 1 : 0)}");
                defaults.Execute(sb.ToString());
            }

            #endregion

            #region Theme.

            Console.WriteLine("Generating: DefaultThemes.");

            defaults.Execute(@"Scripts\RecreateDefaultThemesTable.sql");
            var themes = configDb.Query<TwDefaultTheme>(@"Scripts\GetDefaultThemes.sql");
            foreach (var t in themes)
            {
                sb.Clear();
                sb.AppendLine("INSERT INTO DefaultThemes(Name, DelimitedFiles, ClassNavBar, ClassNavLink, ClassDropdown, ClassBranding, EditorTheme)");
                sb.AppendLine($"SELECT '{ESQ(t.Name)}', '{ESQ(t.DelimitedFiles)}', '{ESQ(t.ClassNavBar)}', '{ESQ(t.ClassNavLink)}', '{ESQ(t.ClassDropdown)}', '{ESQ(t.ClassBranding)}', '{ESQ(t.EditorTheme)}'");
                defaults.Execute(sb.ToString());
            }

            #endregion

            #region FeatureTemplate.

            Console.WriteLine("Generating: DefaultFeatureTemplates.");

            defaults.Execute(@"Scripts\RecreateDefaultFeatureTemplatesTable.sql");
            var templates = pagesDb.Query<TwDefaultFeatureTemplate>(@"Scripts\GetFeatureTemplates.sql");
            foreach (var t in templates)
            {
                sb.Clear();
                sb.AppendLine("INSERT INTO DefaultFeatureTemplates(Name, Type, PageName, Description, TemplateText)");
                sb.AppendLine($"SELECT '{ESQ(t.Name)}', '{ESQ(t.Type)}', '{ESQ(t.PageName)}', '{ESQ(t.Description)}', '{ESQ(t.TemplateText)}'");
                defaults.Execute(sb.ToString());
            }

            #endregion

            #region Default Pages

            Console.WriteLine("Generating: DefaultWikiPages.");

            defaults.Execute(@"Scripts\RecreateDefaultWikiPagesTable.sql");
            var wikiPages = pagesDb.Query<TwDefaultWikiPage>(@"Scripts\GetDefaultDefaultWikiPages.sql");
            foreach (var page in wikiPages)
            {
                sb.Clear();
                sb.AppendLine("INSERT INTO DefaultWikiPages(Id, Name, Namespace, Navigation, Description, CreatedDate, ModifiedDate, Revision, DataHash, Body)");
                sb.AppendLine($"SELECT {page.Id}, '{ESQ(page.Name)}', '{ESQ(page.Namespace)}', '{ESQ(page.Navigation)}', '{ESQ(page.Description)}', '{ESQ(FormatDate(page.CreatedDate))}', '{ESQ(FormatDate(page.ModifiedDate))}', {page.Revision}, {page.DataHash}, '{ESQ(page.Body)}';");
                defaults.Execute(sb.ToString());
            }

            defaults.NativeConnection.Close();
            defaults.NativeConnection.Dispose();
            defaults.Dispose();

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

        /// <summary>
        /// Formats a <see cref="DateTime"/> as SQLite's conventional "yyyy-MM-dd HH:mm:ss.fffffff" text
        /// representation (matching how Data\pages.db itself stores Page.CreatedDate/ModifiedDate), so that
        /// values read back out of defaults.db round-trip to the same DateTime.
        /// </summary>
        public static string FormatDate(DateTime dt)
            => dt.ToString("yyyy-MM-dd HH:mm:ss.fffffff", System.Globalization.CultureInfo.InvariantCulture);

    }
}
