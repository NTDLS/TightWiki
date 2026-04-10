using Autofac;
using Autofac.Extensions.DependencyInjection;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TightWiki.Engine;
using TightWiki.Library;
using TightWiki.Library.Dummy;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;
using TightWiki.Repository.Helpers;

namespace TightWiki.Test.Library
{
    public class MockWikiEngineArtifacts
    {
        public ITwEngine Engine { get; private set; }
        public SignInManager<IdentityUser> SignInManager { get; private set; }
        public UserManager<IdentityUser> UserManager { get; private set; }
        public IUserStore<IdentityUser> UserStore { get; private set; }
        public TwVerbatimLocalizationText Localizer { get; private set; }

        public DatabaseManager DatabaseManager { get; private set; }
        public WikiConfigurationManager WikiConfigurationManager { get; private set; }

        public MockWikiEngineArtifacts()
        {
            SqlMapper.AddTypeHandler(new GuidTypeHandler());

            //Creating localizer.
            Localizer = new TwVerbatimLocalizationText();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            DatabaseManager = new DatabaseManager(configuration);
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
                           containerBuilder.RegisterType<ConsoleLogger>().As<ILogger>();
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

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(DatabaseManager.UsersRepository.UsersFactory.Ephemeral(o => o.NativeConnection.ConnectionString)));

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

    }
}
