using System.Reflection;

namespace TightWiki.Tests.Unit
{
    /// <summary>
    /// Scenario-level integration tests for <see cref="TightWiki.Plugin.Interfaces.Repository.ITwDefaultsRepository"/>
    /// (9 methods - read-only access to the static "canned" seed data used to bootstrap a wiki, as opposed to the
    /// live/mutable data <see cref="ConfigurationRepositoryTests"/>/<see cref="LoggingRepositoryTests"/>/
    /// <see cref="StatisticsRepositoryTests"/>/<see cref="EmojiRepositoryTests"/> exercise), obtained through
    /// <see cref="TwEngineFixture"/> exactly like those four. Written entirely against the provider-agnostic
    /// interface - no <c>#if SQLITE_PROVIDER</c>/<c>#elif SQLSERVER_PROVIDER</c>/<c>#elif POSTGRES_PROVIDER</c>
    /// branching here. The same compiled test code runs three times: <c>dotnet test</c> (SQLite, default),
    /// <c>dotnet test -p:DataProvider=SqlServer</c>, <c>dotnet test -p:DataProvider=Postgres</c>
    /// (Database-Providers-Testing-Plan.md chapter 5.5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unlike the four sibling repository test classes above, this repository is not CRUD over a live, shared,
    /// concurrently-written database - it is a pure reader of a static package: <c>TightWiki.Repository.DefaultsRepository</c>
    /// (the SQLite reference) queries the embedded, per-process-extracted <c>Defaults\defaults.db</c> template via
    /// SQL scripts under <c>Scripts\Defaults\</c>, while <c>TightWiki.Data.EfCore.Seeding.EfDefaultsRepository</c>
    /// (used by the SqlServer/Postgres providers) reads JSON manifests + emoji image entries out of
    /// <c>Seed\tightwiki.seed.zip</c> (produced by <c>GenerateSeedData</c>'s <c>SeedPackageGenerator</c>). Neither
    /// implementation ever mutates anything it reads, so every test below is purely read-only, safe to run any
    /// number of times, concurrently with every other xunit collection in this assembly, with no <c>finally</c>
    /// cleanup required anywhere in this class.
    /// </para>
    /// <para>
    /// <b>The documented SQLite/EF asymmetry, confirmed by direct inspection (not taken on faith from this task's
    /// brief) - and narrower than that brief's own description ("SQLite returns empty for most methods"):</b>
    /// exactly 4 of this interface's 9 methods - <see cref="TightWiki.Plugin.Interfaces.Repository.ITwDefaultsRepository.GetDefaultPageFileAttachments"/>,
    /// <see cref="TightWiki.Plugin.Interfaces.Repository.ITwDefaultsRepository.GetDefaultEmojis"/>,
    /// <see cref="TightWiki.Plugin.Interfaces.Repository.ITwDefaultsRepository.GetDefaultEmojiCategories"/> and
    /// <see cref="TightWiki.Plugin.Interfaces.Repository.ITwDefaultsRepository.GetDefaultMenuItems"/> - are
    /// hardcoded in the SQLite reference (<c>DefaultsRepository.cs</c>) to always return an empty collection,
    /// exactly as each of those four methods' own doc comments on the interface describe (SQLite gets page
    /// attachments/emoji/menu items "for free" via a full copy of <c>Data\pages.db</c>/<c>emoji.db</c>/
    /// <c>config.db</c> instead of through this mechanism). The other 5 methods -
    /// <see cref="TightWiki.Plugin.Interfaces.Repository.ITwDefaultsRepository.GetDefaultConfigurationGroups"/>,
    /// <see cref="TightWiki.Plugin.Interfaces.Repository.ITwDefaultsRepository.GetDefaultConfigurations"/>,
    /// <see cref="TightWiki.Plugin.Interfaces.Repository.ITwDefaultsRepository.GetDefaultThemes"/>,
    /// <see cref="TightWiki.Plugin.Interfaces.Repository.ITwDefaultsRepository.GetDefaultWikiPages"/> and
    /// <see cref="TightWiki.Plugin.Interfaces.Repository.ITwDefaultsRepository.GetDefaultFeatureTemplates"/> - all
    /// execute real SQL against <c>Defaults\defaults.db</c> on SQLite (confirmed by reading every
    /// <c>Scripts\Defaults\*.sql</c> file this class's methods use) and return genuine, non-empty seed data on
    /// SQLite exactly like they do on the EF-backed providers; nothing here is SQLite-specific-empty for those 5.
    /// The 4 tests covering those 5 methods therefore assert plain non-empty/well-formed results unconditionally,
    /// with no provider-dependent branching needed at all. Only the single test covering the remaining 4 methods
    /// (<see cref="GetDefaultPageFileAttachments_GetDefaultEmojis_GetDefaultEmojiCategories_GetDefaultMenuItems_RespectDocumentedSqliteEmptyBehavior"/>)
    /// branches at runtime on what the repository actually returned - never on a compile-time provider flag.
    /// </para>
    /// <para>
    /// <see cref="TightWiki.Data.EfCore.Seeding.EfDefaultsRepository.ReadEmojiImageBytes"/> - needed to verify the
    /// image-bytes half of <see cref="TightWiki.Plugin.Interfaces.Repository.ITwDefaultsRepository.GetDefaultEmojis"/>'s
    /// metadata-only <see cref="TightWiki.Plugin.Models.Defaults.TwDefaultEmoji.ImageEntry"/> round-trips to a real
    /// image - is deliberately <i>not</i> part of <see cref="TightWiki.Plugin.Interfaces.Repository.ITwDefaultsRepository"/>
    /// (see that method's own doc comment: keeping the shared metadata-only contract cheap for callers who don't
    /// need ~18 MB of images). It is invoked below via reflection (<see cref="MethodInfo.Invoke(object?, object?[]?)"/>)
    /// rather than a direct call, precisely so this file can stay <c>#if</c>-free and compile under every
    /// <c>DataProvider</c>: a direct, compile-time reference to <c>TightWiki.Data.EfCore.Seeding.EfDefaultsRepository</c>
    /// would not compile under the default SQLite build, which never references the <c>TightWiki.Data.EfCore</c>
    /// project at all (see <c>TightWiki.Tests.csproj</c>'s <c>DataProvider</c>-conditioned <c>ProjectReference</c>
    /// items). On SQLite this method is skipped entirely - <see cref="TightWiki.Plugin.Interfaces.Repository.ITwDefaultsRepository.GetDefaultEmojis"/>
    /// already returns empty there, so there is no <see cref="TightWiki.Plugin.Models.Defaults.TwDefaultEmoji.ImageEntry"/>
    /// to read bytes for in the first place.
    /// </para>
    /// <para>
    /// One more thing confirmed by inspection while writing this class, worth flagging since it contradicts a
    /// natural assumption (and this task's own brief, which asked to verify a "GZip round-trip"): unlike the
    /// <i>live</i> Emoji table, whose <c>ImageData</c> is stored GZip-compressed on every provider (see
    /// <see cref="EmojiRepositoryTests"/>'s own remarks, which decompress via <c>Utility.Decompress</c> before
    /// comparing), the seed package's <c>Emoji/Images/*</c> zip entries hold the emoji image bytes <i>already
    /// decompressed</i> - <c>SeedPackageGenerator</c>'s own doc comment on its <c>DecompressIfGZip</c> helper says
    /// so explicitly ("so the Emoji/Images/* zip entry holds the actual image bytes instead of the compressed
    /// container"), and <c>EfDefaultsRepository.ReadEmojiImageBytes</c> just returns the entry's raw stream bytes
    /// with no decompression step. There is therefore no GZip round-trip to perform here at all; the test below
    /// instead asserts the returned bytes start with a real PNG file signature - which on its own also proves they
    /// are not still GZip-wrapped, since a GZip container's own magic number (0x1F 0x8B) is incompatible with a PNG
    /// signature (0x89 0x50 0x4E 0x47 ...) by construction.
    /// </para>
    /// </remarks>
    [Collection("Defaults Repository Tests")]
    public class DefaultsRepositoryTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        [Fact]
        public async Task GetDefaultConfigurationGroups_And_GetDefaultConfigurations_ReturnConsistentNonEmptySeedData()
        {
            var repo = fixture.Artifacts.DatabaseManager.DefaultsRepository;

            var groups = await repo.GetDefaultConfigurationGroups();
            var entries = await repo.GetDefaultConfigurations();

            Assert.NotEmpty(groups);
            Assert.NotEmpty(entries);

            //GetDefaultConfigurationGroups.sql's own "SELECT DISTINCT ConfigurationGroupName, ..." (SQLite) and
            //EfDefaultsRepository.GetDefaultConfigurationGroups' direct dump of config.db's ConfigurationGroup
            //table (EF) both promise one row per group name - no duplicates on either provider.
            var groupNames = groups.Select(g => g.ConfigurationGroupName).ToList();
            Assert.Equal(groupNames.Distinct().Count(), groupNames.Count);
            Assert.All(groups, g => Assert.False(string.IsNullOrWhiteSpace(g.ConfigurationGroupName)));

            //Every entry's own group name must resolve to one of the groups just fetched - this is the same
            //relationship EfDefaultsRepository.GetDefaultConfigurations has to reconstruct in memory via a
            //dictionary join (see that method's own doc comment: unlike the flattened SQLite DefaultConfiguration
            //table, ConfigurationGroup.json/ConfigurationEntry.json are normalized), so this also exercises that
            //the join actually landed on the right group for every entry, not just that groups/entries are each
            //independently non-empty.
            var groupNameSet = groupNames.ToHashSet();
            Assert.All(entries, e =>
            {
                Assert.False(string.IsNullOrWhiteSpace(e.ConfigurationEntryName));
                Assert.Contains(e.ConfigurationGroupName, groupNameSet);
            });
        }

