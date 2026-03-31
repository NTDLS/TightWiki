using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NTDLS.Helpers;
using NTDLS.SqliteDapperWrapper;
using System.Reflection;
using System.Security.Claims;
using TightWiki.Library;
using TightWiki.Plugin;
using TightWiki.Plugin.Dummy;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Library;
using TightWiki.Plugin.Models;
using TightWiki.Plugin.Models.Defaults;

namespace TightWiki.Repository.Helpers
{
    public class DatabaseManager
        : ITwDatabaseManager
    {
        public ITwConfigurationRepository ConfigurationRepository { get; private set; }
        public ITwDefaultsRepository DefaultsRepository { get; private set; }
        public ITwEmojiRepository EmojiRepository { get; private set; }
        public ITwLoggingRepository LoggingRepository { get; private set; }
        public ITwPageRepository PageRepository { get; private set; }
        public ITwStatisticsRepository StatisticsRepository { get; private set; }
        public ITwUsersRepository UsersRepository { get; private set; }

        public (string Name, SqliteManagedFactory Factory)[] Databases { get; private set; }

        /// <summary>
        /// We expose this here because it is the earliest we can prop upa database logger.
        /// </summary>
        public ILogger Logger { get; private set; }

        public DatabaseManager(IConfiguration configuration)
        {
            Logger = new ConsoleLogger();

            ConfigurationRepository = new ConfigurationRepository(configuration);

            var defaultsDatabasePath = CreateDefaultsDatabase(configuration).Result
                ?? throw new Exception("Could not determine path to Defaults database.");

            DefaultsRepository = new DefaultsRepository(defaultsDatabasePath);

            LoggingRepository = new LoggingRepository(configuration, ConfigurationRepository);

            var minimumLogLevel = Enum.Parse<LogLevel>(configuration.GetValue("EventLogLevel", LogLevel.Information.ToString()));
            Logger = new DatabaseLogger(LoggingRepository, minimumLogLevel);

            EmojiRepository = new EmojiRepository(configuration, ConfigurationRepository);
            StatisticsRepository = new StatisticsRepository(configuration, ConfigurationRepository);
            PageRepository = new PageRepository(configuration, ConfigurationRepository, StatisticsRepository);
            UsersRepository = new UsersRepository(configuration, ConfigurationRepository);

            Databases =
                [
                    ("DeletedPageRevisions", PageRepository.DeletedPageRevisionsFactory),
                    ("DeletedPages", PageRepository.DeletedPagesFactory),
                    ("Pages", PageRepository.PagesFactory),
                    ("Statistics", StatisticsRepository.StatisticsFactory),
                    ("Emoji", EmojiRepository.EmojiFactory),
                    ("Logging", LoggingRepository.LoggingFactory),
                    ("Users", UsersRepository.UsersFactory),
                    ("Config", ConfigurationRepository.ConfigFactory),
                    //("Defaults", Defaults), //We do not expose this as it is only used for initial seeding of the database.
                ];

        }

        /// <summary>
        /// Gets the current version stored in the VersionState table.
        /// </summary>
        public async Task<string> GetVersionStateVersion()
            => await ConfigurationRepository.ConfigFactory.ExecuteScalarAsync<string>(@"Scripts\Initialization\GetVersionStateVersion.sql") ?? "0.0.0";

        /// <summary>
        /// Stores the current assembly version into the VersionState table.
        /// </summary>
        public async Task SetVersionStateVersion()
        {
            var version = string.Join('.', //Note that we only care about major.minor.patch hence the Take(3).
                (Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0").Split('.').Take(3));

            await ConfigurationRepository.ConfigFactory.ExecuteAsync(@"Scripts\Initialization\SetVersionStateVersion.sql", new { Version = version });
        }

        /// <summary>
        /// See @Initialization.Versions.md
        /// Returns true if an upgrade was performed, false if the database was already at the latest version.
        /// </summary>
        public async Task<bool> ApplyDatabaseUpgradeScripts(ILogger logger)
        {
            try
            {
                string startPreTag = ".Initialization.PreInitialization.";
                string startPostTag = ".Initialization.PostInitialization.";
                string startVersionTag = ".Initialization.Versions.";
                string endVersionTag = ".^";

                var versionString = await GetVersionStateVersion();
                int storedPaddedVersion = Utility.PadVersionString(versionString);

                var assembly = Assembly.GetExecutingAssembly();

                int currentPaddedVersion = Utility.PadVersionString( //Note that we only care about major.minor.patch hence the Take(3).
                    string.Join('.', (assembly.GetName().Version?.ToString() ?? "0.0.0.0").Split('.').Take(3)));

                if (currentPaddedVersion == storedPaddedVersion)
                {
                    return false; //The database version is already at the latest version.
                }

                Logger.LogInformation("Starting database upgrade.");

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
                        await ProcessInitializationScript(logger, assembly, fullPreInitScriptPath, scriptName);
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
                                await ProcessInitializationScript(logger, assembly, fullVersionedInitScriptPath, scriptName);
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
                        await ProcessInitializationScript(logger, assembly, fullPostInitScriptPath, scriptName);
                    }
                }

                await SetVersionStateVersion();
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Database upgrade failed.");
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
        public async Task<string?> CreateDefaultsDatabase(IConfiguration configuration)
        {
            try
            {
                //We have to have a "DatabasePath" or a valid config database path.
                var databasePath = configuration.GetConnectionString("DatabasePath");
                if (string.IsNullOrEmpty(databasePath))
                {
                    var configDatabase = ConfigurationRepository.ConfigFactory.Ephemeral(o => o.NativeConnection.DataSource);
                    databasePath = Path.GetDirectoryName(configDatabase);
                    if (databasePath == null)
                    {
                        await LoggingRepository.WriteException("Could not determine the directory for the config database.");
                        return null;
                    }
                }

                var defaultsDatabasePath = Path.Combine(databasePath, "defaults.db");

                Logger.LogInformation("Creating defaults database.");
                var defaultDatabaseBytes = TwEmbeddedResourceReader.LoadBytes(@"Defaults\defaults.db");
                await File.WriteAllBytesAsync(defaultsDatabasePath, defaultDatabaseBytes);

                return defaultsDatabasePath;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while extracting the default data database.");
            }
            return null;
        }

        public async Task ApplyAllSeedData(ITwSharedLocalizationText localizer, UserManager<IdentityUser> userManager, ITwEngine tightEngine, WikiDefaultDataType[] defaultDataTypes)
        {
            #region Seed: AdminUser.

            var adminUserId = await UsersRepository.UsersFactory.QueryFirstOrDefaultAsync<Guid?>(@"Scripts\Defaults\GetAdminUserId.sql");

            //Check to see if we already have an admin user. If not, we will create one with a random password.
            try
            {
                if (adminUserId == null)
                {
                    Logger.LogWarning($"Admin user with ID {adminUserId} was not found in the identity database. A new admin user will be created.");

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
                        var result = await userManager.CreateAsync(user, TwPasswordGenerator.Generate(32));
                        if (result.Succeeded)
                        {
                            Logger.LogInformation("Database upgrade user created a new account with password.");

                            adminUserId = Guid.Parse(await userManager.GetUserIdAsync(user));

                            await UsersRepository.CreateProfile(adminUserId.Value, user.UserName);

                            var claimsToAdd = new List<Claim> { new("firstname", "Database"), new("lastname", "Upgrade") };

                            await UsersRepository.UpsertUserClaims(userManager, user, claimsToAdd);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while ensuring the existence of an admin user for seeding default wiki pages. Default wiki page seeding will be skipped.");
            }

            if (adminUserId == null)
            {
                Logger.LogError("Database upgrade could not find or create an admin user, which is required for seeding default wiki pages. Default wiki page seeding will be skipped.");
                return;
            }

            #endregion

            #region Seed: Configurations.

            if (defaultDataTypes.Contains(WikiDefaultDataType.Configurations))
            {
                Logger.LogInformation("Seeding default configurations.");

                try
                {
                    var defaultConfigurationGroups = DefaultsRepository.DefaultsFactory.Query<TwDefaultConfiguration>(@"Scripts\Defaults\GetDefaultConfigurationGroups.sql");
                    foreach (var defaultConfigurationGroup in defaultConfigurationGroups)
                    {
                        await ConfigurationRepository.ConfigFactory.ExecuteAsync(@"Scripts\Defaults\Merge\MergeConfigurationGroup.sql",
                            new
                            {
                                Name = defaultConfigurationGroup.ConfigurationGroupName,
                                Description = defaultConfigurationGroup.ConfigurationGroupDescription
                            });
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "An error occurred while seeding default configuration groups.");
                }

                try
                {
                    var defaultConfigurations = DefaultsRepository.DefaultsFactory.Query<TwDefaultConfiguration>(@"Scripts\Defaults\GetDefaultConfigurations.sql");
                    foreach (var defaultConfiguration in defaultConfigurations)
                    {
                        await ConfigurationRepository.ConfigFactory.ExecuteAsync(@"Scripts\Defaults\Merge\MergeConfigurationEntry.sql",
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
                    Logger.LogError(ex, "An error occurred while seeding default configuration entries.");
                }
            }

            #endregion

            #region Seed: Themes.

            if (defaultDataTypes.Contains(WikiDefaultDataType.Themes))
            {
                Logger.LogInformation("Seeding default themes.");
                try
                {
                    var defaultThemes = DefaultsRepository.DefaultsFactory.Query<TwDefaultTheme>(@"Scripts\Defaults\GetDefaultThemes.sql");
                    foreach (var defaultTheme in defaultThemes)
                    {
                        await ConfigurationRepository.ConfigFactory.ExecuteAsync(@"Scripts\Defaults\Merge\MergeTheme.sql",
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
                    Logger.LogError(ex, "An error occurred while seeding default themes.");
                }
            }

            #endregion

            #region Seed: WikiPages.

            if (defaultDataTypes.Contains(WikiDefaultDataType.WikiHelpPages)
                || defaultDataTypes.Contains(WikiDefaultDataType.WikiIncludePages)
                || defaultDataTypes.Contains(WikiDefaultDataType.WikiBuiltinPages))
            {
                Logger.LogInformation("Seeding default wiki pages.");

                try
                {

                    var dummySessionState = new TwDummySessionState();

                    List<TwDefaultWikiPage> defaultWikiPages = new();

                    if (defaultDataTypes.Contains(WikiDefaultDataType.WikiHelpPages))
                    {
                        defaultWikiPages.AddRange(DefaultsRepository.DefaultsFactory.Query<TwDefaultWikiPage>(@"Scripts\Defaults\GetDefaultWikiPages.sql",
                            new { Namespace = "Wiki Help" }));
                    }
                    if (defaultDataTypes.Contains(WikiDefaultDataType.WikiIncludePages))
                    {
                        defaultWikiPages.AddRange(DefaultsRepository.DefaultsFactory.Query<TwDefaultWikiPage>(@"Scripts\Defaults\GetDefaultWikiPages.sql",
                            new { Namespace = "Include" }));
                    }
                    if (defaultDataTypes.Contains(WikiDefaultDataType.WikiBuiltinPages))
                    {
                        defaultWikiPages.AddRange(DefaultsRepository.DefaultsFactory.Query<TwDefaultWikiPage>(@"Scripts\Defaults\GetDefaultWikiPages.sql",
                            new { Namespace = "Builtin" }));
                    }

                    foreach (var defaultWikiPage in defaultWikiPages)
                    {
                        var existingPage = await PageRepository.PagesFactory.QueryFirstOrDefaultAsync<TwPage>(@"Scripts\Defaults\GetPageByNavigation.sql",
                            new { Navigation = defaultWikiPage.Navigation });

                        //if (existingPage == null || existingPage.DataHash != defaultWikiPage.DataHash)
                        {
                            var wikiPage = new TwPage()
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

                            await PageRepository.UpsertPage(tightEngine, localizer, wikiPage, dummySessionState);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "An error occurred while seeding default wiki help pages.");
                }
            }

            #endregion

            #region Seed: FeatureTemplates.

            if (defaultDataTypes.Contains(WikiDefaultDataType.FeatureTemplates))
            {
                try
                {
                    var defaultFeatureTemplates = DefaultsRepository.DefaultsFactory.Query<TwDefaultFeatureTemplate>(@"Scripts\Defaults\GetDefaultFeatureTemplates.sql");
                    foreach (var defaultFeatureTemplate in defaultFeatureTemplates)
                    {
                        await PageRepository.PagesFactory.ExecuteAsync(@"Scripts\Defaults\Merge\MergeFeatureTemplate.sql",
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
                    Logger.LogError(ex, "An error occurred while seeding default feature templates.");
                }
            }

            #endregion
        }

        private async Task ProcessInitializationScript(ILogger logger, Assembly assembly, string fullUpdateScriptPath, string scriptName)
        {
            Logger.LogInformation($"Executing initialization script: \"{fullUpdateScriptPath}\"");

            //Get the script text.
            using var stream = assembly.GetManifestResourceStream(fullUpdateScriptPath);
            using var reader = new StreamReader(stream.EnsureNotNull());
            var scriptText = reader.ReadToEnd();

            //Get the script "metadata" from the file name.
            var scriptNameParts = scriptName.Split('^');
            //string executionOrder = scriptNameParts[0];
            string databaseName = scriptNameParts[1];
            //string scriptName = scriptNameParts[2];

            var databaseFactory = Databases.Single(o => o.Name.Equals(databaseName, StringComparison.InvariantCultureIgnoreCase)).Factory;

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
                await databaseFactory.ExecuteAsync(scriptText);
            }
        }

        #region Database admin.

        public async Task<string> VacuumDatabase(string databaseName)
        {
            var results = await Databases.Single(o => o.Name == databaseName)
                .Factory.QueryAsync<string>("VacuumDatabase.sql");

            return string.Join("\r\n", results);
        }

        public async Task<string> OptimizeDatabase(string databaseName)
        {
            var results = Databases.Single(o => o.Name == databaseName)
                .Factory.Query<string>("OptimizeDatabase.sql");

            return string.Join("\r\n", results);
        }

        public async Task<string> IntegrityCheckDatabase(string databaseName)
        {
            var results = await Databases.Single(o => o.Name == databaseName)
                .Factory.QueryAsync<string>("IntegrityCheckDatabase.sql");

            return string.Join("\r\n", results) + ForeignKeyCheck(databaseName);
        }

        public async Task<string> ForeignKeyCheck(string databaseName)
        {
            var results = await Databases.Single(o => o.Name == databaseName)
                .Factory.QueryAsync<string>("ForeignKeyCheck.sql");

            return string.Join("\r\n", results);
        }

        public async Task<List<(string Name, string Version)>> GetDatabaseVersions()
        {
            var results = new List<(string, string)>();

            foreach (var db in Databases)
            {
                results.Add((db.Name, await db.Factory.ExecuteScalarAsync<string>("GetDatabaseVersion.sql") ?? string.Empty));
            }

            return results;
        }

        public async Task<List<(string Name, int PageCount)>> GetDatabasePageCounts()
        {
            var results = new List<(string, int)>();

            foreach (var db in Databases)
            {
                results.Add((db.Name, await db.Factory.ExecuteScalarAsync<int>("GetDatabasePageCount.sql")));
            }

            return results;
        }

        public async Task<List<(string Name, int PageSize)>> GetDatabasePageSizes()
        {
            var results = new List<(string, int)>();

            foreach (var db in Databases)
            {
                results.Add((db.Name, await db.Factory.ExecuteScalarAsync<int>("GetDatabasePageSize.sql")));
            }

            return results;
        }

        #endregion
    }
}
