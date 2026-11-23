using Microsoft.Extensions.Logging;
using TightWiki.Library;
using TightWiki.Library.Caching;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.Repository.Helpers;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Tests.Unit
{
    [Collection("Markup Tests")]
    public class SignupNotificationServiceTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        private const string NotifyEntry = "Notify Administrators of New Signup";

        private class FakeEmailSender : ITwEmailSender
        {
            public List<(string To, string Subject, string Body)> Sent { get; } = new();
            public bool ThrowOnSend { get; set; }

            public Task SendEmailAsync(string email, string subject, string htmlMessage)
            {
                if (ThrowOnSend)
                {
                    throw new Exception("The mail server is down.");
                }
                Sent.Add((email, subject, htmlMessage));
                return Task.CompletedTask;
            }
        }

        private class FakeLogger : ILogger<ITwEngine>
        {
            public List<LogLevel> Levels { get; } = new();

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                => Levels.Add(logLevel);
        }

        private readonly FakeEmailSender _emailSender = new();
        private readonly FakeLogger _logger = new();

        private SignupNotificationService CreateService()
            => new(fixture.Artifacts.DatabaseManager.ConfigurationRepository, fixture.Artifacts.DatabaseManager.UsersRepository, _emailSender, _logger);

        /// <summary>
        /// The test databases are copies of the committed ones, which do not have the entries that the upgrade scripts add.
        /// </summary>
        private async Task SetNotificationEnabled(bool enabled)
        {
            var configurationRepository = fixture.Artifacts.DatabaseManager.ConfigurationRepository;
            var assembly = typeof(DatabaseManager).Assembly;

            //Only the scripts of this feature are run, other scripts in the same version folder are none of our business.
            string[] scriptFileNames =
            [
                "^000^Config^NotifyAdministratorsOfSignupConfigurationEntry.sql",
                "^001^Config^NewSignupNotificationEmailTemplateConfigurationEntry.sql"
            ];

            //The resource names have an underscore in front of each number in the folder name.
            var allResourceNames = assembly.GetManifestResourceNames();

            foreach (var scriptFileName in scriptFileNames)
            {
                var matchingNames = allResourceNames.Where(o => o.Replace("_", "").EndsWith($"Versions.2.43.1.{scriptFileName}")).ToList();
                Assert.Single(matchingNames);

                var resourceName = matchingNames[0];
                using var reader = new StreamReader(assembly.GetManifestResourceStream(resourceName)!);
                await configurationRepository.ConfigFactory.ExecuteAsync(reader.ReadToEnd());
            }

            await configurationRepository.SaveConfigurationEntryValueByGroupAndEntry(TwConfigGroup.Membership, NotifyEntry, enabled ? "1" : "0");
            MemCache.ClearCategory(MemCache.Category.Configuration);
        }

        private async Task<Guid> CreateSignedUpAccount(string accountName)
        {
            var emailAddress = $"{Guid.NewGuid():N}@example.test";
            await fixture.CreateUserAndProfile(emailAddress, accountName, $"{accountName}sP@ssW0rD!]");
            var profile = await fixture.Artifacts.DatabaseManager.UsersRepository.GetAccountProfileByNavigation(TwNavigation.Clean(accountName));
            return profile!.UserId;
        }

        private async Task<List<string>> GetAdministratorEmailAddresses()
        {
            var usersRepository = fixture.Artifacts.DatabaseManager.UsersRepository;
            var administratorsRole = await usersRepository.GetRoleByName("Administrator");
            var emailAddresses = new List<string>();

            foreach (var user in await usersRepository.GetAllUsers())
            {
                if (await usersRepository.IsAccountAMemberOfRole(user.UserId, administratorsRole.Id))
                {
                    emailAddresses.Add(user.EmailAddress);
                }
            }
            return emailAddresses;
        }

        [Fact]
        public async Task EveryAdministratorAndNobodyElseIsNotified()
        {
            await SetNotificationEnabled(true);

            var secondAdministratorId = await CreateSignedUpAccount("Second Administrator " + Guid.NewGuid().ToString("N"));
            await fixture.Artifacts.DatabaseManager.UsersRepository.AddRoleMemberByname(secondAdministratorId, "Administrator");

            var newAccountName = "New Signup " + Guid.NewGuid().ToString("N");
            var newUserId = await CreateSignedUpAccount(newAccountName);
            var newProfile = await fixture.Artifacts.DatabaseManager.UsersRepository.GetAccountProfileByUserId(newUserId);

            await CreateService().NotifyAdministrators(newUserId);

            var expectedRecipients = await GetAdministratorEmailAddresses();
            Assert.True(expectedRecipients.Count >= 2);
            Assert.Equal(expectedRecipients.Order(), _emailSender.Sent.Select(o => o.To).Order());
            Assert.DoesNotContain(newProfile.EmailAddress, _emailSender.Sent.Select(o => o.To));

            var siteAddress = (await fixture.Artifacts.DatabaseManager.ConfigurationRepository.Get<string>(TwConfigGroup.Basic, "Address"))!.TrimEnd('/');
            foreach (var email in _emailSender.Sent)
            {
                Assert.Contains(newAccountName, email.Body);
                Assert.Contains(newProfile.EmailAddress, email.Body);
                Assert.Contains($"{siteAddress}/AdminSecurity/Account/{newProfile.Navigation}", email.Body);
                Assert.DoesNotContain("##", email.Body);
            }
        }

        [Fact]
        public async Task NothingIsSentWhenTheNotificationIsDisabled()
        {
            await SetNotificationEnabled(false);
            var newUserId = await CreateSignedUpAccount("Quiet Signup " + Guid.NewGuid().ToString("N"));

            await CreateService().NotifyAdministrators(newUserId);

            Assert.Empty(_emailSender.Sent);
        }

        [Fact]
        public async Task AFailureToSendIsLoggedAndNotThrown()
        {
            await SetNotificationEnabled(true);
            var newUserId = await CreateSignedUpAccount("Unlucky Signup " + Guid.NewGuid().ToString("N"));
            _emailSender.ThrowOnSend = true;

            await CreateService().NotifyAdministrators(newUserId);

            Assert.Contains(LogLevel.Error, _logger.Levels);
        }
    }
}
