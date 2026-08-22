using Autofac;
using Autofac.Extensions.DependencyInjection;
#if SQLITE_PROVIDER
using Dapper;
#endif
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
using TightWiki.Plugin.Models;
#if SQLITE_PROVIDER
using TightWiki.Repository.Helpers;
#elif SQLSERVER_PROVIDER
using TightWiki.Data.EfCore.SqlServer;
#elif POSTGRES_PROVIDER
using TightWiki.Data.EfCore.Postgres;
#endif

namespace TightWiki.Test.Library
{
    public class MockWikiEngineArtifacts
    {
        public ITwEngine Engine { get; private set; }
        public SignInManager<IdentityUser> SignInManager { get; private set; }
        public UserManager<IdentityUser> UserManager { get; private set; }
        public IUserStore<IdentityUser> UserStore { get; private set; }
        public TwVerbatimLocalizationText Localizer { get; private set; }

        public ITwDatabaseManager DatabaseManager { get; private set; }
        public WikiConfigurationManager WikiConfigurationManager { get; private set; }

        public MockWikiEngineArtifacts()
        {
#if SQLITE_PROVIDER
            SqlMapper.AddTypeHandler(new GuidTypeHandler());
#endif

            //Creating localizer.
            Localizer = new TwVerbatimLocalizationText();

            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

#if SQLSERVER_PROVIDER
            //appsettings.json is shared by every DataProvider, so it can't hold a provider-specific
            //ConnectionStrings:TightWikiEfCore override. Each EF Core provider gets its own dev-only file
            //instead, mirroring TightWiki/Program.cs - see that file's matching comment. Uses a separate,
            //test-only database name (TightWikiTest) so this fixture never touches the manually-seeded dev
            //database (Database-Providers-Testing-Plan.md chapter 5.2).
            configurationBuilder.AddJsonFile("appsettings.Development.SqlServer.json", optional: true, reloadOnChange: true);
#elif POSTGRES_PROVIDER
            //Same as above, for Postgres (test-only database name tightwiki_test).
            configurationBuilder.AddJsonFile("appsettings.Development.Postgres.json", optional: true, reloadOnChange: true);
#endif

            var configuration = configurationBuilder.Build();

#if SQLITE_PROVIDER
            DatabaseManager = new DatabaseManager(configuration);
#elif SQLSERVER_PROVIDER
            DatabaseManager = new SqlServerDatabaseManager(configuration);
#elif POSTGRES_PROVIDER
            DatabaseManager = new PostgresDatabaseManager(configuration);
#endif
            WikiConfigurationManager = new WikiConfigurationManager(configuration, DatabaseManager);

            PluginLoader.LoadPlugins(DatabaseManager.Logger, Environment.CurrentDirectory);

            //Creating host builder.
            var host = Host.CreateDefaultBuilder()
                       .ConfigureAppConfiguration((context, config) =>
                       {
                           config.SetBasePath(AppContext.BaseDirectory)
                                 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                       })
                       .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                       .ConfigureContainer<ContainerBuilder>(containerBuilder =>
                       {
                           containerBuilder.RegisterInstance(configuration);
                           //Fully qualified: TightWiki.Data.EfCore.SqlServer/.Postgres also declare their own
                           //ConsoleLogger (used internally for early logging bootstrap before LoggingRepository
                           //is up), which would otherwise be ambiguous with this one under those providers.
                           containerBuilder.RegisterType<TightWiki.Library.ConsoleLogger>().As<ILogger>();
                           containerBuilder.RegisterInstance(configuration);
                           containerBuilder.RegisterInstance(WikiConfigurationManager);
                           containerBuilder.RegisterInstance(WikiConfigurationManager.WikiConfiguration);
                           containerBuilder.RegisterType<EmailSender>().As<ITwEmailSender>();
                           containerBuilder.RegisterInstance<ITwConfigurationRepository>(DatabaseManager.ConfigurationRepository);
                           containerBuilder.RegisterInstance<ITwLoggingRepository>(DatabaseManager.LoggingRepository);
                           containerBuilder.RegisterInstance<ITwEmojiRepository>(DatabaseManager.EmojiRepository);
                           containerBuilder.RegisterInstance<ITwStatisticsRepository>(DatabaseManager.StatisticsRepository);
                           containerBuilder.RegisterInstance<ITwPageRepository>(DatabaseManager.PageRepository);
                           containerBuilder.RegisterInstance<ITwUsersRepository>(DatabaseManager.UsersRepository);
                           containerBuilder.RegisterInstance<ITwDatabaseManager>(DatabaseManager);
                           containerBuilder.RegisterType<WikiEngine>().As<ITwEngine>().SingleInstance();
                       }).Build();

            //var configuration = host.Services.GetRequiredService<IConfiguration>();

            var services = new ServiceCollection();

            services.AddLogging(configure => configure.AddConsole());

#if SQLITE_PROVIDER
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(GetIdentityConnectionString(configuration)));
#elif SQLSERVER_PROVIDER
            //ASP.NET Identity follows the same driver as the rest of the EF model - same
            //ConnectionStrings:TightWikiEfCore connection string and same provider as SqlServerDatabaseManager,
            //mirroring TightWiki/Program.cs's SQLSERVER_PROVIDER branch.
            var efCoreConnectionString = configuration.GetConnectionString("TightWikiEfCore")
                ?? throw new InvalidOperationException(
                    "Missing connection string 'ConnectionStrings:TightWikiEfCore', which is required when built with -p:DataProvider=SqlServer.");
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(efCoreConnectionString,
                b => b.MigrationsAssembly("TightWiki.Data.EfCore.SqlServer")
                      .MigrationsHistoryTable(SqlServerMigrationsHistory.ApplicationDbTableName, SqlServerMigrationsHistory.ApplicationDbSchema)));
#elif POSTGRES_PROVIDER
            //ASP.NET Identity follows the same driver as the rest of the EF model - same
            //ConnectionStrings:TightWikiEfCore connection string and same provider as PostgresDatabaseManager,
            //mirroring TightWiki/Program.cs's POSTGRES_PROVIDER branch.
            var efCoreConnectionString = configuration.GetConnectionString("TightWikiEfCore")
                ?? throw new InvalidOperationException(
                    "Missing connection string 'ConnectionStrings:TightWikiEfCore', which is required when built with -p:DataProvider=Postgres.");
            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(efCoreConnectionString,
                b => b.MigrationsAssembly("TightWiki.Data.EfCore.Postgres")
                      .MigrationsHistoryTable(PostgresMigrationsHistory.ApplicationDbTableName, PostgresMigrationsHistory.ApplicationDbSchema)));
