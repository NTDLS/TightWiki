namespace DummyPageGenerator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            /*
            SqlMapper.AddTypeHandler(new GuidTypeHandler());

            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            var configuration = builder.Build();
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
            ManagedDataStorage.Words.SetConnectionString(configuration.GetConnectionString("WordsConnection"));
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
                var workload = pool.CreateQueueStateTracker();

                foreach (var user in pg.Users)
                {
                    workload.Enqueue(() =>
                    {
                        //Create a new page.
                        pg.GeneratePage(user.UserId);

                        //Modify existing pages:
                        int modifications = pg.Random.Next(0, 10);
                        for (int i = 0; i < modifications; i++)
                        {
                            pg.ModifyRandomPages(user.UserId);
                        }
                    });
                }

                workload.WaitForCompletion();
            }
            */
        }
    }
}
