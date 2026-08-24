using Autofac;
using Autofac.Extensions.DependencyInjection;
#if SQLITE_PROVIDER
using Dapper;
#endif
using DiffPlex;
using DiffPlex.DiffBuilder;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NTDLS.Helpers;
#if SQLITE_PROVIDER
using NTDLS.SqliteDapperWrapper;
#endif
using TightWiki.Engine;
using TightWiki.Library;
using TightWiki.Library.Dummy;
using TightWiki.Library.Extensions;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;
#if SQLITE_PROVIDER
using TightWiki.Repository.Helpers;
#elif SQLSERVER_PROVIDER
using TightWiki.Data.EfCore.SqlServer;
#elif POSTGRES_PROVIDER
using TightWiki.Data.EfCore.Postgres;
#endif
using TightWiki.Translations;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
#if SQLITE_PROVIDER
            SqlMapper.AddTypeHandler(new GuidTypeHandler());
#endif

            var builder = WebApplication.CreateBuilder(args);

#if SQLSERVER_PROVIDER || POSTGRES_PROVIDER
            //appsettings.Development.json is shared by every DataProvider, so it can't hold a provider-specific
            //ConnectionStrings:TightWikiEfCore override. Each EF Core provider gets its own dev-only file instead,
            //loaded before databaseManager below reads the connection string.
            if (builder.Environment.IsDevelopment())
            {
#if SQLSERVER_PROVIDER
                builder.Configuration.AddJsonFile("appsettings.Development.SqlServer.json", optional: true, reloadOnChange: true);
#elif POSTGRES_PROVIDER
                builder.Configuration.AddJsonFile("appsettings.Development.Postgres.json", optional: true, reloadOnChange: true);
#endif
            }
#endif

#if SQLITE_PROVIDER
            ITwDatabaseManager databaseManager = new DatabaseManager(builder.Configuration);
#elif SQLSERVER_PROVIDER
            ITwDatabaseManager databaseManager = new SqlServerDatabaseManager(builder.Configuration);
#elif POSTGRES_PROVIDER
            ITwDatabaseManager databaseManager = new PostgresDatabaseManager(builder.Configuration);
#endif
            bool wasDatabaseUpgraded = await databaseManager.InitializeSchema();

