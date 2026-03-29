using Autofac;
using Autofac.Extensions.DependencyInjection;
using Dapper;
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
using NTDLS.SqliteDapperWrapper;
using TightWiki.Email;
using TightWiki.Engine;
using TightWiki.Engine.Implementation.Handlers;
using TightWiki.Library;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.Repository;
using TightWiki.Repository.Extensions;
using TightWiki.Translations;
using static TightWiki.Plugin.Constants;

namespace TightWiki
{
    public class Program
    {
        public class DataStuff
            : ITwManagedDataStorage
        {
            public SqliteManagedFactory DeletedPageRevisions { get; private set; }
            public SqliteManagedFactory DeletedPages { get; private set; }
            public SqliteManagedFactory Pages { get; private set; }
            public SqliteManagedFactory Statistics { get; private set; }
            public SqliteManagedFactory Emoji { get; private set; }
            public SqliteManagedFactory Logging { get; private set; }
            public SqliteManagedFactory Users { get; private set; }
            public SqliteManagedFactory Config { get; private set; }
            public SqliteManagedFactory Defaults { get; private set; }

            public bool WasDatabaseUpgraded { get; private set; }

            public (string Name, SqliteManagedFactory Factory)[] Collection { get; private set; }

            public DataStuff(IConfiguration configuration, ILogger logger)
            {
                Config = new(configuration.GetDatabaseConnectionString("ConfigConnection", "config.db"));
                Logging = new(configuration.GetDatabaseConnectionString("LoggingConnection", "logging.db"));
                Pages = new(configuration.GetDatabaseConnectionString("PagesConnection", "pages.db"));
                DeletedPages = new(configuration.GetDatabaseConnectionString("DeletedPagesConnection", "deletedpages.db"));
                DeletedPageRevisions = new(configuration.GetDatabaseConnectionString("DeletedPageRevisionsConnection", "deletedpagerevisions.db"));
                Statistics = new(configuration.GetDatabaseConnectionString("StatisticsConnection", "statistics.db"));
                Emoji = new(configuration.GetDatabaseConnectionString("EmojiConnection", "emoji.db"));
                Users = new(configuration.GetDatabaseConnectionString("UsersConnection", "users.db"));

                //Upgrade database if needed and create defaults database if needed. This is done at the very beginning before
                //  almost anything else to ensure the database is in the correct state for the rest of the application to work with.
                //
                // Default data is seeded further down after the injection of ILogger, UserManager, and ITightEngine,
                //  via a call to DatabaseUpgrade.ApplyAllSeedData()
                WasDatabaseUpgraded = DatabaseUpgrade.ApplyDatabaseUpgradeScripts(logger).Result;

                var defaultsDatabasePath = DatabaseUpgrade.CreateDefaultsDatabase(logger, configuration, WasDatabaseUpgraded).Result
                    ?? throw new Exception("Could not determine path to Defaults database.");

                Defaults = new(defaultsDatabasePath);

                Collection =
                    [
                        ("DeletedPageRevisions", DeletedPageRevisions),
                        ("DeletedPages", DeletedPages),
                        ("Pages", Pages),
                        ("Statistics", Statistics),
                        ("Emoji", Emoji),
                        ("Logging", Logging),
                        ("Users", Users),
                        ("Config", Config),
                        //("Defaults", Defaults), //We do not expose this as it is only used for initial seeding of the database.
                    ];
            }
        }

        public static async Task Main(string[] args)
        {
            SqlMapper.AddTypeHandler(new GuidTypeHandler());

            var builder = WebApplication.CreateBuilder(args);

            //This is the minimum log level for the database logger, which is used for logging application events and errors to the database.
            var minimumLogLevel = Enum.Parse<LogLevel>(builder.Configuration.GetValue("EventLogLevel", LogLevel.Information.ToString()));

            builder.Logging.ClearProviders();
            builder.Logging.AddProvider(new DatabaseLoggerProvider(minimumLogLevel));

            //var independentLogger = ;

            var dataStuff = new DataStuff(builder.Configuration, new DatabaseLogger("", LogLevel.Information));

            var userConnectionString = dataStuff.Users.Ephemeral(o => o.NativeConnection.ConnectionString);
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(userConnectionString));

            await LoggingRepository.CreateTablesIfNotExist();

            var wikiConfiguration = await WikiConfigurationFactory.Create(builder.Configuration);

            // Add DiffPlex services.
            builder.Services.AddScoped<IDiffer, Differ>();
            builder.Services.AddScoped<ISideBySideDiffBuilder>(sp =>
                new SideBySideDiffBuilder(sp.GetRequiredService<IDiffer>()));

            var membershipConfig = await ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Membership);
            var requireConfirmedAccount = membershipConfig.Value<bool>("Require Email Verification");

