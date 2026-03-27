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
using TightWiki.Email;
using TightWiki.Engine;
using TightWiki.Engine.Implementation;
using TightWiki.Engine.Implementation.Handlers;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Library;
using TightWiki.Library.Interfaces;
using TightWiki.Models;
using TightWiki.Repository;
using TightWiki.Repository.Extensions;
using TightWiki.Translations;

namespace TightWiki
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            SqlMapper.AddTypeHandler(new GuidTypeHandler());

            var builder = WebApplication.CreateBuilder(args);

            //This is the minimum log level for the database logger, which is used for logging application events and errors to the database.
            var minimumLogLevel = Enum.Parse<LogLevel>(builder.Configuration.GetValue("EventLogLevel", LogLevel.Information.ToString()));

            ManagedDataStorage.Config.SetConnectionString(builder.Configuration.GetDatabaseConnectionString("ConfigConnection", "config.db"));
            ManagedDataStorage.Logging.SetConnectionString(builder.Configuration.GetDatabaseConnectionString("LoggingConnection", "logging.db"));

            builder.Logging.ClearProviders();
            builder.Logging.AddProvider(new DatabaseLoggerProvider(minimumLogLevel));

            ManagedDataStorage.Pages.SetConnectionString(builder.Configuration.GetDatabaseConnectionString("PagesConnection", "pages.db"));
            ManagedDataStorage.DeletedPages.SetConnectionString(builder.Configuration.GetDatabaseConnectionString("DeletedPagesConnection", "deletedpages.db"));
            ManagedDataStorage.DeletedPageRevisions.SetConnectionString(builder.Configuration.GetDatabaseConnectionString("DeletedPageRevisionsConnection", "deletedpagerevisions.db"));
            ManagedDataStorage.Statistics.SetConnectionString(builder.Configuration.GetDatabaseConnectionString("StatisticsConnection", "statistics.db"));
            ManagedDataStorage.Emoji.SetConnectionString(builder.Configuration.GetDatabaseConnectionString("EmojiConnection", "emoji.db"));
            ManagedDataStorage.Users.SetConnectionString(builder.Configuration.GetDatabaseConnectionString("UsersConnection", "users.db"));

            var independentLogger = new DatabaseLogger("", LogLevel.Information);

            var userConnectionString = ManagedDataStorage.Users.Ephemeral(o => o.NativeConnection.ConnectionString);
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(userConnectionString));

            await LoggingRepository.CreateTablesIfNotExist();

            //Upgrade database if needed and create defaults database if needed. This is done at the very beginning before
            //  almost anything else to ensure the database is in the correct state for the rest of the application to work with.
            //
            // Default data is seeded further down after the injection of ILogger, UserManager, and ITightEngine,
            //  via a call to DatabaseUpgrade.ApplyAllSeedData()
            var wasDatabaseUpgraded = await DatabaseUpgrade.ApplyDatabaseUpgradeScripts(independentLogger);

            var defaultsDatabasePath = await DatabaseUpgrade.CreateDefaultsDatabase(independentLogger, builder.Configuration, wasDatabaseUpgraded);
            if (defaultsDatabasePath != null)
            {
                ManagedDataStorage.Defaults.SetConnectionString(defaultsDatabasePath);
            }

            await ConfigurationRepository.ReloadEverything();

            // Add DiffPlex services.
            builder.Services.AddScoped<IDiffer, Differ>();
            builder.Services.AddScoped<ISideBySideDiffBuilder>(sp =>
                new SideBySideDiffBuilder(sp.GetRequiredService<IDiffer>()));

            var membershipConfig = await ConfigurationRepository.GetConfigurationEntryValuesByGroupName(Constants.WikiConfigurationGroup.Membership);
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

            builder.Services.AddSingleton<IWikiEmailSender, WikiEmailSender>();

            builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = requireConfirmedAccount)
                .AddEntityFrameworkStores<ApplicationDbContext>();

            var externalAuthenticationConfig = await ConfigurationRepository.GetConfigurationEntryValuesByGroupName(Constants.WikiConfigurationGroup.ExternalAuthentication);
            var basicConfig = await ConfigurationRepository.GetConfigurationEntryValuesByGroupName(Constants.WikiConfigurationGroup.Basic);
            var cookiesConfig = await ConfigurationRepository.GetConfigurationEntryValuesByGroupName(Constants.WikiConfigurationGroup.Cookies);

            var authentication = builder.Services.AddAuthentication()
                .AddCookie("CookieAuth", options =>
                {
                    options.Cookie.Name = basicConfig.Value<string>("Name").EnsureNotNull();
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.LoginPath = $"{GlobalConfiguration.BasePath}/Identity/Account/Login";
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
                                context.Response.Redirect($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString("External login was canceled.")}");
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
                                context.Response.Redirect($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString("External login was canceled.")}");
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
                                context.Response.Redirect($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString("OIDC login was canceled.")}");
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

                containerBuilder.RegisterType<TightEngine>().As<ITightEngine>().SingleInstance();
            });

            var basePath = builder.Configuration.GetValue<string>("BasePath");
            if (!string.IsNullOrEmpty(basePath))
            {
                GlobalConfiguration.BasePath = basePath;

                builder.Services.ConfigureApplicationCookie(options =>
                {
                    if (!string.IsNullOrEmpty(basePath))
                    {
                        options.LoginPath = new PathString($"{basePath}/Identity/Account/Login");
                        options.LogoutPath = new PathString($"{basePath}/Identity/Account/Logout");
                        options.AccessDeniedPath = new PathString($"{basePath}/Identity/Account/AccessDenied");
                        options.Cookie.Path = basePath; // Ensure the cookie is scoped to the sub-site path.
                    }
                    else
                    {
                        options.LoginPath = new PathString("/Identity/Account/Login");
                        options.LogoutPath = new PathString("/Identity/Account/Logout");
                        options.AccessDeniedPath = new PathString("/Identity/Account/AccessDenied");
                        options.Cookie.Path = "/"; // Use root path if no base path is set.
                    }
                });
            }

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

            if (!string.IsNullOrEmpty(basePath))
            {
                app.UsePathBase(basePath);

                // Redirect root requests to basePath (something like '/TightWiki').
                app.Use(async (context, next) =>
                {
                    if (context.Request.Path == "/")
                    {
                        context.Response.Redirect(basePath);
                        return;
                    }
                    await next();
                });

                app.UseStaticFiles(new StaticFileOptions
                {
                    OnPrepareResponse = ctx =>
                    {
                        ctx.Context.Request.PathBase = basePath;
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
                var tightEngine = services.GetRequiredService<ITightEngine>();

                if (wasDatabaseUpgraded)
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
