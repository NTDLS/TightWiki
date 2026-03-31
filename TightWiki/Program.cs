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
using TightWiki.Engine;
using TightWiki.Library;
using TightWiki.Plugin;
using TightWiki.Plugin.Dummy;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Repository.Helpers;
using TightWiki.Translations;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            SqlMapper.AddTypeHandler(new TwGuidTypeHandler());

            var builder = WebApplication.CreateBuilder(args);

            var databaseManager = new DatabaseManager(builder.Configuration);
            bool wasDatabaseUpgraded = await databaseManager.ApplyDatabaseUpgradeScripts(databaseManager.Logger);

            //This is the minimum log level for the database logger, which is used for logging application events and errors to the database.
            var minimumLogLevel = Enum.Parse<LogLevel>(builder.Configuration.GetValue("EventLogLevel", LogLevel.Information.ToString()));

            builder.Logging.ClearProviders();
            builder.Logging.AddProvider(new DatabaseLoggerProvider(databaseManager.LoggingRepository, minimumLogLevel));

            var userConnectionString = databaseManager.UsersRepository.UsersFactory.Ephemeral(o => o.NativeConnection.ConnectionString);
            builder.Services.AddDbContext<TwApplicationDbContext>(options => options.UseSqlite(userConnectionString));

            var wikiConfigurationManager = new Repository.Helpers.ConfigurationManager(builder.Configuration, databaseManager);

            // Add DiffPlex services.
            builder.Services.AddScoped<IDiffer, Differ>();
            builder.Services.AddScoped<ISideBySideDiffBuilder>(sp =>
                new SideBySideDiffBuilder(sp.GetRequiredService<IDiffer>()));

            var membershipConfig = await databaseManager.ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Membership);
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

            var supportedCultures = new TwSupportedCultures();
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
            builder.Services.AddSingleton(wikiConfigurationManager.Configuration);
            builder.Services.AddSingleton<ITwEmailSender, TwEmailSender>();
            builder.Services.AddSingleton<ITwConfigurationRepository>(databaseManager.ConfigurationRepository);
            builder.Services.AddSingleton<ITwLoggingRepository>(databaseManager.LoggingRepository);
            builder.Services.AddSingleton<ITwEmojiRepository>(databaseManager.EmojiRepository);
            builder.Services.AddSingleton<ITwStatisticsRepository>(databaseManager.StatisticsRepository);
            builder.Services.AddSingleton<ITwPageRepository>(databaseManager.PageRepository);
            builder.Services.AddSingleton<ITwUsersRepository>(databaseManager.UsersRepository);
            builder.Services.AddSingleton<ITwDatabaseManager>(databaseManager);

            builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = requireConfirmedAccount)
                .AddEntityFrameworkStores<TwApplicationDbContext>();

            var externalAuthenticationConfig = await databaseManager.ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.ExternalAuthentication);
            var basicConfig = await databaseManager.ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Basic);
            var cookiesConfig = await databaseManager.ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Cookies);

            var authentication = builder.Services.AddAuthentication()
                .AddCookie("CookieAuth", options =>
                {
                    options.Cookie.Name = basicConfig.Value<string>("Name").EnsureNotNull();
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.LoginPath = $"{wikiConfigurationManager.Configuration.BasePath}/Identity/Account/Login";
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
                                context.Response.Redirect($"{wikiConfigurationManager.Configuration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString("External login was canceled.")}");
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
                                context.Response.Redirect($"{wikiConfigurationManager.Configuration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString("External login was canceled.")}");
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
                                context.Response.Redirect($"{wikiConfigurationManager.Configuration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString("OIDC login was canceled.")}");
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
                if (!string.IsNullOrEmpty(wikiConfigurationManager.Configuration.BasePath))
                {
                    options.LoginPath = new PathString($"{wikiConfigurationManager.Configuration.BasePath}/Identity/Account/Login");
                    options.LogoutPath = new PathString($"{wikiConfigurationManager.Configuration.BasePath}/Identity/Account/Logout");
                    options.AccessDeniedPath = new PathString($"{wikiConfigurationManager.Configuration.BasePath}/Identity/Account/AccessDenied");
                    options.Cookie.Path = wikiConfigurationManager.Configuration.BasePath; // Ensure the cookie is scoped to the sub-site path.
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

            if (!string.IsNullOrEmpty(wikiConfigurationManager.Configuration.BasePath))
            {
                app.UsePathBase(wikiConfigurationManager.Configuration.BasePath);

                // Redirect root requests to basePath (something like '/TightWiki').
                app.Use(async (context, next) =>
                {
                    if (context.Request.Path == "/")
                    {
                        context.Response.Redirect(wikiConfigurationManager.Configuration.BasePath);
                        return;
                    }
                    await next();
                });

                app.UseStaticFiles(new StaticFileOptions
                {
                    OnPrepareResponse = ctx =>
                    {
                        ctx.Context.Request.PathBase = wikiConfigurationManager.Configuration.BasePath;
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
                        await databaseManager.ApplyAllSeedData(new TwVerbatimLocalizationText(), userManager, tightEngine,
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
                    databaseManager.UsersRepository.ValidateEncryptionAndCreateAdminUser(userManager);
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
