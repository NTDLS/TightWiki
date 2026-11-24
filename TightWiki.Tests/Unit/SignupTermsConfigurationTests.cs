using TightWiki.Library.Caching;
using TightWiki.Repository.Helpers;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Tests.Unit
{
    [Collection("Markup Tests")]
    public class SignupTermsConfigurationTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        private const string SignupTermsEntry = "Signup Terms";
        private const string ScriptFileName = "^002^Config^SignupTermsConfigurationEntry.sql";

        /// <summary>
        /// The test databases are copies of the committed ones, which do not have the entries that the upgrade scripts add.
        /// </summary>
        private async Task RunUpgradeScript()
        {
            var assembly = typeof(DatabaseManager).Assembly;

            //The resource names have an underscore in front of each number in the folder name.
            var matchingNames = assembly.GetManifestResourceNames()
                .Where(o => o.Replace("_", "").EndsWith($"Versions.2.43.1.{ScriptFileName}")).ToList();
            Assert.Single(matchingNames);

            using var reader = new StreamReader(assembly.GetManifestResourceStream(matchingNames[0])!);
            await fixture.Artifacts.DatabaseManager.ConfigurationRepository.ConfigFactory.ExecuteAsync(reader.ReadToEnd());
            MemCache.ClearCategory(MemCache.Category.Configuration);
        }

        [Fact]
        public async Task TheUpgradeScriptAddsOneEmptyEntryAndCanBeRunAgain()
        {
            var configurationRepository = fixture.Artifacts.DatabaseManager.ConfigurationRepository;

            await RunUpgradeScript();
            await RunUpgradeScript();

            var entries = (await configurationRepository.GetFlatConfiguration())
                .Where(o => o.GroupName == TwConfigGroup.Membership && o.EntryName == SignupTermsEntry).ToList();
            Assert.Single(entries);
            Assert.Equal(string.Empty, entries[0].EntryValue);
            Assert.Equal("Text", entries[0].DataType);
        }
    }
}
