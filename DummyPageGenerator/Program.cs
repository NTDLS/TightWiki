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
using TightWiki.Engine.Implementation;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Library;
using TightWiki.Repository;

namespace DummyPageGenerator
{
    internal class Program
    {
        public class NoOpCompletionHandler : ICompletionHandler
        {
            public void Complete(ITightEngineState state)
            {
            }
        }

        static void Main(string[] args)
        {
            SqlMapper.AddTypeHandler(new GuidTypeHandler());

            var host = Host.CreateDefaultBuilder(args)
                       .ConfigureAppConfiguration((context, config) =>
                       {
                           config.SetBasePath(AppContext.BaseDirectory)
                                 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                       })
                       .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                       .ConfigureContainer<ContainerBuilder>(containerBuilder =>
                       {
                           containerBuilder.RegisterType<StandardFunctionHandler>().As<IStandardFunctionHandler>().SingleInstance();
                           containerBuilder.RegisterType<ScopeFunctionHandler>().As<IScopeFunctionHandler>().SingleInstance();
                           containerBuilder.RegisterType<ProcessingInstructionFunctionHandler>().As<IProcessingInstructionFunctionHandler>().SingleInstance();
                           containerBuilder.RegisterType<PostProcessingFunctionHandler>().As<IPostProcessingFunctionHandler>().SingleInstance();
                           containerBuilder.RegisterType<MarkupHandler>().As<IMarkupHandler>().SingleInstance();
                           containerBuilder.RegisterType<HeadingHandler>().As<IHeadingHandler>().SingleInstance();
                           containerBuilder.RegisterType<CommentHandler>().As<ICommentHandler>().SingleInstance();
                           containerBuilder.RegisterType<EmojiHandler>().As<IEmojiHandler>().SingleInstance();
                           containerBuilder.RegisterType<ExternalLinkHandler>().As<IExternalLinkHandler>().SingleInstance();
                           containerBuilder.RegisterType<InternalLinkHandler>().As<IInternalLinkHandler>().SingleInstance();
                           containerBuilder.RegisterType<ExceptionHandler>().As<IExceptionHandler>().SingleInstance();
                           containerBuilder.RegisterType<NoOpCompletionHandler>().As<ICompletionHandler>().SingleInstance();

                           containerBuilder.RegisterType<TightEngine>();
                       }).Build();


            var configuration = host.Services.GetRequiredService<IConfiguration>();
            var services = new ServiceCollection();

            services.AddLogging(configure => configure.AddConsole());

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("UsersConnection")));

            ManagedDataStorage.Pages.SetConnectionString(configuration.GetConnectionString("PagesConnection"));
            ManagedDataStorage.DeletedPages.SetConnectionString(configuration.GetConnectionString("DeletedPagesConnection"));
            ManagedDataStorage.DeletedPageRevisions.SetConnectionString(configuration.GetConnectionString("DeletedPageRevisionsConnection"));
            ManagedDataStorage.Statistics.SetConnectionString(configuration.GetConnectionString("StatisticsConnection"));
            ManagedDataStorage.Emoji.SetConnectionString(configuration.GetConnectionString("EmojiConnection"));
            ManagedDataStorage.Exceptions.SetConnectionString(configuration.GetConnectionString("ExceptionsConnection"));
            WordsRepository.Words.SetConnectionString(configuration.GetConnectionString("WordsConnection"));
            ManagedDataStorage.Users.SetConnectionString(configuration.GetConnectionString("UsersConnection"));
            ManagedDataStorage.Config.SetConnectionString(configuration.GetConnectionString("ConfigConnection"));

            // Register Identity services
            services.AddIdentity<IdentityUser, IdentityRole>()
                    .AddEntityFrameworkStores<ApplicationDbContext>()
                    .AddDefaultTokenProviders();

            var serviceProvider = services.BuildServiceProvider();

            // Resolve the services
            var signInManager = serviceProvider.GetRequiredService<SignInManager<IdentityUser>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var userStore = serviceProvider.GetRequiredService<IUserStore<IdentityUser>>();

            ConfigurationRepository.ReloadEverything();

            var pg = new PageGenerator(userManager);

            var pool = new NTDLS.DelegateThreadPooling.DelegateThreadPool(4, 0);

            while (true)
            {
                var workload = pool.CreateChildQueue();

                foreach (var user in pg.Users)
                {
                    workload.Enqueue(() =>
                    {
                        using var scope = host.Services.CreateScope();
                        var engine = scope.ServiceProvider.GetRequiredService<TightEngine>();

                        //Create a new page:
                        pg.GeneratePage(engine, user.UserId);

                        //Modify existing pages:
                        int modifications = pg.Random.Next(0, 10);
                        for (int i = 0; i < modifications; i++)
                        {
                            pg.ModifyRandomPages(engine, user.UserId);
                        }
                    });
                }

                workload.WaitForCompletion();
            }
        }
    }
}