        [Fact]
        public async Task GetDefaultThemes_ReturnsThemeMatchingTheActiveSeededTheme()
        {
            var repo = fixture.Artifacts.DatabaseManager.DefaultsRepository;

            var themes = await repo.GetDefaultThemes();
            Assert.NotEmpty(themes);

            //Cross-checked against the same theme WikiConfigurationManager already resolved at fixture startup
            //(same idiom as ConfigurationRepositoryTests.GetAllThemes_ReturnsTheActiveSeededThemeWithParsedFiles)
            //rather than hardcoding a theme name - the live Config.Theme row for this theme was itself seeded from
            //this exact default theme set (SQLite's MergeTheme.sql / EfConfigurationRepository's equivalent), so a
            //name match here is guaranteed regardless of which theme the seed data configures as active.
            var activeThemeName = fixture.Artifacts.WikiConfigurationManager.WikiConfiguration.SystemTheme.Name;
            var activeTheme = Assert.Single(themes, t => t.Name == activeThemeName);

            Assert.False(string.IsNullOrWhiteSpace(activeTheme.DelimitedFiles));
        }

        [Fact]
        public async Task GetDefaultWikiPages_KnownNamespace_ReturnsPages_AndUnknownNamespace_ReturnsEmptyNotNull()
        {
            var repo = fixture.Artifacts.DatabaseManager.DefaultsRepository;

            //"Wiki Help" is a real namespace present in both the SQLite defaults.db template and the seed zip's
            //DefaultWikiPages/ manifests (confirmed by direct inspection of Seed\tightwiki.seed.zip's entry list),
            //and is the one DatabaseManager.ApplyAllSeedData/PostgresDatabaseManager.SeedWikiPages actually seed
            //real installations from for TwDefaultDataType.HelpPages on every provider.
            const string knownNamespace = "Wiki Help";

            var helpPages = await repo.GetDefaultWikiPages(knownNamespace);
            Assert.NotEmpty(helpPages);
            Assert.All(helpPages, p =>
            {
                Assert.Equal(knownNamespace, p.Namespace);
                Assert.False(string.IsNullOrWhiteSpace(p.Name));
                Assert.False(string.IsNullOrWhiteSpace(p.Navigation));
                Assert.False(string.IsNullOrWhiteSpace(p.Body));
            });

            //A namespace with no matching rows must come back as an empty list, not null and not a thrown
            //exception - GetDefaultWikiPages.sql's plain "WHERE Namespace = @Namespace" (SQLite) and
            //EfDefaultsRepository.GetDefaultWikiPages's explicit "entry == null -> return []" branch (EF) both
            //promise exactly this (see EfDefaultsRepository's own doc comment on that method).
            var unknownNamespace = $"TestDefaults_NoSuchNamespace_{Guid.NewGuid():N}";
            var unknownPages = await repo.GetDefaultWikiPages(unknownNamespace);
            Assert.NotNull(unknownPages);
            Assert.Empty(unknownPages);
        }

