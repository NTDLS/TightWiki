namespace TightWiki.Test.Library
{
    /*
    public class MockWikiEngineArtifacts
    {
        public ITwEngine Engine { get; private set; }
        public SignInManager<IdentityUser> SignInManager { get; private set; }
        public UserManager<IdentityUser> UserManager { get; private set; }
        public IUserStore<IdentityUser> UserStore { get; private set; }
        public TwVerbatimLocalizationText Localizer { get; private set; }

        public MockWikiEngineArtifacts()
        {
            SqlMapper.AddTypeHandler(new TwGuidTypeHandler());

            //Creating localizer.
            Localizer = new TwVerbatimLocalizationText();

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
                           containerBuilder.RegisterType<MarkupHandler>().As<ITwMarkupHandler>().SingleInstance();
                           containerBuilder.RegisterType<HeadingHandler>().As<ITwHeadingHandler>().SingleInstance();
                           containerBuilder.RegisterType<CommentHandler>().As<ITwCommentHandler>().SingleInstance();
                           containerBuilder.RegisterType<EmojiHandler>().As<ITwEmojiHandler>().SingleInstance();
                           containerBuilder.RegisterType<ExternalLinkHandler>().As<ITwExternalLinkHandler>().SingleInstance();
                           containerBuilder.RegisterType<InternalLinkHandler>().As<ITwInternalLinkHandler>().SingleInstance();
                           containerBuilder.RegisterType<ExceptionHandler>().As<ITwExceptionHandler>().SingleInstance();
                           containerBuilder.RegisterType<NoOpCompletionHandler>().As<ITwCompletionHandler>().SingleInstance();
                           containerBuilder.RegisterType<TwEngine>().As<ITwEngine>().SingleInstance();
                       }).Build();

            //Resolving config services.
            var configuration = host.Services.GetRequiredService<IConfiguration>();

            //Setting database contexts.
            ManagedDataStorage.Config.SetConnectionString(configuration.GetDatabaseConnectionString("ConfigConnection", "config.db"));

            var configDatabaseFile = configurationRepository.ConfigFactory.Ephemeral(o => o.NativeConnection.DataSource);

            ManagedDataStorage.Logging.SetConnectionString(configuration.GetDatabaseConnectionString("LoggingConnection", "logging.db", configDatabaseFile));
            ManagedDataStorage.Pages.SetConnectionString(configuration.GetDatabaseConnectionString("PagesConnection", "pages.db", configDatabaseFile));
            ManagedDataStorage.DeletedPages.SetConnectionString(configuration.GetDatabaseConnectionString("DeletedPagesConnection", "deletedpages.db", configDatabaseFile));
            ManagedDataStorage.DeletedPageRevisions.SetConnectionString(configuration.GetDatabaseConnectionString("DeletedPageRevisionsConnection", "deletedpagerevisions.db", configDatabaseFile));
            ManagedDataStorage.Statistics.SetConnectionString(configuration.GetDatabaseConnectionString("StatisticsConnection", "statistics.db", configDatabaseFile));
            ManagedDataStorage.Emoji.SetConnectionString(configuration.GetDatabaseConnectionString("EmojiConnection", "emoji.db", configDatabaseFile));
            ManagedDataStorage.Users.SetConnectionString(configuration.GetDatabaseConnectionString("UsersConnection", "users.db", configDatabaseFile));

            var services = new ServiceCollection();

            services.AddLogging(configure => configure.AddConsole());

            services.AddDbContext<TwApplicationDbContext>(options =>
                options.UseSqlite(ManagedDataStorage.Users.Ephemeral(o => o.NativeConnection.ConnectionString)));

            //Register identity services.
            services.AddIdentity<IdentityUser, IdentityRole>()
                    .AddEntityFrameworkStores<TwApplicationDbContext>()
                    .AddDefaultTokenProviders();

            //Build service provider.
            var serviceProvider = services.BuildServiceProvider();

            //Resolving services.
            SignInManager = serviceProvider.GetRequiredService<SignInManager<IdentityUser>>();
            UserManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            UserStore = serviceProvider.GetRequiredService<IUserStore<IdentityUser>>();
            Engine = host.Services.GetRequiredService<ITwEngine>();

            //Loading all settings.
            //ConfigurationRepository.ReloadEverything().Wait();
        }

        public async Task CreatePage(string navigation, string createdByAccountNavigaion, List<string>? tags = null)
        {
            try
            {
                var profile = await UsersRepository.GetAccountProfileByNavigation(TwNavigation.Clean(createdByAccountNavigaion));
                var body = new StringBuilder();

                body.AppendLine($"##title");
                if (tags?.Count > 0)
                {
                    body.AppendLine($"$##Tag(\"{string.Join("\",\"", tags)})\"");
                }
                body.AppendLine($"##toc");

                body.AppendLine($"==Overview");
                body.AppendLine($"The {navigation} page.");
                body.AppendLine("\r\n");

                body.AppendLine($"==Details");
                body.AppendLine($"This is a test page.");

                body.AppendLine($"==Related");
                body.AppendLine($"##related");
                body.AppendLine("\r\n");

                var page = new TwPage()
                {
                    Name = navigation,
                    Body = body.ToString(),
                    CreatedByUserId = profile.UserId,
                    ModifiedByUserId = profile.UserId,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    Description = "This is just a test page.",
                };

                var localizer = new TwVerbatimLocalizationText();
                int newPageId = await RepositoryHelpers.UpsertPage(Engine, localizer, page);

                var fileName = "testFile.txt";
                var fileData = Encoding.UTF8.GetBytes(page.Body);
                await AttachFile(newPageId, profile.UserId, fileName, fileData);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task AttachFile(int pageId, Guid userId, string fileName, byte[] fileData)
        {
            //if (fileData.Length > GlobalConfiguration.MaxAttachmentFileSize)
            {
                //    throw new Exception("Could not save the attached file, too large");
            }

            await PageFileRepository.UpsertPageFile(new TwPageFileAttachment()
            {
                Data = fileData,
                CreatedDate = DateTime.UtcNow,
                PageId = pageId,
                Name = fileName,
                FileNavigation = TwNavigation.Clean(fileName),
                Size = fileData.Length,
                ContentType = Utility.GetMimeType(fileName)
            }, userId);
        }

        public async Task CreateUserAndProfile(string emailAddress, string accountName, string password)
        {
            var user = new IdentityUser()
            {
                UserName = emailAddress,
                Email = emailAddress
            };

            var result = UserManager.CreateAsync(user, password).Result;
            if (!result.Succeeded)
            {
                throw new Exception(string.Join("\r\n", result.Errors.Select(o => o.Description)));
            }

            var userId = UserManager.GetUserIdAsync(user).Result;
            var membershipConfig = await ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Membership);

            await UsersRepository.CreateProfile(Guid.Parse(userId), accountName);

            var claimsToAdd = new List<Claim>
                    {
                        new (ClaimTypes.Role, membershipConfig.Value<string>("Default Signup Role").EnsureNotNull()),
                        new ("timezone", membershipConfig.Value<string>("Default TimeZone").EnsureNotNull()),
                        new (ClaimTypes.Country, membershipConfig.Value<string>("Default Country").EnsureNotNull()),
                        new ("language", membershipConfig.Value<string>("Default Language").EnsureNotNull()),
                    };

            await SecurityRepository.UpsertUserClaims(UserManager, user, claimsToAdd);
        }
    }
    */
}
