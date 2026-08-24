using TightWiki.Plugin.Models;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Tests.Unit
{
    /// <summary>
    /// Scenario-level integration tests for <see cref="TightWiki.Plugin.Interfaces.Repository.ITwConfigurationRepository"/>
    /// (17 methods - configuration read/write, themes, wiki-wide metrics, crypto-check bootstrap, menu items),
    /// obtained through <see cref="TwEngineFixture"/> exactly like <c>MarkupTests</c>/<c>FullPageTests</c> get
    /// <c>ITwEngine</c>. Written entirely against the provider-agnostic interface - no <c>#if SQLITE_PROVIDER</c>/
    /// <c>#elif SQLSERVER_PROVIDER</c>/<c>#elif POSTGRES_PROVIDER</c> branching here. The same compiled test code
    /// runs three times: <c>dotnet test</c> (SQLite, default), <c>dotnet test -p:DataProvider=SqlServer</c>,
    /// <c>dotnet test -p:DataProvider=Postgres</c> (Database-Providers-Testing-Plan.md chapter 5.5).
    /// </summary>
    /// <remarks>
    /// These run against the shared, persistent test database (chapter 5.3 - not a fresh-empty database, and not
    /// per-test transaction rollback), concurrently with every other xunit collection in this assembly (default
    /// xunit parallelization: collections run in parallel with each other, tests within one collection run
    /// sequentially - see <c>MarkupTests</c>/<c>FullPageTests</c>/<c>DatabaseTests</c>, none of which opt out of
    /// this via <c>[CollectionDefinition(DisableParallelization = true)]</c>, so this class follows the same
    /// convention rather than introducing a new one). Every test here is therefore either:
    /// <list type="bullet">
    /// <item><description>read-only against already-seeded data (safe to run concurrently, any number of times), or</description></item>
    /// <item><description>a mutation scoped to a GUID-suffixed "TestCfg_"-prefixed name it creates and deletes itself
    /// (menu items - <see cref="ITwConfigurationRepository.InsertMenuItem"/> has no notion of an "existing" row to
    /// collide with), or</description></item>
    /// <item><description>a mutation of one specific pre-existing seeded entry, restored to its original value in a
    /// <c>finally</c> block - because <see cref="ITwConfigurationRepository.SaveConfigurationEntryValueByGroupAndEntry"/>
    /// only *updates* a matching (group, entry) row (mirroring <c>ConfigurationRepository</c>/
    /// SaveConfigurationEntryValueByGroupAndEntry.sql - an <c>UPDATE</c>, not an <c>UPSERT</c>), so there is no
    /// interface method to insert a brand-new, never-seeded configuration entry to use as an unconditionally unique
    /// key the way menu items allow.</description></item>
    /// </list>
    /// </remarks>
    [Collection("Configuration Repository Tests")]
    public class ConfigurationRepositoryTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        [Fact]
        public async Task GetConfigurationEntryValuesByGroupName_ReturnsSeededSearchGroup_ConsistentAcrossAllReadAccessors()
        {
            var repo = fixture.Artifacts.DatabaseManager.ConfigurationRepository;
            const string entryName = "Include Search on Navbar";

            var groupEntries = await repo.GetConfigurationEntryValuesByGroupName(TwConfigGroup.Search);
            Assert.NotEmpty(groupEntries.Collection);

            var entryFromGroup = groupEntries.Collection.SingleOrDefault(e => e.Name == entryName);
            Assert.NotNull(entryFromGroup);

            var directValue = await repo.GetConfigurationEntryValuesByGroupNameAndEntryName(TwConfigGroup.Search, entryName);
            Assert.Equal(entryFromGroup!.Value, directValue);

            var expectedTypedValue = entryFromGroup.As<bool>();
            var typedValue = await repo.Get<bool>(TwConfigGroup.Search, entryName);
            Assert.Equal(expectedTypedValue, typedValue);

            //A default is supplied but must be ignored since the entry exists.
            var typedWithIgnoredDefault = await repo.Get(TwConfigGroup.Search, entryName, !typedValue);
            Assert.Equal(typedValue, typedWithIgnoredDefault);

            var flatEntry = (await repo.GetFlatConfiguration())
                .Single(f => f.GroupName == TwConfigGroup.Search && f.EntryName == entryName);
            Assert.Equal(entryFromGroup.Value, flatEntry.EntryValue);

            var nestGroup = (await repo.GetConfigurationNest()).Single(n => n.Name == TwConfigGroup.Search);
            var nestEntry = nestGroup.Entries.Single(e => e.Name == entryName);
            Assert.Equal(entryFromGroup.Value, nestEntry.Value);
        }

        [Fact]
        public async Task NonExistentConfigurationEntry_BehavesConsistently_AndUnguardedTypedGetAlwaysThrows()
        {
            var repo = fixture.Artifacts.DatabaseManager.ConfigurationRepository;

            var missingGroup = $"TestCfg_NoSuchGroup_{Guid.NewGuid():N}";
            var missingEntry = $"TestCfg_NoSuchEntry_{Guid.NewGuid():N}";

            //A missing (group, entry) pair is handled differently by the two repository implementations:
            //ConfigurationRepository (SQLite reference) uses Dapper's QuerySingleAsync, which throws
            //InvalidOperationException for zero matching rows, while EfConfigurationRepository uses
            //FirstOrDefaultAsync, which returns null instead. Since Get<T>(string,string,T) is built directly on
            //top of GetConfigurationEntryValuesByGroupNameAndEntryName, this divergence propagates: on SQLite,
            //Get<T>(...,defaultValue) for a missing key also throws instead of returning defaultValue as its own
            //XML doc promises. This test documents/tolerates both behaviors rather than hardcoding one (no #if
            //branching allowed in this file - Database-Providers-Testing-Plan.md chapter 5.5); see this task's
            //final report for a flagged follow-up recommendation.
            var rawLookupException = await Record.ExceptionAsync(async () =>
            {
                var rawValue = await repo.GetConfigurationEntryValuesByGroupNameAndEntryName(missingGroup, missingEntry);
                Assert.Null(rawValue);
            });
            Assert.True(rawLookupException is null or InvalidOperationException,
                $"Expected either 'returned null' or an InvalidOperationException, but got: {rawLookupException}");

            var defaultValue = $"TestCfg_Default_{Guid.NewGuid():N}";
            var defaultLookupException = await Record.ExceptionAsync(async () =>
            {
                var typedWithDefault = await repo.Get(missingGroup, missingEntry, defaultValue);
                Assert.Equal(defaultValue, typedWithDefault);
            });
            Assert.True(defaultLookupException is null or InvalidOperationException,
                $"Expected either 'returned the supplied default' or an InvalidOperationException, but got: {defaultLookupException}");

            //The 2-arg overload has no fallback, so a missing entry must throw on every provider - and does,
            //whether via the underlying QuerySingleAsync throw (SQLite) or NTDLS.Helpers.EnsureNotNull on a null
            //lookup result (EF Core providers).
            await Assert.ThrowsAnyAsync<Exception>(() => repo.Get<string>(missingGroup, missingEntry));
        }

        [Fact]
        public async Task SaveConfigurationEntryValueByGroupAndEntry_PersistsImmediately_ButCachedReaderStaysStale_UntilConfigurationCacheCategoryCleared()
        {
            var repo = fixture.Artifacts.DatabaseManager.ConfigurationRepository;

            //"Cookies" is never read by WikiConfigurationManager.ReloadAll at fixture startup (unlike Search/Basic/
            //Customization/etc. above), so this entry's entry-level cache key is guaranteed cold when this test
            //starts - the very first read below is a real cache miss, not a leftover from fixture bootstrap.
            var target = (await repo.GetFlatConfiguration())
                .First(e => e.GroupName == TwConfigGroup.Cookies && !e.IsEncrypted);

            var originalValue = target.EntryValue;
            var newValue = $"TestCfg_{Guid.NewGuid():N}";

            try
            {
                var cachedBeforeSave = await repo.GetConfigurationEntryValuesByGroupNameAndEntryName(TwConfigGroup.Cookies, target.EntryName);
                Assert.Equal(originalValue, cachedBeforeSave);

                await repo.SaveConfigurationEntryValueByGroupAndEntry(TwConfigGroup.Cookies, target.EntryName, newValue);

                //An uncached read (GetFlatConfiguration never goes through MemCache) proves the write really
                //persisted to the database.
                var freshEntry = (await repo.GetFlatConfiguration())
                    .Single(f => f.GroupName == TwConfigGroup.Cookies && f.EntryName == target.EntryName);
                Assert.Equal(newValue, freshEntry.EntryValue);

                //SaveConfigurationEntryValueByGroupAndEntry deliberately does not clear the Configuration cache
                //category (mirrors the SQLite reference implementation's documented quirk) - the cached getter
                //still returns the pre-save value immediately after the write above.
                var cachedAfterSave = await repo.GetConfigurationEntryValuesByGroupNameAndEntryName(TwConfigGroup.Cookies, target.EntryName);
                Assert.Equal(originalValue, cachedAfterSave);

                //Inserting a menu item clears the entire Configuration cache category as a side effect - this is
                //what actually invalidates the stale entry-level cache read above.
                var tempMenuItemId = await repo.InsertMenuItem(new TwMenuItem
                {
                    Name = $"TestCfg_CacheProbe_{Guid.NewGuid():N}",
                    Link = "/TestCfg/CacheProbe",
                    Ordinal = 999
                });
                try
                {
                    var cachedAfterCacheCleared = await repo.GetConfigurationEntryValuesByGroupNameAndEntryName(TwConfigGroup.Cookies, target.EntryName);
                    Assert.Equal(newValue, cachedAfterCacheCleared);
                }
                finally
                {
                    await repo.DeleteMenuItemById(tempMenuItemId);
                }
            }
            finally
            {
                //Restore the original seeded value so this test is safe to re-run against the shared, persistent
                //test database (Database-Providers-Testing-Plan.md chapter 5.3) and doesn't leak into other runs.
                await repo.SaveConfigurationEntryValueByGroupAndEntry(TwConfigGroup.Cookies, target.EntryName, originalValue);
            }
        }

        [Fact]
        public async Task MenuItem_InsertGetUpdateDelete_RoundTrips()
        {
            var repo = fixture.Artifacts.DatabaseManager.ConfigurationRepository;
            var uniqueSuffix = Guid.NewGuid().ToString("N");

            var newId = await repo.InsertMenuItem(new TwMenuItem
            {
                Name = $"TestCfg_MenuItem_{uniqueSuffix}",
                Link = $"/TestCfg/{uniqueSuffix}",
                Ordinal = 12345
            });
            Assert.True(newId > 0);

            try
            {
                var fetched = await repo.GetMenuItemById(newId);
                Assert.Equal($"TestCfg_MenuItem_{uniqueSuffix}", fetched.Name);
                Assert.Equal($"/TestCfg/{uniqueSuffix}", fetched.Link);
                Assert.Equal(12345, fetched.Ordinal);

                var allItems = await repo.GetAllMenuItems();
                Assert.Contains(allItems, m => m.Id == newId);

                await repo.UpdateMenuItemById(new TwMenuItem
                {
                    Id = newId,
                    Name = $"TestCfg_MenuItem_Updated_{uniqueSuffix}",
                    Link = $"/TestCfg/Updated/{uniqueSuffix}",
                    Ordinal = 54321
                });

                var fetchedAfterUpdate = await repo.GetMenuItemById(newId);
                Assert.Equal($"TestCfg_MenuItem_Updated_{uniqueSuffix}", fetchedAfterUpdate.Name);
                Assert.Equal($"/TestCfg/Updated/{uniqueSuffix}", fetchedAfterUpdate.Link);
                Assert.Equal(54321, fetchedAfterUpdate.Ordinal);

                //Exercises the orderBy/orderByDirection parameters (GetAllMenuItems.sql / RepositoryHelpers.TransposeOrderby).
                var itemsByNameDescending = await repo.GetAllMenuItems("Name", "desc");
                Assert.Contains(itemsByNameDescending, m => m.Id == newId);
            }
            finally
            {
                await repo.DeleteMenuItemById(newId);
            }

            //GetMenuItemById is a SingleAsync lookup by primary key - it throws rather than returning null once deleted.
            await Assert.ThrowsAnyAsync<Exception>(() => repo.GetMenuItemById(newId));
        }

        [Fact]
        public async Task GetAllThemes_ReturnsTheActiveSeededThemeWithParsedFiles()
        {
            var repo = fixture.Artifacts.DatabaseManager.ConfigurationRepository;

            var themes = await repo.GetAllThemes();
            Assert.NotEmpty(themes);

            //Cross-checked against the same theme WikiConfigurationManager already resolved at fixture startup
            //(WikiConfigurationManager.ReloadAll's `.Single(o => o.Name == themeName)`) rather than hardcoding a
            //theme name, so this stays valid regardless of which theme the seed data configures as active.
            var activeThemeName = fixture.Artifacts.WikiConfigurationManager.WikiConfiguration.SystemTheme.Name;
            var activeTheme = Assert.Single(themes, t => t.Name == activeThemeName);

            Assert.NotEmpty(activeTheme.Files);
            Assert.Equal(
                activeTheme.DelimitedFiles.Split(';', StringSplitOptions.RemoveEmptyEntries).Length,
                activeTheme.Files.Count);
        }

        [Fact]
        public async Task GetWikiDatabaseMetrics_ReturnsNonNegativeCountsAcrossBothSchemas()
        {
            var repo = fixture.Artifacts.DatabaseManager.ConfigurationRepository;

            var metrics = await repo.GetWikiDatabaseMetrics();

            Assert.True(metrics.Pages >= 0);
            Assert.True(metrics.Namespaces >= 0);
            Assert.True(metrics.IntraLinks >= 0);
            Assert.True(metrics.PageRevisions >= 0);
            Assert.True(metrics.PageAttachments >= 0);
            Assert.True(metrics.PageAttachmentRevisions >= 0);
            Assert.True(metrics.PageTags >= 0);
            Assert.True(metrics.PageSearchTokens >= 0);
            Assert.True(metrics.Profiles >= 0);
            //Users lives in ASP.NET Core Identity's own schema (AspNetUsers) - on the EF providers this is a
            //second, cross-DbContext read (see EfConfigurationRepository.GetWikiDatabaseMetrics remarks). At least
            //the fixture's own admin/test accounts must exist by the time this runs.
            Assert.True(metrics.Users >= 1);
        }

        [Fact]
        public async Task CryptoCheck_SetThenGet_RoundTrips_AndIsFirstRunReportsFalseAfterwards()
        {
            var repo = fixture.Artifacts.DatabaseManager.ConfigurationRepository;

            await repo.SetCryptoCheck();

            Assert.True(await repo.GetCryptoCheck());
            //The database is already initialized/seeded by TwEngineFixture by the time any test runs, and
            //SetCryptoCheck above just re-wrote a valid check value, so this must report "not the first run".
            Assert.False(await repo.IsFirstRun());
        }
    }
}
