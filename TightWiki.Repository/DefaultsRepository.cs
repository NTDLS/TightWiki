using NTDLS.SqliteDapperWrapper;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;
using TightWiki.Plugin.Models.Defaults;

namespace TightWiki.Repository
{
    public partial class DefaultsRepository
        : ITwDefaultsRepository
    {
        public SqliteManagedFactory DefaultsFactory { get; private set; }

        public DefaultsRepository(string connectionString)
        {
            DefaultsFactory = new SqliteManagedFactory(connectionString);
        }

        public async Task<List<TwDefaultConfiguration>> GetDefaultConfigurationGroups()
            => await DefaultsFactory.QueryAsync<TwDefaultConfiguration>(@"Scripts\Defaults\GetDefaultConfigurationGroups.sql");

        public async Task<List<TwDefaultConfiguration>> GetDefaultConfigurations()
            => await DefaultsFactory.QueryAsync<TwDefaultConfiguration>(@"Scripts\Defaults\GetDefaultConfigurations.sql");

        public async Task<List<TwDefaultTheme>> GetDefaultThemes()
            => await DefaultsFactory.QueryAsync<TwDefaultTheme>(@"Scripts\Defaults\GetDefaultThemes.sql");

        public async Task<List<TwDefaultWikiPage>> GetDefaultWikiPages(string namespaceName)
            => await DefaultsFactory.QueryAsync<TwDefaultWikiPage>(@"Scripts\Defaults\GetDefaultWikiPages.sql",
                new { Namespace = namespaceName });

        public async Task<List<TwDefaultFeatureTemplate>> GetDefaultFeatureTemplates()
            => await DefaultsFactory.QueryAsync<TwDefaultFeatureTemplate>(@"Scripts\Defaults\GetDefaultFeatureTemplates.sql");

        /// <summary>
        /// Always returns an empty collection on SQLite: "Defaults\defaults.db" carries no page file attachment
        /// data - the SQLite install path gets page attachments (images, etc.) "for free" via a full copy of
        /// Data\pages.db rather than going through this seed mechanism. This method only exists to satisfy the
        /// shared ITwDefaultsRepository contract for the EF-based providers, which seed from
        /// Seed\tightwiki.seed.zip instead (Database-Providers-Plan.md chapter 4.6) - it must never be wired into
        /// DatabaseManager.ApplyAllSeedData for SQLite, as that would change today's (correct) no-op behavior.
        /// </summary>
        public Task<List<TwDefaultPageFileAttachment>> GetDefaultPageFileAttachments(string namespaceName)
            => Task.FromResult(new List<TwDefaultPageFileAttachment>());

        /// <summary>
        /// Always returns an empty collection on SQLite: "Defaults\defaults.db" carries no PageStatistics data -
        /// the SQLite install path gets page statistics "for free" via a full copy of Data\statistics.db rather
        /// than going through this seed mechanism. This method only exists to satisfy the shared
        /// ITwDefaultsRepository contract for the EF-based providers, which seed from Seed\tightwiki.seed.zip
        /// instead (Database-Providers-Plan.md chapter 4.6) - it must never be wired into
        /// DatabaseManager.ApplyAllSeedData for SQLite, as that would change today's (correct) no-op behavior.
        /// </summary>
        public Task<List<TwDefaultPageStatistic>> GetDefaultPageStatistics()
            => Task.FromResult(new List<TwDefaultPageStatistic>());

        /// <summary>
        /// Always returns an empty collection on SQLite: unlike the other Default* tables, "Defaults\defaults.db"
        /// carries no Emoji data - the SQLite install path seeds its Emoji database by copying the whole,
        /// pre-populated Data\emoji.db file rather than going through this seed mechanism (see
        /// DatabaseManager.CreateDefaultsDatabase / EmojiRepository). This method only exists to satisfy the
        /// shared ITwDefaultsRepository contract for the future EF-based providers, which will seed from
        /// Seed\tightwiki.seed.zip instead (Database-Providers-Plan.md chapter 4.6) - it must never be wired into
        /// DatabaseManager.ApplyAllSeedData for SQLite, as that would change today's (correct) no-op behavior.
        /// </summary>
        public Task<List<TwDefaultEmoji>> GetDefaultEmojis()
            => Task.FromResult(new List<TwDefaultEmoji>());

        /// <summary>
        /// Always returns an empty collection on SQLite - see <see cref="GetDefaultEmojis"/> for why.
        /// </summary>
        public Task<List<TwDefaultEmojiCategory>> GetDefaultEmojiCategories()
            => Task.FromResult(new List<TwDefaultEmojiCategory>());

        /// <summary>
        /// Always returns an empty collection on SQLite: "Defaults\defaults.db" carries no MenuItem data - the
        /// SQLite install path seeds Config.MenuItem by copying the whole, pre-populated Data\config.db file
        /// rather than going through this seed mechanism (see DatabaseManager.CreateDefaultsDatabase /
        /// ConfigurationRepository). This method only exists to satisfy the shared ITwDefaultsRepository contract
        /// for the future EF-based providers, which seed from Seed\tightwiki.seed.zip instead
        /// (Database-Providers-Plan.md chapter 4.6) - it must never be wired into
        /// DatabaseManager.ApplyAllSeedData for SQLite, as that would change today's (correct) no-op behavior.
        /// </summary>
        public Task<List<TwMenuItem>> GetDefaultMenuItems()
            => Task.FromResult(new List<TwMenuItem>());

        /// <summary>
        /// Always returns null on SQLite: "Defaults\defaults.db" carries no admin Profile data - the SQLite
        /// install path gets its admin Users.Profile row "for free" via a full copy of Data\users.db rather than
        /// going through this seed mechanism. This method only exists to satisfy the shared ITwDefaultsRepository
        /// contract for the EF-based providers, which seed from Seed\tightwiki.seed.zip instead
        /// (Database-Providers-Plan.md chapter 4.6) - it must never be wired into DatabaseManager.ApplyAllSeedData
        /// for SQLite, as that would change today's (correct) no-op behavior.
        /// </summary>
        public Task<TwDefaultProfile?> GetDefaultAdminProfile()
            => Task.FromResult<TwDefaultProfile?>(null);
    }
}