#if SQLSERVER_PROVIDER || POSTGRES_PROVIDER
            //WikiConfigurationManager is constructed further down (still before builder.Build()) and eagerly
            //reads Config.Theme (WikiConfigurationManager.ReloadAll: .Single(o => o.Name == themeName)), which is
            //empty on a freshly migrated-but-unseeded MSSQL database and crashes the app before Kestrel ever
            //starts listening (see Database-Providers-Plan.md phase 2a.10). SeedContentDataAsync is the DI-free
            //half of ApplyAllSeedData (everything except EnsureAdminUser, which needs a
            //UserManager<IdentityUser> that only exists once the DI container below is built) - see its doc
            //comment on SqlServerDatabaseManager for how this call and the later, post-Build ApplyAllSeedData
            //call (below, inside app.Services.CreateScope()) divide the seeding work between them. Same
            //wasDatabaseUpgraded gate as that later call.
            //BuiltinPages ("Wiki Page Does Not Exist"/"Wiki Page Revision Does Not Exist"/etc. - see
            //TwDefaultDataType.BuiltinPages's own doc comment, "Core built-in wiki pages") is included here (and
            //in the later, post-Build ApplyAllSeedData call below) only for the SQL Server/Postgres builds. SQLite
            //needs no equivalent: those pages already exist unconditionally the moment DatabaseManager.CreateDefaultsDatabase
            //copies the shipped, pre-populated Data\pages.db file - this selective, TwDefaultDataType-gated reseed
            //is only ever a redundant, idempotent no-op refresh for SQLite (matched by navigation, see
            //DatabaseManager.ApplyAllSeedData), never how those pages get there in the first place. MSSQL/PostgreSQL
            //have no such file-copy shortcut (SqlServerDatabaseManager/PostgresDatabaseManager.SeedContentDataAsync's
            //own doc comment), so without this flag the fallback configured pages ("Page Not Exists Page"/"Revision
            //Does Not Exists Page") never get seeded at all, and every navigation to a nonexistent page - including
            //the very first request to the home page on a brand new install - throws an unhandled exception
            //(PageController.Display's .EnsureNotNull() on a null GetPageRevisionByNavigation result). Confirmed
            //live against SQL Server LocalDB.
            if (wasDatabaseUpgraded)
            {
#if SQLSERVER_PROVIDER
                await ((SqlServerDatabaseManager)databaseManager).SeedContentDataAsync(
#elif POSTGRES_PROVIDER
                await ((PostgresDatabaseManager)databaseManager).SeedContentDataAsync(
#endif
                    [TwDefaultDataType.Themes,
                    TwDefaultDataType.Configurations,
                    TwDefaultDataType.FeatureTemplates,
                    TwDefaultDataType.HelpPages,
                    TwDefaultDataType.BuiltinPages,
                    TwDefaultDataType.IncludePages,
                    TwDefaultDataType.RootPages,
                    TwDefaultDataType.SandboxPages]);
            }
#endif

            //This is the minimum log level for the database logger, which is used for logging application events and errors to the database.
            var minimumLogLevel = Enum.Parse<LogLevel>(builder.Configuration.GetValue("EventLogLevel", LogLevel.Information.ToString()));

            builder.Logging.ClearProviders();
            builder.Logging.AddProvider(new DatabaseLoggerProvider(databaseManager.LoggingRepository, minimumLogLevel));

#if SQLITE_PROVIDER
            var userConnectionString = GetIdentityConnectionString(builder.Configuration);
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(userConnectionString));
#elif SQLSERVER_PROVIDER
            //ASP.NET Identity follows the same driver as the rest of the EF model (Database-Providers-Plan.md
            //chapter 4.1.1 - "stejná databáze, schéma Users"): same ConnectionStrings:TightWikiEfCore connection
            //string and same provider as SqlServerDatabaseManager/TightWikiDbContext.
            var efCoreConnectionString = builder.Configuration.GetConnectionString("TightWikiEfCore")
                ?? throw new InvalidOperationException(
                    "Missing connection string 'ConnectionStrings:TightWikiEfCore', which is required when built with -p:DataProvider=SqlServer.");
            //MigrationsAssembly points at TightWiki.Data.EfCore.SqlServer - see the matching comment on
            //SqlServerDatabaseManager.CreateApplicationDbContext, which applies these same migrations at startup.
            //MigrationsHistoryTable is likewise explicit and distinct from TightWikiDbContext's - see
            //SqlServerMigrationsHistory for why two DbContexts over one database must not share EF Core's
            //default dbo.__EFMigrationsHistory table.
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(efCoreConnectionString,
                b => b.MigrationsAssembly("TightWiki.Data.EfCore.SqlServer")
                      .MigrationsHistoryTable(SqlServerMigrationsHistory.ApplicationDbTableName, SqlServerMigrationsHistory.ApplicationDbSchema)));
#elif POSTGRES_PROVIDER
            //ASP.NET Identity follows the same driver as the rest of the EF model (Database-Providers-Plan.md
            //chapter 4.1.1 - "stejná databáze, schéma Users"): same ConnectionStrings:TightWikiEfCore connection
            //string and same provider as PostgresDatabaseManager/TightWikiDbContext.
            var efCoreConnectionString = builder.Configuration.GetConnectionString("TightWikiEfCore")
                ?? throw new InvalidOperationException(
                    "Missing connection string 'ConnectionStrings:TightWikiEfCore', which is required when built with -p:DataProvider=Postgres.");
            //MigrationsAssembly points at TightWiki.Data.EfCore.Postgres - see the matching comment on
            //PostgresDatabaseManager.CreateApplicationDbContext, which applies these same migrations at startup.
            //MigrationsHistoryTable is likewise explicit and distinct from TightWikiDbContext's - see
            //PostgresMigrationsHistory for why two DbContexts over one database must not share EF Core's
            //default public.__EFMigrationsHistory table.
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(efCoreConnectionString,
                b => b.MigrationsAssembly("TightWiki.Data.EfCore.Postgres")
                      .MigrationsHistoryTable(PostgresMigrationsHistory.ApplicationDbTableName, PostgresMigrationsHistory.ApplicationDbSchema)));
