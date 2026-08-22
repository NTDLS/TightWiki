using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;

namespace TightWiki.Tests.Unit
{
    /// <summary>
    /// Scenario-level integration tests for <see cref="ITwStatisticsRepository"/> (6 methods - page view counts and
    /// compilation statistics, plus the paged admin listing over them), obtained through <see cref="TwEngineFixture"/>
    /// exactly like <see cref="ConfigurationRepositoryTests"/>/<see cref="LoggingRepositoryTests"/> get their own
    /// repositories. Written entirely against the provider-agnostic interface - no <c>#if SQLITE_PROVIDER</c>/
    /// <c>#elif SQLSERVER_PROVIDER</c>/<c>#elif POSTGRES_PROVIDER</c> branching here. The same compiled test code
    /// runs three times: <c>dotnet test</c> (SQLite, default), <c>dotnet test -p:DataProvider=SqlServer</c>,
    /// <c>dotnet test -p:DataProvider=Postgres</c> (Database-Providers-Testing-Plan.md chapter 5.5).
    /// </summary>
    /// <remarks>
    /// These run against the shared, persistent test database (chapter 5.3 - not a fresh-empty database, and not
    /// per-test transaction rollback), concurrently with every other xunit collection in this assembly (default
    /// xunit parallelization: collections run in parallel with each other, tests within one collection run
    /// sequentially - see <see cref="ConfigurationRepositoryTests"/>/<see cref="LoggingRepositoryTests"/> for the
    /// same reasoning). PageStatistics is a *much* more sensitive shared table than Log or Configuration, for a
    /// reason specific to this assembly, not just "other tests might also be writing to it concurrently":
    /// <list type="bullet">
    /// <item><description><c>FullPageTests</c>' ~1500 generated <c>Markup\*.wiki</c> golden-file cases include
    /// several dozen exercising the built-in <c>##MostViewed()</c>/<c>##PageviewCount()</c> markup functions
    /// (<c>TestMostViewed_*</c>/<c>TestPageviewCount_*</c>), whose <c>.wiki.expected</c> files bake in the *exact*
    /// <c>TotalViewCount</c> of every real page that currently has a PageStatistics row (<c>##MostViewed()</c> has
    /// no topCount/namespace filtering at all - see <c>PageRepository.GetTopViewedPagesInfo</c>/
    /// <c>GetTopViewedPagesInfo.sql</c>'s unfiltered <c>INNER JOIN</c> - so it reflects literally every row in the
    /// table), and every one of those cases runs concurrently, in a separate xunit collection, for the whole
    /// duration of any full/unfiltered <c>dotnet test</c> run. Unlike Log (nothing else in this assembly reads it -
    /// see <see cref="LoggingRepositoryTests"/>'s own remarks) or Configuration, mutating PageStatistics here is
    /// therefore observable by a concurrently-running, otherwise-unrelated xunit collection's golden-file
    /// comparison, not just by tests in this file.</description></item>
    /// <item><description><b><c>PurgePageStatistics</c> is deliberately not covered by a test here</b> - it is the
    /// one member of this interface with no way to scope its effect (mirrors <c>PurgePageStatistics.sql</c>'s
    /// unconditional "DELETE FROM PageStatistics;"), and calling the real method against this specific shared,
    /// actively-written database was empirically confirmed (twice, while writing this class) to break
    /// <c>TestMostViewed_*</c>/<c>TestPageviewCount_*</c> golden files during a full, unfiltered <c>dotnet test</c>
    /// run: a first attempt with no restoration at all took the documented 51 pre-existing failures to 79 (all 28
    /// new ones in that family); a second attempt that snapshotted every row before purging and restored every
    /// page's TotalViewCount immediately afterward *still* intermittently failed, both on its own "table is now
    /// empty" assertion (a concurrently-running <c>MarkupTests</c>/<c>FullPageTests</c> case re-inserted a PageId
    /// 0/1 row within the same instant) and on several golden files landing inside the few-hundred-millisecond-to-
    /// low-seconds restore window - because <c>MarkupTests</c>/<c>FullPageTests</c> write PageId 0/1 rows
    /// essentially continuously for the whole run (see the next bullet), not just once. Disabling xunit
    /// parallelization to close this window would be a new precedent this class deliberately avoids introducing
    /// (see <see cref="ConfigurationRepositoryTests"/>/<see cref="LoggingRepositoryTests"/>'s own remarks on
    /// following, not extending, existing convention). <see cref="ITwStatisticsRepository.PurgePageStatistics"/>'s
    /// own implementation was still read and compared against the SQLite reference (see
    /// <c>EfStatisticsRepository.PurgePageStatistics</c>'s doc comment - a one-line <c>ExecuteDeleteAsync()</c>,
    /// about as low-risk as EF Core LINQ gets) as part of this task's review, just not exercised live here.</description></item>
    /// <item><description>Two PageIds are especially "hot" for the reason above and are deliberately never
    /// targeted for mutation by any test that remains below: PageId 0 (every one of <c>MarkupTests</c>'
    /// <c>[InlineData]</c> cases calls the pageless <c>Engine.Transform(localizer, session, string)</c> overload,
    /// which transforms a synthetic/adhoc <see cref="TwPage"/> with <c>Id == 0</c> - see
    /// <c>EfStatisticsRepository.MergePageCompilationStatistics</c>'s own doc comment for the same PageId 0 case)
    /// and PageId 1 (every one of <c>FullPageTests</c>' cases renders through <c>MockWikiEngineArtifacts.GetMockPage</c>,
    /// which hardcodes <c>Id = 1</c> - the real, seeded "Home" page, whose <c>TotalViewCount</c> the
    /// <c>TestPageviewCount_000001</c> golden file bakes in directly). PageId 0 has no matching <c>Page</c> row for
    /// <c>GetTopViewedPagesInfo</c>'s <c>INNER JOIN</c> to find, so it is structurally safe there regardless (used
    /// by <see cref="MergePageCompilationStatistics_NonPositivePageId_NeverThrows_RegardlessOfProviderGuard"/>
    /// below).</description></item>
    /// <item><description>The remaining two mutating tests below instead target "Sandbox :: Default" (Namespace
    /// "Sandbox") - a real page from the actual seed content set shipped for every provider (<c>Data\pages.db</c>/
    /// <c>TwDefaultDataType.SandboxPages</c>), not one <see cref="TwEngineFixture"/> creates itself (its own
    /// <c>GenerateTestPages</c>/<c>CreateFixtureInstance</c> methods exist but are never actually invoked from its
    /// constructor - confirmed by inspection). Confirmed (by inspecting every current <c>TestMostViewed_*.wiki.expected</c>
    /// file) to have no PageStatistics row at all at baseline - namespaces "Sandbox"/"Builtin"/"Include" never
    /// appear in that golden output - so it contributes nothing there as long as its row doesn't outlive the single
    /// test that creates it; both tests delete it again in a <c>finally</c> block for exactly that reason. This is
    /// a narrower, single-row mutation - unlike a full-table purge, nothing else in this assembly ever touches this
    /// specific page's PageId (confirmed by inspection: every other real-page-touching call site in
    /// <c>TightWiki.Tests</c> uses PageId 0 or 1, per the bullet above), so there is no analogous concurrent-writer
    /// race here.</description></item>
    /// </list>
    /// </remarks>
    [Collection("Statistics Repository Tests")]
    public class StatisticsRepositoryTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        /// <summary>
        /// The full name (with namespace prefix) of the real, already-seeded page the mutating tests below target
        /// (aside from <see cref="MergePageCompilationStatistics_NonPositivePageId_NeverThrows_RegardlessOfProviderGuard"/>,
        /// which uses sentinel PageIds instead) - see this class's own remarks for why this specific page.
        /// </summary>
        private const string SeededPageName = "Sandbox :: Default";

