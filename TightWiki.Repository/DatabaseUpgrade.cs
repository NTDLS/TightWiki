using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NTDLS.Helpers;
using System.Reflection;
using System.Security.Claims;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Library;
using TightWiki.Models.DataModels.Defaults;
using static TightWiki.Library.Constants;

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
        public static bool ApplyDatabaseUpgradeScripts(ILogger logger)
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

                logger.LogInformation("Starting database upgrade.");

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
                        ProcessInitializationScript(logger, assembly, fullPreInitScriptPath, scriptName);
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
                                ProcessInitializationScript(logger, assembly, fullVersionedInitScriptPath, scriptName);
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
                        ProcessInitializationScript(logger, assembly, fullPostInitScriptPath, scriptName);
                    }
                }

                SetVersionStateVersion();
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database upgrade failed.");
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
        public static string? CreateDefaultsDatabase(ILogger logger, ConfigurationManager configuration, bool overwrite)
        {
            logger.LogInformation("Creating defaults database.");

            try
            {
                //We have to have a "DatabasePath" or a valid config database path.
                var databasePath = configuration.GetConnectionString("DatabasePath");
                if (string.IsNullOrEmpty(databasePath))
                {
                    var configDatabase = ManagedDataStorage.Config.Ephemeral(o => o.NativeConnection.DataSource);
                    databasePath = Path.GetDirectoryName(configDatabase);
                    if (databasePath == null)
                    {
                        LoggingRepository.WriteException("Could not determine the directory for the config database.");
                        return null;
                    }
                }

                var defaultsDatabasePath = Path.Combine(databasePath, "defaults.db");

                if (!File.Exists(defaultsDatabasePath) || overwrite)
                {
                    var defaultDatabaseBytes = EmbeddedResourceReader.LoadBytes(@"Defaults\defaults.db");
                    File.WriteAllBytes(defaultsDatabasePath, defaultDatabaseBytes);
                }
                return defaultsDatabasePath;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while extracting the default data database.");
            }
            return null;
        }

        public static async Task ApplyAllSeedData(ILogger logger, UserManager<IdentityUser> userManager, ITightEngine tightEngine, DefaultDataType[] defaultDataTypes)
        {
            #region Seed: AdminUser.

            var adminUserId = ManagedDataStorage.Users.QueryFirstOrDefault<Guid?>(@"Scripts\Defaults\GetAdminUserId.sql");

            //Check to see if we already have an admin user. If not, we will create one with a random password.
            try
            {
                if (adminUserId == null)
                {
                    logger.LogWarning($"Admin user with ID {adminUserId} was not found in the identity database. A new admin user will be created.");

                    //We couldn't find an admin user, so we will create a new one with a random password.
                    var user = new IdentityUser()
                    {
                        UserName = "admin"
                    };

                    var existingUser = await userManager.FindByNameAsync(user.UserName);

                    if (existingUser != null)
                    {
                        adminUserId = Guid.Parse(existingUser.Id);
                    }
                    else
                    {
                        var result = await userManager.CreateAsync(user, PasswordGenerator.Generate(32));
                        if (result.Succeeded)
                        {
                            logger.LogInformation("Database upgrade user created a new account with password.");

                            adminUserId = Guid.Parse(await userManager.GetUserIdAsync(user));

                            UsersRepository.CreateProfile(adminUserId.Value, user.UserName);

                            var claimsToAdd = new List<Claim> { new("firstname", "Database"), new("lastname", "Upgrade") };

                            SecurityRepository.UpsertUserClaims(userManager, user, claimsToAdd);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while ensuring the existence of an admin user for seeding default wiki pages. Default wiki page seeding will be skipped.");
            }

            if (adminUserId == null)
            {
                logger.LogError("Database upgrade could not find or create an admin user, which is required for seeding default wiki pages. Default wiki page seeding will be skipped.");
                return;
            }

            #endregion

            #region Seed: Configurations.

            if (defaultDataTypes.Contains(DefaultDataType.Configurations))
            {
                logger.LogInformation("Seeding default configurations.");

                try
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
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while seeding default configuration groups.");
                }

                try
                {
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
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while seeding default configuration entries.");
                }
            }

            #endregion

            #region Seed: Themes.

            if (defaultDataTypes.Contains(DefaultDataType.Themes))
            {
                logger.LogInformation("Seeding default themes.");
                try
                {
                    var defaultThemes = ManagedDataStorage.Defaults.Query<DefaultTheme>(@"Scripts\Defaults\GetDefaultThemes.sql");
                    foreach (var defaultTheme in defaultThemes)
                    {
                        ManagedDataStorage.Config.Execute(@"Scripts\Defaults\Merge\MergeTheme.sql",
                            new
                            {
                                Name = defaultTheme.Name,
                                DelimitedFiles = defaultTheme.DelimitedFiles,
                                ClassNavBar = defaultTheme.ClassNavBar,
                                ClassNavLink = defaultTheme.ClassNavLink,
                                ClassDropdown = defaultTheme.ClassDropdown,
                                ClassBranding = defaultTheme.ClassBranding,
                                EditorTheme = defaultTheme.EditorTheme
                            });
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while seeding default themes.");
                }
            }

            #endregion

            #region Seed: WikiPages.

            if (defaultDataTypes.Contains(DefaultDataType.WikiHelpPages)
                || defaultDataTypes.Contains(DefaultDataType.WikiIncludePages)
                || defaultDataTypes.Contains(DefaultDataType.WikiBuiltinPages))
            {
                logger.LogInformation("Seeding default wiki pages.");

                try
                {
                    var dummySessionState = new DummySessionState();

                    List<DefaultWikiPage> defaultWikiPages = new();

                    if (defaultDataTypes.Contains(DefaultDataType.WikiHelpPages))
                    {
                        defaultWikiPages.AddRange(ManagedDataStorage.Defaults.Query<DefaultWikiPage>(@"Scripts\Defaults\GetDefaultWikiPages.sql",
                            new { Namespace = "Wiki Help" }));
                    }
                    if (defaultDataTypes.Contains(DefaultDataType.WikiIncludePages))
                    {
                        defaultWikiPages.AddRange(ManagedDataStorage.Defaults.Query<DefaultWikiPage>(@"Scripts\Defaults\GetDefaultWikiPages.sql",
                            new { Namespace = "Include" }));
                    }
                    if (defaultDataTypes.Contains(DefaultDataType.WikiBuiltinPages))
                    {
                        defaultWikiPages.AddRange(ManagedDataStorage.Defaults.Query<DefaultWikiPage>(@"Scripts\Defaults\GetDefaultWikiPages.sql",
                            new { Namespace = "Builtin" }));
                    }

                    foreach (var defaultWikiPage in defaultWikiPages)
                    {
                        var existingPage = ManagedDataStorage.Pages.QueryFirstOrDefault<Models.DataModels.Page>(@"Scripts\Defaults\GetPageByNavigation.sql",
                            new { Navigation = defaultWikiPage.Navigation });

                        //if (existingPage == null || existingPage.DataHash != defaultWikiPage.DataHash)
                        {
                            var wikiPage = new Models.DataModels.Page()
                            {
                                Id = existingPage?.Id ?? 0,
                                Name = defaultWikiPage.Name,
                                Navigation = defaultWikiPage.Navigation,
                                Description = defaultWikiPage.Description,
                                Body = defaultWikiPage.Body,
                                DataHash = defaultWikiPage.DataHash,
                                CreatedByUserId = adminUserId.Value,
                                ModifiedByUserId = adminUserId.Value,
                                CreatedDate = DateTime.UtcNow,
                                ModifiedDate = DateTime.UtcNow
                            };

                            RepositoryHelpers.UpsertPage(tightEngine, wikiPage, dummySessionState);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while seeding default wiki help pages.");
                }
            }

            #endregion

            #region Seed: FeatureTemplates.

            if (defaultDataTypes.Contains(DefaultDataType.FeatureTemplates))
            {
                try
                {
                    var defaultFeatureTemplates = ManagedDataStorage.Defaults.Query<DefaultFeatureTemplate>(@"Scripts\Defaults\GetDefaultFeatureTemplates.sql");
                    foreach (var defaultFeatureTemplate in defaultFeatureTemplates)
                    {
                        ManagedDataStorage.Pages.Execute(@"Scripts\Defaults\Merge\MergeFeatureTemplate.sql",
                            new
                            {
                                Name = defaultFeatureTemplate.Name,
                                Type = defaultFeatureTemplate.Type,
                                PageName = defaultFeatureTemplate.PageName,
                                Description = defaultFeatureTemplate.Description,
                                TemplateText = defaultFeatureTemplate.TemplateText
                            });
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while seeding default feature templates.");
                }
            }

            #endregion
        }

        private static void ProcessInitializationScript(ILogger logger, Assembly assembly, string fullUpdateScriptPath, string scriptName)
        {
            logger.LogInformation($"Executing initialization script: \"{fullUpdateScriptPath}\"");

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
