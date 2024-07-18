using Dapper;
using Microsoft.Extensions.Configuration;
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

            IConfiguration configuration = builder.Build();
            ManagedDataStorage.Pages.SetConnectionString(configuration.GetConnectionString("PagesConnection"));
            ManagedDataStorage.DeletedPages.SetConnectionString(configuration.GetConnectionString("DeletedPagesConnection"));
            ManagedDataStorage.DeletedPageRevisions.SetConnectionString(configuration.GetConnectionString("DeletedPageRevisionsConnection"));
            ManagedDataStorage.Statistics.SetConnectionString(configuration.GetConnectionString("StatisticsConnection"));
            ManagedDataStorage.Emoji.SetConnectionString(configuration.GetConnectionString("EmojiConnection"));
            ManagedDataStorage.Exceptions.SetConnectionString(configuration.GetConnectionString("ExceptionsConnection"));
            ManagedDataStorage.Words.SetConnectionString(configuration.GetConnectionString("WordsConnection"));
            ManagedDataStorage.Users.SetConnectionString(configuration.GetConnectionString("UsersConnection"));
            ManagedDataStorage.Config.SetConnectionString(configuration.GetConnectionString("ConfigConnection"));

            ConfigurationRepository.ReloadEverything();

            var users = UsersRepository.GetAllProfiles();

            if (users.Count < 100)
            {
                for (int i = 0; i < 100; i++)
                {
                    var userId = Guid.NewGuid();
                    var userAccountName = string.Join(' ', WordsRepository.GetRandomWords(2));

                    UsersRepository.CreateProfile(userId, userAccountName);
                }

                users = UsersRepository.GetAllProfiles();
            }


            foreach (var user in users)
            {
                new Thread(() => PageGenerator.GeneratePages(user.UserId)).Start();
            }
        }
    }
}