        /// <summary>
        /// Looks up <see cref="SeededPageName"/>'s stored PageId via <see cref="ITwPageRepository.GetPageInfoByNavigation"/>,
        /// computing the navigation key the same way <c>PageRepository.SavePage</c>/<c>EfPageRepository.SavePage</c>
        /// derive it from a page's <c>Name</c> (<see cref="TwNamespaceNavigation.CleanAndValidate(string?, bool)"/>)
        /// rather than hardcoding the already-cleaned/lowercased string, so this stays correct even if that
        /// cleaning logic ever changes.
        /// </summary>
        private static async Task<TwPage> GetSeededTestPageAsync(ITwPageRepository pageRepo)
        {
            var navigation = TwNamespaceNavigation.CleanAndValidate(SeededPageName);
            return await pageRepo.GetPageInfoByNavigation(navigation)
                ?? throw new Exception($"Could not find the seeded page at navigation '{navigation}'.");
        }

        /// <summary>
        /// Scans every page of <see cref="ITwStatisticsRepository.GetPageStatisticsPaged"/>, ordered by PageId
        /// ascending (bounded by the first page's own <see cref="TwPageStatistics.PaginationPageCount"/>), and
        /// returns every row found. Mirrors <c>LoggingRepositoryTests.FindLogEntryByTextAsync</c>'s own reasoning
        /// for not trusting a single page against the shared test database.
        /// </summary>
        private static async Task<List<TwPageStatistics>> GetAllPageStatisticsAsync(ITwStatisticsRepository repo)
        {
            var all = new List<TwPageStatistics>();

            var firstPage = await repo.GetPageStatisticsPaged(1, orderBy: "PageId", orderByDirection: "asc");
            all.AddRange(firstPage);
            var totalPages = firstPage.Count > 0 ? firstPage[0].PaginationPageCount : 1;

            for (var pageNumber = 2; pageNumber <= totalPages; pageNumber++)
            {
                all.AddRange(await repo.GetPageStatisticsPaged(pageNumber, orderBy: "PageId", orderByDirection: "asc"));
            }

            return all;
        }

