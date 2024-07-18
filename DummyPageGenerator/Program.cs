using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TightWiki;
using TightWiki.Library;
using TightWiki.Repository;

namespace DummyPageGenerator
{
    internal class Program
    {
        static void Main(string[] args)
        {
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

            var users = UsersRepository.GetAllUsers();

            if (users.Count < 1124)
            {
                for (int i = 0; i < 1124 - users.Count; i++)
                {
                    string emailAddress = WordsRepository.GetRandomWords(1).First() + "@" + WordsRepository.GetRandomWords(1).First() + ".com";

                    #region Create User and Profile.

                    var user = new IdentityUser()
                    {
                        UserName = emailAddress,
                        Email = emailAddress
                    };

                    var result = userManager.CreateAsync(user, WordsRepository.GetRandomWords(1).First() + Guid.NewGuid().ToString()).Result;
                    if (!result.Succeeded)
                    {
                        throw new Exception(string.Join("\r\n", result.Errors.Select(o => o.Description)));
                    }

                    var userId = userManager.GetUserIdAsync(user).Result;
                    var membershipConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Membership");

                    UsersRepository.CreateProfile(Guid.Parse(userId));

                    var claimsToAdd = new List<Claim>
                    {
                        new (ClaimTypes.Role, membershipConfig.Value<string>("Default Signup Role")),
                        new ("timezone", membershipConfig.Value<string>("Default TimeZone")),
                        new (ClaimTypes.Country, membershipConfig.Value<string>("Default Country")),
                        new ("language", membershipConfig.Value<string>("Default Language")),
                    };

                    SecurityRepository.UpsertUserClaims(userManager, user, claimsToAdd);

                    #endregion
                }

                users = UsersRepository.GetAllUsers();
            }

            var rand = new Random();

            var namespaces = PageRepository.GetAllNamespaces();
            var tags = WordsRepository.GetRandomWords(250);
            var fileNames = WordsRepository.GetRandomWords(50);
            var recentPageNames = new List<string>();

            if (namespaces.Count < 250)
            {
                namespaces.AddRange(WordsRepository.GetRandomWords(250));
            }

            foreach (var user in users)
            {
                Console.WriteLine($"{user.AccountName} is making changes.");
                PageGenerator.GeneratePages(user.UserId, rand, namespaces, tags, fileNames, recentPageNames);
            }
        }
    }
}