            // Add services to the container.
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            // Adds support for controllers and views
            builder.Services.AddControllersWithViews(config =>
                {
                    config.ModelBinderProviders.Insert(0, new InvariantDecimalModelBinderProvider());
                })
                .AddDataAnnotationsLocalization()
                .AddXmlSerializerFormatters()
                .AddXmlDataContractSerializerFormatters();

            builder.Services.AddLocalization(options =>
            {
                options.ResourcesPath = "";
            });

            builder.Services.AddScoped<ISharedLocalizationText, SharedLocalizationText>();

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
            builder.Services.AddSingleton<ITwManagedDataStorage>(dataStuff);
            builder.Services.AddSingleton(wikiConfiguration);

            builder.Services.AddSingleton<IWikiEmailSender, WikiEmailSender>();

            builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = requireConfirmedAccount)
                .AddEntityFrameworkStores<ApplicationDbContext>();

            var externalAuthenticationConfig = await ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.ExternalAuthentication);
            var basicConfig = await ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Basic);
            var cookiesConfig = await ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Cookies);

            var authentication = builder.Services.AddAuthentication()
                .AddCookie("CookieAuth", options =>
                {
                    options.Cookie.Name = basicConfig.Value<string>("Name").EnsureNotNull();
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.LoginPath = $"{wikiConfiguration.BasePath}/Identity/Account/Login";
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
                    await LoggingRepository.WriteException($"Cannot read/write to the specified path for persistent keys: {persistKeysPath}. Check the configuration and path permission.");
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
                                context.Response.Redirect($"{wikiConfiguration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString("External login was canceled.")}");
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
                                context.Response.Redirect($"{wikiConfiguration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString("External login was canceled.")}");
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
                                context.Response.Redirect($"{wikiConfiguration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString("OIDC login was canceled.")}");
                                context.HandleResponse();
                                return Task.CompletedTask;
                            }
                        };
                    });
                }
            }

            builder.Services.AddControllersWithViews();

            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
            builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
            {
                containerBuilder.RegisterType<MarkupHandler>().As<IMarkupHandler>().SingleInstance();
                containerBuilder.RegisterType<HeadingHandler>().As<IHeadingHandler>().SingleInstance();
                containerBuilder.RegisterType<CommentHandler>().As<ICommentHandler>().SingleInstance();
                containerBuilder.RegisterType<EmojiHandler>().As<IEmojiHandler>().SingleInstance();
                containerBuilder.RegisterType<ExternalLinkHandler>().As<IExternalLinkHandler>().SingleInstance();
                containerBuilder.RegisterType<InternalLinkHandler>().As<IInternalLinkHandler>().SingleInstance();
                containerBuilder.RegisterType<ExceptionHandler>().As<IExceptionHandler>().SingleInstance();
                containerBuilder.RegisterType<CompletionHandler>().As<ICompletionHandler>().SingleInstance();

                containerBuilder.RegisterType<TwEngine>().As<ITwEngine>().SingleInstance();
            });


            builder.Services.ConfigureApplicationCookie(options =>
            {
                if (!string.IsNullOrEmpty(wikiConfiguration.BasePath))
                {
                    options.LoginPath = new PathString($"{wikiConfiguration.BasePath}/Identity/Account/Login");
                    options.LogoutPath = new PathString($"{wikiConfiguration.BasePath}/Identity/Account/Logout");
                    options.AccessDeniedPath = new PathString($"{wikiConfiguration.BasePath}/Identity/Account/AccessDenied");
                    options.Cookie.Path = wikiConfiguration.BasePath; // Ensure the cookie is scoped to the sub-site path.
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

            if (!string.IsNullOrEmpty(wikiConfiguration.BasePath))
            {
                app.UsePathBase(wikiConfiguration.BasePath);

                // Redirect root requests to basePath (something like '/TightWiki').
                app.Use(async (context, next) =>
                {
                    if (context.Request.Path == "/")
                    {
                        context.Response.Redirect(wikiConfiguration.BasePath);
                        return;
                    }
                    await next();
                });

                app.UseStaticFiles(new StaticFileOptions
                {
                    OnPrepareResponse = ctx =>
                    {
                        ctx.Context.Request.PathBase = wikiConfiguration.BasePath;
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

                if (dataStuff.WasDatabaseUpgraded)
                {
                    try
                    {
                        await DatabaseUpgrade.ApplyAllSeedData(logger, new VerbatimLocalizationText(), userManager, tightEngine,
                            [WikiDefaultDataType.Themes,
                            WikiDefaultDataType.Configurations,
                            WikiDefaultDataType.FeatureTemplates,
                            WikiDefaultDataType.WikiHelpPages]);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "An error occurred while applying seed data after database upgrade.");
                    }
                }

                try
                {
                    SecurityRepository.ValidateEncryptionAndCreateAdminUser(userManager);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while validating encryption or creating the admin user.");
                }
            }

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
    }
}