        /// <summary>
        /// <see cref="GetAllPageStatisticsAsync"/>, filtered down to the one row matching <paramref name="pageId"/>
        /// (or null if there isn't one).
        /// </summary>
        private static async Task<TwPageStatistics?> FindPageStatisticsByPageIdAsync(ITwStatisticsRepository repo, int pageId)
            => (await GetAllPageStatisticsAsync(repo)).FirstOrDefault(s => s.PageId == pageId);

        [Fact]
        public async Task IncrementPageViewCount_And_GetPageTotalViewCount_RoundTrip_WithDeltaAssertions()
        {
            var statsRepo = fixture.Artifacts.DatabaseManager.StatisticsRepository;
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;

            var page = await GetSeededTestPageAsync(pageRepo);
            try
            {
                //Before/after deltas rather than absolute counts - this page's PageStatistics row may already have
                //a non-zero TotalViewCount from other activity against the shared, persistent test database
                //(chapter 5.3), same reasoning as LoggingRepositoryTests' own delta-based assertions.
                var countBefore = await statsRepo.GetPageTotalViewCount(page.Id);

                await statsRepo.IncrementPageViewCount(page.Id);
                Assert.Equal(countBefore + 1, await statsRepo.GetPageTotalViewCount(page.Id));

                await statsRepo.IncrementPageViewCount(page.Id);
                Assert.Equal(countBefore + 2, await statsRepo.GetPageTotalViewCount(page.Id));

                //GetPageTotalViewCount.sql's "no matching row" fallback (mirrors EfStatisticsRepository.GetPageTotalViewCount's
                //own doc comment: "a genuinely sensible 'no statistics recorded yet' fallback") - a page id that
                //can never have a real PageStatistics row must read back as 0, not throw or return null.
                Assert.Equal(0, await statsRepo.GetPageTotalViewCount(int.MaxValue - 1));
            }
            finally
            {
                //This page must not keep a PageStatistics row after this test finishes - see this class's own
                //remarks: it would otherwise start appearing in ##MostViewed()'s unfiltered golden-file output the
                //next time FullPageTests runs (in this same process, concurrently, or in a later run against this
                //same shared/persistent database).
                await statsRepo.DeletePageStatisticsByPageId(page.Id);
            }
        }

