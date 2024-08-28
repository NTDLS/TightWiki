using Autofac;
using Autofac.Extensions.DependencyInjection;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TightWiki.Email;
using TightWiki.Engine;
using TightWiki.Engine.Implementation;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Library;
using TightWiki.Library.Interfaces;
using TightWiki.Repository;

namespace TightWiki
{
    public class Program
    {
        public static void Main(string[] args)
        {
            SqlMapper.AddTypeHandler(new GuidTypeHandler());

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("UsersConnection")));

            ManagedDataStorage.Pages.SetConnectionString(builder.Configuration.GetConnectionString("PagesConnection"));
            ManagedDataStorage.DeletedPages.SetConnectionString(builder.Configuration.GetConnectionString("DeletedPagesConnection"));
            ManagedDataStorage.DeletedPageRevisions.SetConnectionString(builder.Configuration.GetConnectionString("DeletedPageRevisionsConnection"));
            ManagedDataStorage.Statistics.SetConnectionString(builder.Configuration.GetConnectionString("StatisticsConnection"));
            ManagedDataStorage.Emoji.SetConnectionString(builder.Configuration.GetConnectionString("EmojiConnection"));
            ManagedDataStorage.Exceptions.SetConnectionString(builder.Configuration.GetConnectionString("ExceptionsConnection"));
            ManagedDataStorage.Words.SetConnectionString(builder.Configuration.GetConnectionString("WordsConnection"));
            ManagedDataStorage.Users.SetConnectionString(builder.Configuration.GetConnectionString("UsersConnection"));
            ManagedDataStorage.Config.SetConnectionString(builder.Configuration.GetConnectionString("ConfigConnection"));

            ConfigurationRepository.ReloadEverything();

            var membershipConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Membership");
            var requireConfirmedAccount = membershipConfig.Value<bool>("Require Email Verification");

            // Add services to the container.
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddControllersWithViews(); // Adds support for controllers and views

            builder.Services.AddSingleton<IWikiEmailSender, WikiEmailSender>();

            builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = requireConfirmedAccount)
                .AddEntityFrameworkStores<ApplicationDbContext>();

            var ExternalAuthenticationConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("External Authentication");

            var authentication = builder.Services.AddAuthentication();

            if (ExternalAuthenticationConfig.Value<bool>("Google : Use Google Authentication"))
            {
                var clientId = ExternalAuthenticationConfig.Value<string>("Google : ClientId");
                var clientSecret = ExternalAuthenticationConfig.Value<string>("Google : ClientSecret");

                if (clientId != null && clientSecret != null && !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
                {
                    authentication.AddGoogle(options =>
                    {
                        options.ClientId = clientId;
                        options.ClientSecret = clientSecret;
                    });
                }
            }
            if (ExternalAuthenticationConfig.Value<bool>("Microsoft : Use Microsoft Authentication"))
            {
                var clientId = ExternalAuthenticationConfig.Value<string>("Microsoft : ClientId");
                var clientSecret = ExternalAuthenticationConfig.Value<string>("Microsoft : ClientSecret");

                if (clientId != null && clientSecret != null && !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
                {
                    authentication.AddMicrosoftAccount(options =>
                    {
                        options.ClientId = clientId;
                        options.ClientSecret = clientSecret;
                    });
                }
            }

            builder.Services.AddControllersWithViews();

            builder.Services.AddRazorPages();

            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
            builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
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
                containerBuilder.RegisterType<CompletionHandler>().As<ICompletionHandler>().SingleInstance();

                containerBuilder.RegisterType<TightEngine>().As<ITightEngine>().SingleInstance();
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
                try
                {
                    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
                    SecurityRepository.ValidateEncryptionAndCreateAdminUser(userManager);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }
            }

            app.Run();
        }
    }
}
