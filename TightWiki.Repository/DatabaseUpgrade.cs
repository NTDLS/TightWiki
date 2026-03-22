using NTDLS.Helpers;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Xml.Linq;
using TightWiki.Library;
using TightWiki.Models.DataModels.Defaults;

namespace TightWiki.Repository
{
    public static class DatabaseUpgrade
    {
        /// <summary>
        /// Gets the current version stored in the VersionState table.
        /// </summary>
        public static string GetVersionStateVersion()
            => ManagedDataStorage.Config.ExecuteScalar<string>(@"Scripts\Initialization\GetVersionStateVersion.sql") ?? "0.0.0";

        /// <summary>
        /// Stores the current assembly version into the VersionState table.
        /// </summary>
        public static void SetVersionStateVersion()
        {
            var version = string.Join('.', //Note that we only care about major.minor.patch hence the Take(3).
                (Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0").Split('.').Take(3));

            ManagedDataStorage.Config.Execute(@"Scripts\Initialization\SetVersionStateVersion.sql", new { Version = version });
        }

        /// <summary>
        /// See @Initialization.Versions.md
        /// Returns true if an upgrade was performed, false if the database was already at the latest version.
        /// </summary>
        public static bool ApplyDatabaseUpgradeScripts()
        {
            try
            {
                string startPreTag = ".Initialization.PreInitialization.";
                string startPostTag = ".Initialization.PostInitialization.";
                string startVersionTag = ".Initialization.Versions.";
                string endVersionTag = ".^";

                var versionString = GetVersionStateVersion();
                int storedPaddedVersion = Utility.PadVersionString(versionString);

                var assembly = Assembly.GetExecutingAssembly();

                int currentPaddedVersion = Utility.PadVersionString( //Note that we only care about major.minor.patch hence the Take(3).
                    string.Join('.', (assembly.GetName().Version?.ToString() ?? "0.0.0.0").Split('.').Take(3)));

                if (currentPaddedVersion == storedPaddedVersion)
                {
                    return false; //The database version is already at the latest version.
                }

                Console.WriteLine($"Starting database initialization.");

                var manifestResources = Assembly.GetExecutingAssembly().GetManifestResourceNames();

                var fullPreInitScriptPaths = manifestResources
                    .Where(o => o.Contains("Repository.Scripts.Initialization.PreInitialization", StringComparison.InvariantCultureIgnoreCase))
                    .OrderBy(o => o);

                foreach (var fullPreInitScriptPath in fullPreInitScriptPaths)
                {
                    //Execute pre-initialization scripts.
                    int startPreTagIndex = fullPreInitScriptPath.IndexOf(startPreTag, StringComparison.InvariantCultureIgnoreCase);
                    if (startPreTagIndex >= 0)
                    {
                        int endIndex = fullPreInitScriptPath.IndexOf(endVersionTag, startPreTagIndex, StringComparison.InvariantCultureIgnoreCase);
                        var scriptName = fullPreInitScriptPath.Substring(endIndex + endVersionTag.Length).Trim().Replace("_", "");
                        ProcessInitializationScript(assembly, fullPreInitScriptPath, scriptName);
                    }
                }

                var fullVersionedInitScriptPaths = manifestResources
                    .Where(o => o.Contains("Repository.Scripts.Initialization.Versions", StringComparison.InvariantCultureIgnoreCase))
                    .OrderBy(o => o);

                foreach (var fullVersionedInitScriptPath in fullVersionedInitScriptPaths)
                {
                    //Execute version based initialization scripts.
                    int startVersionTagIndex = fullVersionedInitScriptPath.IndexOf(startVersionTag, StringComparison.InvariantCultureIgnoreCase);
                    if (startVersionTagIndex >= 0)
                    {
                        startVersionTagIndex += startVersionTag.Length;

                        int endIndex = fullVersionedInitScriptPath.IndexOf(endVersionTag, startVersionTagIndex, StringComparison.InvariantCultureIgnoreCase);
                        if (endIndex > startVersionTagIndex)
                        {
                            //The name of the script file without the namespaces, version numbers etc.
                            var scriptName = fullVersionedInitScriptPath.Substring(endIndex + endVersionTag.Length).Trim().Replace("_", "");

                            int filesFolderVersion = Utility.PadVersionString(fullVersionedInitScriptPath.Substring(startVersionTagIndex, endIndex - startVersionTagIndex).Trim().Replace("_", ""));
                            if (filesFolderVersion > storedPaddedVersion)
                            {
                                ProcessInitializationScript(assembly, fullVersionedInitScriptPath, scriptName);
                            }
                        }
                    }
                }

                var fullPostInitScriptPaths = manifestResources
                    .Where(o => o.Contains("Repository.Scripts.Initialization.PostInitialization", StringComparison.InvariantCultureIgnoreCase))
                    .OrderBy(o => o);

                foreach (var fullPostInitScriptPath in fullPostInitScriptPaths)
                {
                    //Execute post-initialization scripts.
                    int startPostTagIndex = fullPostInitScriptPath.IndexOf(startPostTag, StringComparison.InvariantCultureIgnoreCase);
                    if (startPostTagIndex >= 0)
                    {
                        int endIndex = fullPostInitScriptPath.IndexOf(endVersionTag, startPostTagIndex, StringComparison.InvariantCultureIgnoreCase);
                        var scriptName = fullPostInitScriptPath.Substring(endIndex + endVersionTag.Length).Trim().Replace("_", "");
                        ProcessInitializationScript(assembly, fullPostInitScriptPath, scriptName);
                    }
                }

                SetVersionStateVersion();
                return true;
            }
            catch (Exception ex)
            {
                ExceptionRepository.InsertException(ex, "Database upgrade failed.");
                //Yea, we want to write this to the console so that it can be seen when running manually.
                Console.WriteLine($"Database upgrade failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a copy of the default database file in the configuration directory if it does not already exist, or
        /// overwrites it if specified.
        /// </summary>
        /// <remarks>We do this because WE own the defaults database and we do not want the user to have to put it in the proper place.
        /// after every upgrade.</remarks>
        /// <param name="overwrite">true to overwrite the existing defaults database if it exists; otherwise, false to leave the existing file
        /// unchanged.</param>
        /// <returns>The full path to the created or existing defaults database file, or null if the operation fails.</returns>
        public static string? CreateDefaultsDatabase(bool overwrite)
        {
            try
            {
                var configDatabase = ManagedDataStorage.Config.Ephemeral(o => o.NativeConnection.DataSource);
                var configDatabaseDirectory = Path.GetDirectoryName(configDatabase);
                if (configDatabaseDirectory == null)
                {
                    ExceptionRepository.InsertException("Could not determine the directory for the config database.");
                    return null;
                }
                var defaultsDatabasePath = Path.Combine(configDatabaseDirectory, "defaults.db");

                if (!File.Exists(defaultsDatabasePath) || overwrite)
                {
                    var defaultDatabaseBytes = EmbeddedResourceReader.LoadBytes(@"Defaults\defaults.db");
                    File.WriteAllBytes(defaultsDatabasePath, defaultDatabaseBytes);
                }
                return defaultsDatabasePath;
            }
            catch (Exception ex)
            {
                ExceptionRepository.InsertException(ex, "An error occurred while extracting the default data database.");
            }
            return null;
        }

        public static void ApplyAllSeedData()
        {
            var defaultConfigurationGroups = ManagedDataStorage.Defaults.Query<DefaultConfiguration>(@"Scripts\Defaults\GetDefaultConfigurationGroups.sql");
            foreach (var defaultConfigurationGroup in defaultConfigurationGroups)
            {
                ManagedDataStorage.Config.Execute(@"Scripts\Defaults\Merge\MergeConfigurationGroup.sql",
                    new
                    {
                        Name = defaultConfigurationGroup.ConfigurationGroupName,
                        Description = defaultConfigurationGroup.ConfigurationGroupDescription
                    });
            }

            var defaultConfigurations = ManagedDataStorage.Defaults.Query<DefaultConfiguration>(@"Scripts\Defaults\GetDefaultConfigurations.sql");
            foreach (var defaultConfiguration in defaultConfigurations)
            {
                ManagedDataStorage.Config.Execute(@"Scripts\Defaults\Merge\MergeConfigurationEntry.sql",
                    new
                    {
                        Name = defaultConfiguration.ConfigurationEntryName,
                        Value = defaultConfiguration.Value,
                        DataTypeId = defaultConfiguration.DataTypeId,
                        Description = defaultConfiguration.ConfigurationEntryDescription,
                        IsEncrypted = defaultConfiguration.IsEncrypted,
                        IsRequired = defaultConfiguration.IsRequired,
                        ConfigurationGroupName = defaultConfiguration.ConfigurationGroupName,
                    });
            }


            var defaultFeatureTemplates = ManagedDataStorage.Defaults.Query<DefaultFeatureTemplate>(@"Scripts\Defaults\GetDefaultFeatureTemplates.sql");
            var defaultThemes = ManagedDataStorage.Defaults.Query<DefaultTheme>(@"Scripts\Defaults\GetDefaultThemes.sql");
            var defaultWikiPages = ManagedDataStorage.Defaults.Query<DefaultWikiPage>(@"Scripts\Defaults\GetDefaultWikiPages.sql");


        }

        private static void ProcessInitializationScript(Assembly assembly, string fullUpdateScriptPath, string scriptName)
        {
            Console.WriteLine($"Executing initialization script: \"{fullUpdateScriptPath}\"");

            //Get the script text.
            using var stream = assembly.GetManifestResourceStream(fullUpdateScriptPath);
            using var reader = new StreamReader(stream.EnsureNotNull());
            var scriptText = reader.ReadToEnd();

            //Get the script "metadata" from the file name.
            var scriptNameParts = scriptName.Split('^');
            //string executionOrder = scriptNameParts[0];
            string databaseName = scriptNameParts[1];
            //string scriptName = scriptNameParts[2];

            var databaseFactory = ManagedDataStorage.Collection.Single(o => o.Name == databaseName).Factory;

            bool shouldExecute = true;

            if (scriptText.StartsWith("--##IF", StringComparison.InvariantCultureIgnoreCase))
            {
                int endOfConditional = scriptText.IndexOf('(');
                int endOfFirstLine = scriptText.IndexOf('\n');
                var conditionalTag = scriptText.Substring(4, endOfConditional - 4).Trim().ToUpperInvariant();
                var conditionalParam = scriptText.Substring(endOfConditional + 1, endOfFirstLine - endOfConditional).Trim().Trim(['(', ')']).Trim();

                #region Conditional processing.

                switch (conditionalTag)
                {
                    case "IF EXISTS":
                        var ifExists = databaseFactory.ExecuteScalar<string?>(conditionalParam);
                        shouldExecute = (ifExists == null);
                        break;
                    case "IF NOT EXISTS":
                        var ifNotExists = databaseFactory.ExecuteScalar<string?>(conditionalParam);
                        shouldExecute = (ifNotExists != null);
                        break;
                    case "IF TABLE EXISTS":
                        shouldExecute = databaseFactory.DoesTableExist(conditionalParam);
                        break;
                    case "IF TABLE NOT EXISTS":
                        shouldExecute = !databaseFactory.DoesTableExist(conditionalParam);
                        break;
                    case "IF COLUMN EXISTS":
                        {
                            var param = conditionalParam.Split(',').Select(o => o.Trim()).ToArray();
                            if (param.Length != 2)
                            {
                                throw new Exception("IF COLUMN EXISTS requires two parameters: TABLE_NAME, COLUMN_NAME");
                            }

                            shouldExecute = databaseFactory.DoesColumnExist(param[0], param[1]);
                            break;
                        }
                    case "IF COLUMN NOT EXISTS":
                        {
                            var param = conditionalParam.Split(',').Select(o => o.Trim()).ToArray();
                            if (param.Length != 2)
                            {
                                throw new Exception("IF COLUMN EXISTS requires two parameters: TABLE_NAME, COLUMN_NAME");
                            }

                            shouldExecute = !databaseFactory.DoesColumnExist(param[0], param[1]);
                            break;
                        }
                    case "IF INDEX EXISTS":
                        {
                            var param = conditionalParam.Split(',').Select(o => o.Trim()).ToArray();
                            if (param.Length != 2)
                            {
                                throw new Exception("IF COLUMN EXISTS requires two parameters: TABLE_NAME, INDEX_NAME");
                            }

                            var tableSchema = databaseFactory.DoesIndexExist(param[0], param[1]);
                            break;
                        }
                    case "IF INDEX NOT EXISTS":
                        {
                            var param = conditionalParam.Split(',').Select(o => o.Trim()).ToArray();
                            if (param.Length != 2)
                            {
                                throw new Exception("IF COLUMN EXISTS requires two parameters: TABLE_NAME, INDEX_NAME");
                            }

                            var tableSchema = !databaseFactory.DoesIndexExist(param[0], param[1]);
                            break;
                        }
                    default:
                        throw new Exception(scriptText + " contains an unknown conditional: " + conditionalTag);
                }

                #endregion

                scriptText = scriptText.Substring(endOfFirstLine + 1).Trim();
            }

            if (shouldExecute)
            {
                databaseFactory.Execute(scriptText);
            }
        }
    }
}