        [Fact]
        public async Task MergePageCompilationStatistics_UpsertsAndAccumulates_AndGetPageStatisticsPagedPopulatesNamespace_RegressionFor2a9Bug()
        {
            var statsRepo = fixture.Artifacts.DatabaseManager.StatisticsRepository;
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;

            var page = await GetSeededTestPageAsync(pageRepo);
            try
            {
                //Sanity check this really is a namespaced page ("Sandbox :: Default" -> Namespace "Sandbox") -
                //otherwise the Namespace assertions below wouldn't actually exercise the phase 2a.9 bug (an
                //*empty* Namespace would pass trivially for a non-namespaced page even with the bug present).
                Assert.False(string.IsNullOrEmpty(page.Namespace));

                var statsBefore = await FindPageStatisticsByPageIdAsync(statsRepo, page.Id);
                var totalCompilationCountBefore = statsBefore?.TotalCompilationCount ?? 0;
                var totalWikifyTimeMsBefore = statsBefore?.TotalWikifyTimeMs ?? 0m;

                //First call - proves the upsert path handles both "row already exists" and "row does not exist
                //yet" identically from the caller's perspective (this page is expected to have no row at all at
                //baseline - see this class's own remarks - but the method itself must not care either way).
                await statsRepo.MergePageCompilationStatistics(page.Id, 12.5, 1, 0, 1, 1, 100, 100);

                //Second call, with different values - Last* columns must be overwritten (not accumulated), Total*
                //columns must accumulate on top of whatever was already there before our first call above.
                await statsRepo.MergePageCompilationStatistics(page.Id, 30.25, 7, 2, 3, 4, 555, 666);

                var statsAfter = await FindPageStatisticsByPageIdAsync(statsRepo, page.Id)
                    ?? throw new Exception($"Could not locate a PageStatistics row for page id {page.Id} after MergePageCompilationStatistics.");

                Assert.Equal(7, statsAfter.LastMatchCount);
                Assert.Equal(2, statsAfter.LastErrorCount);
                Assert.Equal(3, statsAfter.LastOutgoingLinkCount);
                Assert.Equal(4, statsAfter.LastTagCount);
                Assert.Equal(555, statsAfter.LastProcessedBodySize);
                Assert.Equal(666, statsAfter.LastBodySize);
                Assert.Equal(30.25m, statsAfter.LastWikifyTimeMs);

                Assert.Equal(totalCompilationCountBefore + 2, statsAfter.TotalCompilationCount);

                //A tolerance rather than exact equality for the accumulated wikify time: the "before" value may
                //carry genuine floating-point noise from a real elapsed-time measurement, and TotalWikifyTimeMs is
                //accumulated server-side as a double across two separate UPDATE statements - comparing that
                //against decimal arithmetic performed here could differ in the last few digits without indicating
                //an actual bug. The delta (our own two contributions, 12.5 + 30.25 = 42.75, both exactly
                //representable in binary) is what actually matters.
                var totalWikifyTimeMsDelta = statsAfter.TotalWikifyTimeMs - totalWikifyTimeMsBefore;
                Assert.True(Math.Abs(totalWikifyTimeMsDelta - 42.75m) < 0.001m,
                    $"Expected TotalWikifyTimeMs to have increased by ~42.75, but it increased by {totalWikifyTimeMsDelta}.");

                //Regression test for the phase 2a.9 bug (Database-Providers-Testing-Plan.md chapter 6 / this
                //task's own brief): GetPageStatisticsPaged in EfStatisticsRepository originally left
                //TwPageStatistics.Namespace unset (empty) for every row - used by the admin Statistics screen -
                //fixed in that phase. Asserted against the page's own real Namespace/Name/Navigation values (not
                //hardcoded literals), so this stays valid even if the seeded page's name ever changes.
                Assert.Equal(page.Namespace, statsAfter.Namespace);
                Assert.False(string.IsNullOrEmpty(statsAfter.Namespace));
                Assert.Equal(page.Name, statsAfter.PageName);
                Assert.Equal(page.Navigation, statsAfter.Navigation);
            }
            finally
            {
                //This page must not keep a PageStatistics row after this test finishes - see this class's own
                //remarks: it would otherwise start appearing in ##MostViewed()'s unfiltered golden-file output the
                //next time FullPageTests runs.
                await statsRepo.DeletePageStatisticsByPageId(page.Id);
            }
        }