        [Fact]
        public async Task GetDefaultFeatureTemplates_ReturnsNonEmptyTemplatesWithNameAndType()
        {
            var repo = fixture.Artifacts.DatabaseManager.DefaultsRepository;

            var templates = await repo.GetDefaultFeatureTemplates();
            Assert.NotEmpty(templates);
            Assert.All(templates, t =>
            {
                Assert.False(string.IsNullOrWhiteSpace(t.Name));
                Assert.False(string.IsNullOrWhiteSpace(t.Type));
                Assert.False(string.IsNullOrWhiteSpace(t.TemplateText));
            });
        }

        /// <summary>
        /// Covers the 4 methods that this class's own remarks confirm are hardcoded to return an empty collection
        /// on the SQLite reference - <see cref="TightWiki.Plugin.Interfaces.Repository.ITwDefaultsRepository.GetDefaultPageFileAttachments"/>,
        /// <see cref="TightWiki.Plugin.Interfaces.Repository.ITwDefaultsRepository.GetDefaultEmojis"/>,
        /// <see cref="TightWiki.Plugin.Interfaces.Repository.ITwDefaultsRepository.GetDefaultEmojiCategories"/> and
        /// <see cref="TightWiki.Plugin.Interfaces.Repository.ITwDefaultsRepository.GetDefaultMenuItems"/> - by
        /// branching once, at runtime, on what was actually returned (never on a compile-time provider flag): if
        /// <see cref="TightWiki.Plugin.Interfaces.Repository.ITwDefaultsRepository.GetDefaultEmojis"/> came back
        /// empty (SQLite), all four are asserted empty, exactly mirroring <c>DefaultsRepository.cs</c>'s documented
        /// behavior; otherwise (the EF-backed providers, reading real data out of <c>Seed\tightwiki.seed.zip</c>)
        /// all four are asserted non-empty and well-formed, including an emoji-category -> emoji foreign-key style
        /// cross-check and an image-bytes round-trip for one sample emoji (see this class's own remarks for why
        /// that last part uses reflection and does not expect the bytes to still be GZip-compressed).
        /// </summary>
        [Fact]
        public async Task GetDefaultPageFileAttachments_GetDefaultEmojis_GetDefaultEmojiCategories_GetDefaultMenuItems_RespectDocumentedSqliteEmptyBehavior()
        {
            var repo = fixture.Artifacts.DatabaseManager.DefaultsRepository;

            //Same namespace GetDefaultWikiPages_KnownNamespace_ReturnsPages_AndUnknownNamespace_ReturnsEmptyNotNull
            //above already confirmed has real default wiki pages - and, per Seed\tightwiki.seed.zip's own entry
            //list, also has a "DefaultPageFileAttachments/Wiki Help.json" manifest.
            var attachments = await repo.GetDefaultPageFileAttachments("Wiki Help");
            var emojis = await repo.GetDefaultEmojis();
            var emojiCategories = await repo.GetDefaultEmojiCategories();
            var menuItems = await repo.GetDefaultMenuItems();

            if (emojis.Count == 0)
            {
                //SQLite reference: DefaultsRepository.GetDefaultPageFileAttachments/GetDefaultEmojiCategories/
                //GetDefaultMenuItems are every one of them hardcoded Task.FromResult(new List<...>()) - this is
                //the documented, intentional behavior this class's own remarks describe, not a bug to work around.
                Assert.Empty(attachments);
                Assert.Empty(emojiCategories);
                Assert.Empty(menuItems);
                return;
            }

            //EF-backed provider (SqlServer/Postgres): EfDefaultsRepository reads real data out of
            //Seed\tightwiki.seed.zip for all four methods.
            Assert.NotEmpty(attachments);
            Assert.All(attachments, a =>
            {
                Assert.False(string.IsNullOrWhiteSpace(a.PageName));
                Assert.False(string.IsNullOrWhiteSpace(a.FileName));
                Assert.False(string.IsNullOrWhiteSpace(a.ContentType));
                Assert.NotEmpty(a.Data);
            });

            Assert.All(emojis, e =>
            {
                Assert.True(e.Id > 0);
                Assert.False(string.IsNullOrWhiteSpace(e.Name));
                Assert.False(string.IsNullOrWhiteSpace(e.MimeType));
                Assert.False(string.IsNullOrWhiteSpace(e.ImageEntry));
            });

            Assert.NotEmpty(emojiCategories);
            var emojiIds = emojis.Select(e => e.Id).ToHashSet();
            Assert.All(emojiCategories, c => Assert.Contains(c.EmojiId, emojiIds));

            Assert.NotEmpty(menuItems);
            Assert.All(menuItems, m =>
            {
                Assert.False(string.IsNullOrWhiteSpace(m.Name));
                Assert.False(string.IsNullOrWhiteSpace(m.Link));
            });

            //Image-bytes round-trip via ReadEmojiImageBytes - not part of ITwDefaultsRepository (see this class's
            //own remarks for why), so it is located and invoked via reflection rather than a direct, compile-time
            //call, keeping this file #if-free and buildable under every DataProvider.
            var readImageMethod = repo.GetType().GetMethod("ReadEmojiImageBytes", BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(readImageMethod);

            var samplePngEmoji = emojis.First(e => e.ImageEntry.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
            var imageBytesTask = (Task<byte[]>)readImageMethod!.Invoke(repo, [samplePngEmoji.ImageEntry])!;
            var imageBytes = await imageBytesTask;

            //A real PNG file signature - not corrupted/empty, and (see this class's own remarks) not still
            //GZip-wrapped either, since the two magic numbers are mutually exclusive.
            Assert.NotEmpty(imageBytes);
            byte[] pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
            Assert.True(imageBytes.Length >= pngSignature.Length, $"Expected at least {pngSignature.Length} bytes, got {imageBytes.Length}.");
            Assert.Equal(pngSignature, imageBytes.Take(pngSignature.Length).ToArray());
        }
    }
}
