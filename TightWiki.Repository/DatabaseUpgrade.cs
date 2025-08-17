using NTDLS.Helpers;
using System.Reflection;
using TightWiki.Library;

namespace TightWiki.Repository
{
    public static class DatabaseUpgrade
    {
        public static string GetVersionStateVersion()
        {
            var entries = ManagedDataStorage.Config.ExecuteScalar<string>(@"Scripts\Initialization\GetVersionStateVersion.sql");
            return entries ?? "0.0.0";
        }

        public static void SetVersionStateVersion()
        {
            var version = string.Join('.',
                (Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0").Split('.').Take(3));
            ManagedDataStorage.Config.Execute(@"Scripts\Initialization\SetVersionStateVersion.sql", new { Version = version });
        }

        /// <summary>
        /// See @Initialization.Versions.md
        /// </summary>
        public static void UpgradeDatabase()
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

                int currentPaddedVersion = Utility.PadVersionString(
                    string.Join('.', (assembly.GetName().Version?.ToString() ?? "0.0.0.0").Split('.').Take(3)));

                if (currentPaddedVersion == storedPaddedVersion)
                {
                    return; //The database version is already at the latest version.
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
            }
            catch (Exception ex)
            {
                ExceptionRepository.InsertException(ex, "Database upgrade failed.");
                //Yea, we want to write this to the console so that it can be seen when running manually.
                Console.WriteLine($"Database upgrade failed: {ex.Message}");
            }
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
                var conditionalTag = scriptText.Substring(4, endOfConditional - 4).Trim();
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