#endif

            //Register identity services.
            services.AddIdentity<IdentityUser, IdentityRole>()
                    .AddEntityFrameworkStores<ApplicationDbContext>()
                    .AddDefaultTokenProviders();

            //Build service provider.
            var serviceProvider = services.BuildServiceProvider();

            //Resolving services.
            SignInManager = serviceProvider.GetRequiredService<SignInManager<IdentityUser>>();
            UserManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            UserStore = serviceProvider.GetRequiredService<IUserStore<IdentityUser>>();
            Engine = host.Services.GetRequiredService<ITwEngine>();
        }

        public TwPage GetMockPage(string name, string body)
        {
            var profile = Engine.DatabaseManager.UsersRepository.GetAccountProfileByNavigation("admin").Result
                ?? throw new Exception("Admin profile was not found.");

            return new TwPage()
            {
                Name = name,
                Body = body,
                CreatedByUserId = profile.UserId,
                ModifiedByUserId = profile.UserId,
                CreatedDate = DateTime.Parse("1/1/2030 05:00:00"),
                ModifiedDate = DateTime.Parse("1/1/2040 10:00:00"),
                Description = $"The {name} page.",
                Id = 1,
                MostCurrentRevision = 1,
                Revision = 1,
                Navigation = TwNavigation.Clean(name),
            };
        }

#if SQLITE_PROVIDER
        /// <summary>
        /// Derives the SQLite connection string for the users database (used to configure ASP.NET Core
        /// Identity's <see cref="ApplicationDbContext"/>) directly from configuration, using the same
        /// connection-string resolution/normalization that <see cref="TightWiki.Repository.UsersRepository"/>
        /// applies internally - without needing a live repository instance to read it from. Kept SQLite-only
        /// (like the rest of this method's body), mirroring TightWiki/Program.cs's own GetIdentityConnectionString.
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