#endif

            var wikiConfigurationManager = new WikiConfigurationManager(builder.Configuration, databaseManager);

            // Add DiffPlex services.
            builder.Services.AddScoped<IDiffer, Differ>();
            builder.Services.AddScoped<ISideBySideDiffBuilder>(sp =>
                new SideBySideDiffBuilder(sp.GetRequiredService<IDiffer>()));

            var membershipConfig = await databaseManager.ConfigurationRepository.GetConfigurationEntryValuesByGroupName(TwConfigGroup.Membership);
            var requireConfirmedAccount = membershipConfig.Value<bool>("Require Email Verification");

            // Add services to the container.
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            // Adds support for controllers and views
            builder.Services.AddControllersWithViews(config =>
                {
                    config.ModelBinderProviders.Insert(0, new TwInvariantDecimalModelBinderProvider());
                })
                .AddDataAnnotationsLocalization()
                .AddXmlSerializerFormatters()
                .AddXmlDataContractSerializerFormatters();

            builder.Services.AddLocalization(options =>
            {
                options.ResourcesPath = "";
            });

            builder.Services.AddScoped<ITwSharedLocalizationText, SharedLocalizationText>();

            builder.Services.AddRazorPages();

            var supportedCultures = new SupportedCultures();
            builder.Services.AddSingleton(x => supportedCultures);

            builder.Services.Configure<RequestLocalizationOptions>(opts =>
            {
                opts.DefaultRequestCulture = new RequestCulture("en");
                // Formatting numbers, dates, etc.
                opts.SupportedCultures = supportedCultures.UICompleteCultures;
                // UI strings that we have localized.
                opts.SupportedUICultures = supportedCultures.UICompleteCultures;

                opts.RequestCultureProviders = new List<IRequestCultureProvider>
                {
                    //new Routing.LanguageRouteRequestCultureProvider(supportedCultures),
                    new QueryStringRequestCultureProvider(),
                    new CookieRequestCultureProvider(),
                    new AcceptLanguageHeaderRequestCultureProvider(),
                };
            });
            builder.Services.AddSingleton<RequestLocalizationOptions>();
            //builder.Services.AddSingleton<ITwManagedDataStorage>(dataStuff);
            builder.Services.AddSingleton(wikiConfigurationManager);
            builder.Services.AddSingleton(wikiConfigurationManager.WikiConfiguration);
            builder.Services.AddSingleton<ITwEmailSender, EmailSender>();
            builder.Services.AddSingleton<ITwConfigurationRepository>(databaseManager.ConfigurationRepository);
            builder.Services.AddSingleton<ITwLoggingRepository>(databaseManager.LoggingRepository);
            builder.Services.AddSingleton<ITwEmojiRepository>(databaseManager.EmojiRepository);
            builder.Services.AddSingleton<ITwStatisticsRepository>(databaseManager.StatisticsRepository);
            builder.Services.AddSingleton<ITwPageRepository>(databaseManager.PageRepository);
            builder.Services.AddSingleton<ITwUsersRepository>(databaseManager.UsersRepository);
            builder.Services.AddSingleton<ITwDatabaseManager>(databaseManager);
            builder.Services.AddSingleton<ISpannedRepository>((ISpannedRepository)databaseManager);

            builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = requireConfirmedAccount)
                .AddEntityFrameworkStores<ApplicationDbContext>();

            var externalAuthenticationConfig = await databaseManager.ConfigurationRepository.GetConfigurationEntryValuesByGroupName(TwConfigGroup.ExternalAuthentication);
            var basicConfig = await databaseManager.ConfigurationRepository.GetConfigurationEntryValuesByGroupName(TwConfigGroup.Basic);
            var cookiesConfig = await databaseManager.ConfigurationRepository.GetConfigurationEntryValuesByGroupName(TwConfigGroup.Cookies);

            var authentication = builder.Services.AddAuthentication()
                .AddCookie("CookieAuth", options =>
                {
                    options.Cookie.Name = basicConfig.Value<string>("Name").EnsureNotNull();
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.LoginPath = $"{wikiConfigurationManager.WikiConfiguration.BasePath}/Identity/Account/Login";
                    options.ExpireTimeSpan = TimeSpan.FromHours(cookiesConfig.Value<int>("Expiration Hours"));
                    options.SlidingExpiration = true;
                    options.Cookie.IsEssential = true;

                });

            var persistKeysPath = cookiesConfig.Value("Persist Keys Path", string.Empty);
            if (string.IsNullOrEmpty(persistKeysPath) == false)
            {
                if (CanReadWrite(persistKeysPath))
                {
                    // Add persistent data protection
                    builder.Services.AddDataProtection()
                        .PersistKeysToFileSystem(new DirectoryInfo(persistKeysPath))
                        .SetApplicationName(basicConfig.Value<string>("Name").EnsureNotNull());
                }
                else
                {
                    await databaseManager.LoggingRepository.WriteException($"Cannot read/write to the specified path for persistent keys: {persistKeysPath}. Check the configuration and path permission.");
                }
            }

            if (externalAuthenticationConfig.Value<bool>("Google : Use Google Authentication"))
            {
                var clientId = externalAuthenticationConfig.Value<string>("Google : ClientId");
                var clientSecret = externalAuthenticationConfig.Value<string>("Google : ClientSecret");

                if (clientId != null && clientSecret != null && !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
                {
                    authentication.AddGoogle(options =>
                    {
                        options.ClientId = clientId;
                        options.ClientSecret = clientSecret;

                        options.Events = new OAuthEvents
                        {
                            OnRemoteFailure = context =>
                            {
                                context.Response.Redirect($"{wikiConfigurationManager.WikiConfiguration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString("External login was canceled.")}");
                                context.HandleResponse();
                                return Task.CompletedTask;
                            }
                        };
                    });
                }
            }

            if (externalAuthenticationConfig.Value<bool>("Microsoft : Use Microsoft Authentication"))
            {
                var clientId = externalAuthenticationConfig.Value<string>("Microsoft : ClientId");
                var clientSecret = externalAuthenticationConfig.Value<string>("Microsoft : ClientSecret");

                if (clientId != null && clientSecret != null && !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
                {
                    authentication.AddMicrosoftAccount(options =>
                    {
                        options.ClientId = clientId;
                        options.ClientSecret = clientSecret;

                        options.Events = new OAuthEvents
                        {
                            OnRemoteFailure = context =>
                            {
                                context.Response.Redirect($"{wikiConfigurationManager.WikiConfiguration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString("External login was canceled.")}");
                                context.HandleResponse();
                                return Task.CompletedTask;
                            }
                        };

                    });
                }
            }

            if (externalAuthenticationConfig.Value<bool>("OIDC : Use OIDC Authentication"))
            {
                var authority = externalAuthenticationConfig.Value<string>("OIDC : Authority");
                var clientId = externalAuthenticationConfig.Value<string>("OIDC : ClientId");
                var clientSecret = externalAuthenticationConfig.Value<string>("OIDC : ClientSecret");

                if (!string.IsNullOrEmpty(authority) && !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
                {
                    authentication.AddOpenIdConnect("oidc", options =>
                    {
                        options.Authority = authority;
                        options.ClientId = clientId;
                        options.ClientSecret = clientSecret;
                        options.ResponseType = "code";

                        options.SaveTokens = true;
                        options.GetClaimsFromUserInfoEndpoint = true;

                        options.Scope.Clear();
                        options.Scope.Add("openid");
                        options.Scope.Add("profile");
                        options.Scope.Add("email");

                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            NameClaimType = "name",
                            RoleClaimType = "role"
                        };

                        options.Events = new OpenIdConnectEvents
                        {
                            OnRemoteFailure = context =>
                            {
                                context.Response.Redirect($"{wikiConfigurationManager.WikiConfiguration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString("OIDC login was canceled.")}");
                                context.HandleResponse();
                                return Task.CompletedTask;
                            }
                        };
                    });
                }
            }

            var pluginFolder = Path.Combine(Environment.CurrentDirectory, "Plugins");
            PluginLoader.LoadPlugins(databaseManager.Logger, pluginFolder);

            builder.Services.AddControllersWithViews();

            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
            builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
            {
                containerBuilder.RegisterType<WikiEngine>().As<ITwEngine>().SingleInstance();
            });

            builder.Services.ConfigureApplicationCookie(options =>
            {
                if (!string.IsNullOrEmpty(wikiConfigurationManager.WikiConfiguration.BasePath))
                {
                    options.LoginPath = new PathString($"{wikiConfigurationManager.WikiConfiguration.BasePath}/Identity/Account/Login");
                    options.LogoutPath = new PathString($"{wikiConfigurationManager.WikiConfiguration.BasePath}/Identity/Account/Logout");
                    options.AccessDeniedPath = new PathString($"{wikiConfigurationManager.WikiConfiguration.BasePath}/Identity/Account/AccessDenied");
                    options.Cookie.Path = wikiConfigurationManager.WikiConfiguration.BasePath; // Ensure the cookie is scoped to the sub-site path.
                }
                else
                {
                    options.LoginPath = new PathString("/Identity/Account/Login");
                    options.LogoutPath = new PathString("/Identity/Account/Logout");
                    options.AccessDeniedPath = new PathString("/Identity/Account/AccessDenied");
                    options.Cookie.Path = "/"; // Use root path if no base path is set.
                }
            });

            var app = builder.Build();

            //Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            if (!string.IsNullOrEmpty(wikiConfigurationManager.WikiConfiguration.BasePath))
            {
                app.UsePathBase(wikiConfigurationManager.WikiConfiguration.BasePath);

                // Redirect root requests to basePath (something like '/TightWiki').
                app.Use(async (context, next) =>
                {
                    if (context.Request.Path == "/")
                    {
                        context.Response.Redirect(wikiConfigurationManager.WikiConfiguration.BasePath);
                        return;
                    }
                    await next();
                });

                app.UseStaticFiles(new StaticFileOptions
                {
                    OnPrepareResponse = ctx =>
                    {
                        ctx.Context.Request.PathBase = wikiConfigurationManager.WikiConfiguration.BasePath;
                    }
                });
            }

            //We are just going to use one giant resource file for all the shared strings in the application for simplicity.
            //This makes it easy to scan the code and add missing source language entries to the resource file, as well as to find and reuse existing entries.
            LocalizerFactory.Initialize(app.Services);

            var localizationOptions = app.Services
                .GetRequiredService<IOptions<RequestLocalizationOptions>>()
                .Value;

            app.UseRequestLocalization(localizationOptions);

            app.UseRouting();

            app.UseAuthentication(); // Ensures the authentication middleware is configured
            app.UseAuthorization();

            app.MapRazorPages();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Page}/{action=Display}");

            app.MapControllerRoute(
                name: "Page_Edit",
                pattern: "Page/{givenCanonical}/Edit");

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();
                var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
                var tightEngine = services.GetRequiredService<ITwEngine>();

                if (wasDatabaseUpgraded)
                {
                    try
                    {
                        //See the matching comment on the earlier, pre-Build SeedContentDataAsync call
                        //(#if SQLSERVER_PROVIDER || POSTGRES_PROVIDER, above) for why TwDefaultDataType.BuiltinPages
                        //is added only for the SQL Server/Postgres builds here and left untouched (so genuinely
                        //0-diff) for SQLite - this is the call that actually performs the seed (the earlier one is
                        //always a no-op for wiki pages specifically, since no admin Users.Profile row exists yet at
                        //that point).
                        await databaseManager.ApplyAllSeedData(new TwVerbatimLocalizationText(), userManager, tightEngine,
                            [TwDefaultDataType.Themes,
                            TwDefaultDataType.Configurations,
                            TwDefaultDataType.FeatureTemplates,
                            TwDefaultDataType.HelpPages,
#if SQLSERVER_PROVIDER || POSTGRES_PROVIDER
                            TwDefaultDataType.BuiltinPages,
                            TwDefaultDataType.IncludePages,
                            TwDefaultDataType.RootPages,
                            TwDefaultDataType.SandboxPages,
#endif
                            ]);

                        await wikiConfigurationManager.ReloadAll();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "An error occurred while applying seed data after database upgrade.");
                    }
                }

                try
                {
                    databaseManager.UsersRepository.ValidateEncryptionAndCreateAdminUser(userManager);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while validating encryption or creating the admin user.");
                }
            }

            /*
            using (var scope = app.Services.CreateScope())
            {
                var tightEngine = scope.ServiceProvider.GetRequiredService<ITwEngine>();
                var selfDocument = new SelfDocument(tightEngine);
                await selfDocument.CreateNotExisting();
            }
            */

            app.Run();
        }

        private static bool CanReadWrite(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                string tempFilePath = Path.Combine(path, Path.GetRandomFileName());
                File.WriteAllText(tempFilePath, "test");
                File.Delete(tempFilePath);
            }
            catch
            {
                return false;
            }

            return true;
        }

#if SQLITE_PROVIDER
        /// <summary>
        /// Derives the SQLite connection string for the users database (used to configure ASP.NET Core
        /// Identity's <see cref="ApplicationDbContext"/>) directly from configuration, using the same
        /// connection-string resolution/normalization that <c>TightWiki.Repository.UsersRepository</c>
        /// applies internally - without needing a live repository instance to read it from. Referenced by
        /// name rather than <c>cref</c> since this type is only present when built with DataProvider=Sqlite.
        /// </summary>
        private static string GetIdentityConnectionString(IConfiguration configuration)
        {
            var configConnectionString = configuration.GetDatabaseConnectionString("ConfigConnection", "config.db");
            var configDatabaseFile = new SqliteManagedFactory(configConnectionString).Ephemeral(o => o.NativeConnection.DataSource);

            var safeUsersDbPath = Path.Combine(Path.GetDirectoryName(configDatabaseFile)
                ?? throw new Exception("Could not determine directory of configuration database file"), "users.db");

            var usersConnectionString = configuration.GetDatabaseConnectionString("UsersConnection", "users.db", safeUsersDbPath);

            return new SqliteManagedFactory(usersConnectionString).Ephemeral(o => o.NativeConnection.ConnectionString);
        }
#endif
    }
}