        [Fact]
        public async Task MergePageCompilationStatistics_NonPositivePageId_NeverThrows_RegardlessOfProviderGuard()
        {
            var statsRepo = fixture.Artifacts.DatabaseManager.StatisticsRepository;

            //Sentinels no real code ever passes here: every persisted TwPage.Id is >= 1 (see TwPage.Id's own doc
            //comment - "a value of 0 indicates the page has not been saved"), and EngineHandlers.HandleCompletion -
            //the one production caller besides PageController - always passes state.Page.Id, which for the
            //synthetic/adhoc TwPage used by pageless markup transforms is exactly 0, never negative. Neither
            //sentinel has a matching Page row, so neither can ever be joined into ##MostViewed()'s
            //GetTopViewedPagesInfo (an INNER JOIN) regardless of what TotalViewCount ends up on any orphan row -
            //structurally safe against this class's own golden-file concern (see its remarks), unlike the
            //"Sandbox :: Default" page the other mutating tests below use.
            foreach (var pageId in new[] { 0, -777 })
            {
                //Deliberate, documented divergence between providers, both accepted here without #if branching
                //(Database-Providers-Testing-Plan.md chapter 5.5): EfStatisticsRepository treats pageId <= 0 as a
                //no-op (a real FK from PageStatistics.PageId to Page.Id would otherwise make the insert attempt
                //fail), while the SQLite reference has no such guard and silently inserts/updates an orphan row
                //that no Page will ever match. Either way, the call itself must never throw - that's what actually
                //matters to every caller (EngineHandlers.HandleCompletion awaits this on every single page compile,
                //pageless or not, and a throw there would break every page render).
                var exception = await Record.ExceptionAsync(() => statsRepo.MergePageCompilationStatistics(
                    pageId, 1.0, 1, 0, 0, 0, 10, 10));
                Assert.Null(exception);

                try
                {
                    //Whichever provider we're running against, GetPageTotalViewCount for this sentinel must land
                    //on one of exactly two well-understood outcomes: 0 (EF Core guard - no row was ever created,
                    //for us or for anyone else, since the guard applies unconditionally to pageId <= 0) or 1
                    //(SQLite reference - the orphan row's INSERT path always sets TotalViewCount = 1 for a brand
                    //new row, and is never touched again by later Merge calls once it exists - see
                    //EfStatisticsRepository.MergePageCompilationStatistics's own doc comment).
                    var viewCount = await statsRepo.GetPageTotalViewCount(pageId);
                    Assert.True(viewCount is 0 or 1,
                        $"Expected GetPageTotalViewCount({pageId}) to be 0 (EF Core guard) or 1 (SQLite reference orphan row), but got {viewCount}.");
                }
                finally
                {
                    //Cleanup regardless of provider - a no-op if the guard prevented any row from ever being
                    //created (EF Core), or removes the genuine orphan row otherwise (SQLite reference). Also
                    //exercises DeletePageStatisticsByPageId itself: its interface contract promises "the number of
                    //records deleted", but the SQLite reference has its own confirmed, documented bug of always
                    //returning 0 regardless of how many rows were actually deleted (see
                    //EfStatisticsRepository.DeletePageStatisticsByPageId's own doc comment) - so the return value
                    //is tolerated as 0 or 1 here rather than asserted exactly, while the real effect (the row is
                    //genuinely gone afterward) is asserted precisely below.
                    var deletedCount = await statsRepo.DeletePageStatisticsByPageId(pageId);
                    Assert.True(deletedCount is 0 or 1,
                        $"Expected DeletePageStatisticsByPageId({pageId}) to return 0 or 1, but got {deletedCount}.");

                    Assert.Equal(0, await statsRepo.GetPageTotalViewCount(pageId));
                }
            }
        }
    }
}
