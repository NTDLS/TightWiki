using TightWiki.Library;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;

namespace TightWiki.Tests.Unit
{
    /// <summary>
    /// Scenario-level integration tests for the first two slices of <see cref="ITwPageRepository"/> (86 members
    /// total, per <c>EfPageRepository</c>'s own class-level remarks - far too many for one test file, same
    /// reasoning as <see cref="UsersRepositoryRoleTests"/>/<see cref="UsersRepositoryPermissionAuthTests"/>/
    /// <see cref="UsersRepositoryProfileTests"/> split <see cref="ITwUsersRepository"/> into three files). This is
    /// the first of four <c>ITwPageRepository</c> test tasks, covering the 30 members
    /// <c>EfPageRepository</c>'s own class-level remarks call out as landing in phases 2b.2/2b.3: autocomplete,
    /// page-cache flushing, page comments, current-page-editors (11 members), plus page/revision metadata-read
    /// members (19 members) - <see cref="ITwPageRepository.AutoCompletePage"/>, <see
    /// cref="ITwPageRepository.AutoCompleteNamespace"/>, <see cref="ITwPageRepository.FlushPageCache"/>, <see
    /// cref="ITwPageRepository.InsertPageComment"/>, <see cref="ITwPageRepository.DeletePageCommentById"/>, <see
    /// cref="ITwPageRepository.DeletePageCommentByUserAndId"/>, <see
    /// cref="ITwPageRepository.GetTotalPageCommentCount"/>, <see cref="ITwPageRepository.GetPageCommentsPaged"/>,
    /// <see cref="ITwPageRepository.UpsertCurrentPageEditor"/>, <see
    /// cref="ITwPageRepository.DeleteCurrentPageEditor"/>, <see cref="ITwPageRepository.GetCurrentPageEditors"/>,
    /// <see cref="ITwPageRepository.GetPageRevisionInfoById"/>, <see
    /// cref="ITwPageRepository.GetPageProcessingInstructionsByPageId"/>, <see
    /// cref="ITwPageRepository.GetPageTagsById"/>, <see
    /// cref="ITwPageRepository.GetPageRevisionsInfoByNavigationPaged"/>, <see
    /// cref="ITwPageRepository.GetTopRecentlyModifiedPagesInfoByUserId"/>, <see
    /// cref="ITwPageRepository.GetPageNavigationByPageId"/>, <see
    /// cref="ITwPageRepository.GetTopRecentlyModifiedPagesInfo"/>, <see
    /// cref="ITwPageRepository.GetTopRecentlyCreatedPagesInfo"/>, <see
    /// cref="ITwPageRepository.GetTopViewedPagesInfo"/>, <see cref="ITwPageRepository.GetTopEditedPagesInfo"/>,
    /// <see cref="ITwPageRepository.GetCurrentPageRevision"/>, <see
    /// cref="ITwPageRepository.GetLimitedPageInfoByIdAndRevision"/>, <see
    /// cref="ITwPageRepository.GetPageInfoByNavigation"/>, <see
    /// cref="ITwPageRepository.GetPageRevisionCountByPageId"/>, <see
    /// cref="ITwPageRepository.GetLatestPageRevisionById"/>, <see cref="ITwPageRepository.GetPageNextRevision"/>,
    /// <see cref="ITwPageRepository.GetPagePreviousRevision"/>, and both <see
    /// cref="ITwPageRepository.GetPageRevisionByNavigation(TwNamespaceNavigation, int?)"/> overloads. Search/tags/
    /// tokens, bulk/paged listings, CRUD/upsert, file attachments, and delete/restore are out of scope - three
    /// follow-up tasks.
    /// <para>
    /// Obtained through <see cref="TwEngineFixture"/> exactly like every sibling repository test class in this
    /// project. Written entirely against the provider-agnostic interface - no <c>#if SQLITE_PROVIDER</c>/
    /// <c>#elif SQLSERVER_PROVIDER</c>/<c>#elif POSTGRES_PROVIDER</c> branching here. The same compiled test code
    /// runs three times: <c>dotnet test</c> (SQLite, default), <c>dotnet test -p:DataProvider=SqlServer</c>,
    /// <c>dotnet test -p:DataProvider=Postgres</c> (Database-Providers-Testing-Plan.md chapter 5.5).
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why every scenario below targets the real, already-seeded "Sandbox :: Default" page, never a page this
    /// class creates itself:</b> same reasoning as <c>StatisticsRepositoryTests</c>' own remarks - <see
    /// cref="ITwPageRepository"/> does have a real delete path (unlike <see cref="ITwUsersRepository"/>), but a
    /// page created via <see cref="ITwPageRepository.UpsertPage"/> and then torn down again via <see
    /// cref="ITwPageRepository.MovePageToDeletedById"/>/<see cref="ITwPageRepository.PurgeDeletedPageByPageId"/> is
    /// CRUD/delete territory - explicitly out of scope for this task (its own brief). "Sandbox :: Default" is a
    /// real page from the actual seed content set shipped for every provider, not counted in any
    /// <c>TestMostViewed_*</c>/<c>TestPageviewCount_*</c> golden file at baseline (confirmed by
    /// <c>StatisticsRepositoryTests</c>' own remarks), so mutating its Pages.PageComment/Pages.CurrentPageEditors
    /// rows here - both tables no golden <c>.wiki.expected</c> file reads from at all - cannot affect any
    /// concurrently-running <c>MarkupTests</c>/<c>FullPageTests</c> case. Every mutating test below restores the
    /// row count to its original value again in a <c>finally</c> block for exactly that reason.
    /// </para>
    /// <para>
    /// Every read-only test below (top-N listings, revision navigation, autocomplete) only reads from the shared,
    /// persistent test database - never mutates it - so there is no golden-file risk there either.
    /// </para>
    /// </remarks>
    [Collection("Page Repository Metadata Tests")]
    public class PageRepositoryMetadataTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        /// <summary>
        /// The full name (with namespace prefix) of the real, already-seeded page every test below targets - same
        /// page <c>StatisticsRepositoryTests</c> uses, for the same reasons (see this class's own remarks).
        /// </summary>
        private const string SeededPageName = "Sandbox :: Default";

        /// <summary>
        /// Looks up <see cref="SeededPageName"/>'s stored page metadata via <see
        /// cref="ITwPageRepository.GetPageInfoByNavigation"/>, computing the navigation key the same way
        /// <c>PageRepository.SavePage</c>/<c>EfPageRepository.SavePage</c> derive it from a page's <c>Name</c>
        /// (<see cref="TwNamespaceNavigation.CleanAndValidate(string?, bool)"/>) rather than hardcoding the
        /// already-cleaned/lowercased string - same helper as <c>StatisticsRepositoryTests.GetSeededTestPageAsync</c>.
        /// </summary>
        private static async Task<TwPage> GetSeededTestPageAsync(ITwPageRepository pageRepo)
        {
            var navigation = TwNamespaceNavigation.CleanAndValidate(SeededPageName);
            return await pageRepo.GetPageInfoByNavigation(navigation)
                ?? throw new Exception($"Could not find the seeded page at navigation '{navigation}'.");
        }

        [Fact]
        public async Task AutoCompletePage_AutoCompleteNamespace_ContainSeededSandboxPage()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;

            var page = await GetSeededTestPageAsync(pageRepo);

            //The seeded page's own full Name is unique enough (no other page can share the exact same Name -
            //Navigation is derived from it and must be unique) that it cannot be excluded by AutoCompletePage's own
            //25-row/alphabetical-order cap, unlike a short generic substring - same "exact-case substring of the
            //row's own name" reasoning as UsersRepositoryRoleTests.AutoCompleteRole's own assertions.
            var autoCompletePages = await pageRepo.AutoCompletePage(page.Name);
            Assert.Contains(autoCompletePages, p => p.Id == page.Id && p.Navigation == page.Navigation);

            //The seeded page's own Namespace ("Sandbox") is likewise used verbatim as the search text.
            Assert.False(string.IsNullOrEmpty(page.Namespace));
            var autoCompleteNamespaces = await pageRepo.AutoCompleteNamespace(page.Namespace);
            Assert.Contains(page.Namespace, autoCompleteNamespaces);

            //An unknown search text matches nothing in either method.
            var unknownSearchText = $"no-such-page-{Guid.NewGuid():N}";
            Assert.Empty(await pageRepo.AutoCompletePage(unknownSearchText));
            Assert.Empty(await pageRepo.AutoCompleteNamespace(unknownSearchText));
        }

        [Fact]
        public async Task GetPageInfoByNavigation_GetPageNavigationByPageId_GetCurrentPageRevision_GetPageRevisionCountByPageId_ReturnConsistentDataForSeededPage()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var navigation = TwNamespaceNavigation.CleanAndValidate(SeededPageName);

            var page = await pageRepo.GetPageInfoByNavigation(navigation);
            Assert.NotNull(page);
            Assert.Equal(SeededPageName, page!.Name);
            Assert.Equal(navigation, page.Navigation);

            Assert.Equal(navigation, await pageRepo.GetPageNavigationByPageId(page.Id));
            Assert.Equal(page.Revision, await pageRepo.GetCurrentPageRevision(page.Id));

            var revisionCount = await pageRepo.GetPageRevisionCountByPageId(page.Id);
            Assert.True(revisionCount >= 1);
            //The current revision number can never exceed the total number of revisions ever recorded.
            Assert.True(page.Revision <= revisionCount);

            //A page id/navigation that can never have a real row returns a sensible "not found" value, not throw.
            Assert.Null(await pageRepo.GetPageNavigationByPageId(int.MaxValue - 1));
            Assert.Equal(0, await pageRepo.GetCurrentPageRevision(int.MaxValue - 1));
            Assert.Equal(0, await pageRepo.GetPageRevisionCountByPageId(int.MaxValue - 1));
            Assert.Null(await pageRepo.GetPageInfoByNavigation($"no-such-page-{Guid.NewGuid():N}"));
        }

        [Fact]
        public async Task GetPageRevisionInfoById_GetLimitedPageInfoByIdAndRevision_GetLatestPageRevisionById_ReturnConsistentMetadataForCurrentRevision()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var page = await GetSeededTestPageAsync(pageRepo);

            //Default (null) revision resolves to the page's own current revision, same as explicitly passing it.
            var revisionInfoDefault = await pageRepo.GetPageRevisionInfoById(page.Id);
            var revisionInfoExplicit = await pageRepo.GetPageRevisionInfoById(page.Id, page.Revision);
            Assert.NotNull(revisionInfoDefault);
            Assert.NotNull(revisionInfoExplicit);
            Assert.Equal(page.Id, revisionInfoDefault!.Id);
            Assert.Equal(page.Revision, revisionInfoDefault.Revision);
            Assert.Equal(page.Navigation, revisionInfoDefault.Navigation);
            Assert.Equal(revisionInfoDefault.ModifiedDate, revisionInfoExplicit!.ModifiedDate);

            var limited = await pageRepo.GetLimitedPageInfoByIdAndRevision(page.Id);
            Assert.NotNull(limited);
            Assert.Equal(page.Revision, limited!.Revision);
            Assert.Equal(page.Revision, limited.MostCurrentRevision);
            Assert.Equal(page.Navigation, limited.Navigation);

            var latest = await pageRepo.GetLatestPageRevisionById(page.Id);
            Assert.NotNull(latest);
            Assert.Equal(page.Revision, latest!.Revision);
            Assert.Equal(page.Revision, latest.MostCurrentRevision);
            Assert.Equal(page.Navigation, latest.Navigation);
            //Unlike GetPageRevisionInfoById/GetLimitedPageInfoByIdAndRevision, GetLatestPageRevisionById includes
            //the revision body content.
            Assert.False(string.IsNullOrEmpty(latest.Body));

            //A page id that can never have a real row returns null, not throw.
            Assert.Null(await pageRepo.GetPageRevisionInfoById(int.MaxValue - 1));
            Assert.Null(await pageRepo.GetLimitedPageInfoByIdAndRevision(int.MaxValue - 1));
            Assert.Null(await pageRepo.GetLatestPageRevisionById(int.MaxValue - 1));
        }

        [Fact]
        public async Task GetPageRevisionByNavigation_BothOverloads_ReturnSamePageAsGetPageInfoByNavigation_AndNullForUnknown()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var page = await GetSeededTestPageAsync(pageRepo);

            var viaNamespaceNavigation = await pageRepo.GetPageRevisionByNavigation(new TwNamespaceNavigation(page.Navigation));
            Assert.NotNull(viaNamespaceNavigation);
            Assert.Equal(page.Id, viaNamespaceNavigation!.Id);
            Assert.Equal(page.Revision, viaNamespaceNavigation.Revision);

            var viaString = await pageRepo.GetPageRevisionByNavigation(page.Navigation);
            Assert.NotNull(viaString);
            Assert.Equal(page.Id, viaString!.Id);
            Assert.Equal(page.Revision, viaString.Revision);

            //refreshCache: true forces the cache entry to be evicted and rebuilt - result must be identical.
            var viaStringRefreshed = await pageRepo.GetPageRevisionByNavigation(page.Navigation, revision: null, refreshCache: true);
            Assert.NotNull(viaStringRefreshed);
            Assert.Equal(page.Id, viaStringRefreshed!.Id);
            Assert.Equal(page.Revision, viaStringRefreshed.Revision);

            //Explicit revision number, matching the current one.
            var viaStringExplicitRevision = await pageRepo.GetPageRevisionByNavigation(page.Navigation, revision: page.Revision);
            Assert.NotNull(viaStringExplicitRevision);
            Assert.Equal(page.Id, viaStringExplicitRevision!.Id);

            var unknownNavigation = $"no-such-page-{Guid.NewGuid():N}";
            Assert.Null(await pageRepo.GetPageRevisionByNavigation(new TwNamespaceNavigation(unknownNavigation)));
            Assert.Null(await pageRepo.GetPageRevisionByNavigation(unknownNavigation));
        }

        [Fact]
        public async Task GetPageRevisionsInfoByNavigationPaged_GetPageNextRevision_GetPagePreviousRevision_ForSeededPage()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var page = await GetSeededTestPageAsync(pageRepo);

            var revisionsDescending = await pageRepo.GetPageRevisionsInfoByNavigationPaged(page.Navigation, 1);
            Assert.NotEmpty(revisionsDescending);
            //The current revision must always be present in the history, with zero revisions higher than it.
            var currentRevisionRow = Assert.Single(revisionsDescending, r => r.Revision == page.Revision);
            Assert.Equal(0, currentRevisionRow.HigherRevisionCount);
            Assert.Equal(page.Navigation, currentRevisionRow.Navigation);

            //Explicit ordering (ascending "Revision") - exercises the orderBy/orderByDirection parameters and
            //RepositoryHelpers.TransposeOrderby mapping, same idiom as UsersRepositoryProfileTests' own
            //GetAllUsersPaged(orderBy: "Account", orderByDirection: "asc") case.
            var revisionsAscending = await pageRepo.GetPageRevisionsInfoByNavigationPaged(page.Navigation, 1, orderBy: "Revision", orderByDirection: "asc");
            Assert.NotEmpty(revisionsAscending);
            for (var i = 1; i < revisionsAscending.Count; i++)
            {
                Assert.True(revisionsAscending[i - 1].Revision <= revisionsAscending[i].Revision);
            }

            //An unrecognized orderBy throws, matching RepositoryHelpers.TransposeOrderby's own "No order by
            //mapping..." exception - same convention documented on EfPageRepository.GetPageRevisionsInfoByNavigationPaged.
            //ThrowsAnyAsync<Exception> (not the exact InvalidOperationException EfPageRepository throws) because
            //the SQLite reference's own RepositoryHelpers.TransposeOrderby throws a plain System.Exception instead -
            //a pre-existing, documented divergence between providers (Database-Providers-Testing-Plan.md chapter
            //5.5), not something this test should paper over with an #if branch.
            await Assert.ThrowsAnyAsync<Exception>(
                () => pageRepo.GetPageRevisionsInfoByNavigationPaged(page.Navigation, 1, orderBy: "NoSuchColumn"));

            //Revision-boundary checks that hold regardless of how many revisions this page actually has: nothing
            //is higher than the current revision, and nothing is lower than revision 1.
            Assert.Equal(0, await pageRepo.GetPageNextRevision(page.Id, page.Revision));
            Assert.Equal(0, await pageRepo.GetPagePreviousRevision(page.Id, 1));

            //If this page happens to have more than one revision, the next/previous walk must actually move
            //between real, adjacent revision numbers.
            if (page.Revision > 1)
            {
                var previousOfCurrent = await pageRepo.GetPagePreviousRevision(page.Id, page.Revision);
                Assert.True(previousOfCurrent > 0 && previousOfCurrent < page.Revision);

                var nextOfFirst = await pageRepo.GetPageNextRevision(page.Id, 1);
                Assert.True(nextOfFirst > 1);
            }
        }

        [Fact]
        public async Task GetPageProcessingInstructionsByPageId_GetPageTagsById_ReturnNonNullCollectionsForSeededPage()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var page = await GetSeededTestPageAsync(pageRepo);

            var instructions = await pageRepo.GetPageProcessingInstructionsByPageId(page.Id);
            Assert.NotNull(instructions);
            Assert.NotNull(instructions.Collection);

            var tags = await pageRepo.GetPageTagsById(page.Id);
            Assert.NotNull(tags);

            //A page id that can never have a real row returns an empty collection, not null/throw.
            var noSuchPageInstructions = await pageRepo.GetPageProcessingInstructionsByPageId(int.MaxValue - 1);
            Assert.Empty(noSuchPageInstructions.Collection);
            Assert.Empty(await pageRepo.GetPageTagsById(int.MaxValue - 1));
        }

        [Fact]
        public async Task GetTopRecentlyModifiedPagesInfo_GetTopRecentlyCreatedPagesInfo_GetTopViewedPagesInfo_GetTopEditedPagesInfo_ReturnOrderedBoundedLists()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            const int topCount = 5;

            //Unlike the three lists below, the returned row's own ModifiedDate cannot be used to verify ordering
            //here: GetTopRecentlyModifiedPagesInfo.sql orders by the *Page*'s own ModifiedDate column but returns
            //the *joined current PageRevision*'s ModifiedDate instead (EfPageRepository.GetTopRecentlyModifiedPagesInfo's
            //own doc comment documents the same literal quirk) - the two are not guaranteed to compare identically
            //against the shared seed data (confirmed empirically: asserting descending order on the returned field
            //intermittently fails). GetPageInfoByNavigation, by contrast, returns the Page's own ModifiedDate
            //directly (EfPageRepository.GetPageInfoByNavigation's own Select) - the very column
            //GetTopRecentlyModifiedPagesInfo's ORDER BY is sourced from - so each returned page's real
            //Page.ModifiedDate is independently re-fetched via GetPageInfoByNavigation below, and the descending
            //order verified against those independently-obtained values instead.
            var recentlyModified = await pageRepo.GetTopRecentlyModifiedPagesInfo(topCount);
            Assert.NotEmpty(recentlyModified);
            Assert.True(recentlyModified.Count <= topCount);

            var recentlyModifiedActualPageDates = new List<DateTime>();
            foreach (var page in recentlyModified)
            {
                var pageInfo = await pageRepo.GetPageInfoByNavigation(page.Navigation);
                Assert.NotNull(pageInfo);
                recentlyModifiedActualPageDates.Add(pageInfo!.ModifiedDate);
            }
            for (var i = 1; i < recentlyModifiedActualPageDates.Count; i++)
            {
                Assert.True(recentlyModifiedActualPageDates[i - 1] >= recentlyModifiedActualPageDates[i]);
            }

            var recentlyCreated = await pageRepo.GetTopRecentlyCreatedPagesInfo(topCount);
            Assert.NotEmpty(recentlyCreated);
            Assert.True(recentlyCreated.Count <= topCount);
            for (var i = 1; i < recentlyCreated.Count; i++)
            {
                Assert.True(recentlyCreated[i - 1].CreatedDate >= recentlyCreated[i].CreatedDate);
            }

            //Read-only - the seeded "Home" page (Id 1) already carries a real PageStatistics row baked into the
            //TestMostViewed_*/TestPageviewCount_* golden files (StatisticsRepositoryTests' own remarks), so this
            //list is never empty at baseline, and reading it here (unlike incrementing/purging PageStatistics)
            //cannot itself affect any golden file.
            var topViewed = await pageRepo.GetTopViewedPagesInfo(topCount);
            Assert.NotEmpty(topViewed);
            Assert.True(topViewed.Count <= topCount);
            for (var i = 1; i < topViewed.Count; i++)
            {
                Assert.True(topViewed[i - 1].TotalViewCount >= topViewed[i].TotalViewCount);
            }

            var topEdited = await pageRepo.GetTopEditedPagesInfo(topCount);
            Assert.NotEmpty(topEdited);
            Assert.True(topEdited.Count <= topCount);
            for (var i = 1; i < topEdited.Count; i++)
            {
                Assert.True(topEdited[i - 1].Revision >= topEdited[i].Revision);
            }
        }

        [Fact]
        public async Task GetTopRecentlyModifiedPagesInfoByUserId_ReturnsPagesModifiedByAdmin_EmptyForUnknownUser()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var usersRepo = fixture.Artifacts.DatabaseManager.UsersRepository;

            var admin = await usersRepo.GetAccountProfileByNavigation(TwNavigation.Clean(Constants.DEFAULTACCOUNT))
                ?? throw new Exception($"Could not find the seeded '{Constants.DEFAULTACCOUNT}' account profile.");

            var modifiedByAdmin = await pageRepo.GetTopRecentlyModifiedPagesInfoByUserId(admin.UserId, 100);
            Assert.NotEmpty(modifiedByAdmin);
            Assert.All(modifiedByAdmin, p => Assert.Equal(admin.UserId, p.ModifiedByUserId));
            for (var i = 1; i < modifiedByAdmin.Count; i++)
            {
                Assert.True(modifiedByAdmin[i - 1].ModifiedDate >= modifiedByAdmin[i].ModifiedDate);
            }

            //A user id that can never have modified anything returns an empty list, not throw.
            Assert.Empty(await pageRepo.GetTopRecentlyModifiedPagesInfoByUserId(Guid.NewGuid(), 100));
        }

        /// <summary>
        /// Round-trips <see cref="ITwPageRepository.InsertPageComment"/>/<see
        /// cref="ITwPageRepository.GetTotalPageCommentCount"/>/<see cref="ITwPageRepository.GetPageCommentsPaged"/>/
        /// <see cref="ITwPageRepository.DeletePageCommentById"/>/<see
        /// cref="ITwPageRepository.DeletePageCommentByUserAndId"/> against the seeded page, doubling as the
        /// scenario exercising <see cref="ITwPageRepository.FlushPageCache"/>: <see
        /// cref="ITwPageRepository.GetPageCommentsPaged"/> is cached under a key that starts with the page's own
        /// navigation (same cache-key shape <see cref="ITwPageRepository.FlushPageCache"/> clears), so the
        /// "prime the cache, mutate via a call that internally flushes it, read again" sequence below fails if
        /// <see cref="ITwPageRepository.FlushPageCache"/> stops actually clearing that cache entry.
        /// </summary>
        [Fact]
        public async Task InsertPageComment_FlushesCacheForGetPageCommentsPaged_GetTotalPageCommentCount_DeletePageCommentById_DeletePageCommentByUserAndId_RoundTrip()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var usersRepo = fixture.Artifacts.DatabaseManager.UsersRepository;

            var page = await GetSeededTestPageAsync(pageRepo);
            var admin = await usersRepo.GetAccountProfileByNavigation(TwNavigation.Clean(Constants.DEFAULTACCOUNT))
                ?? throw new Exception($"Could not find the seeded '{Constants.DEFAULTACCOUNT}' account profile.");

            var countBefore = await pageRepo.GetTotalPageCommentCount(page.Id);

            //Primes the GetPageCommentsPaged cache entry for this page before any comment has been inserted.
            _ = await pageRepo.GetPageCommentsPaged(page.Navigation, 1);

            var body1 = $"TestComment1_{Guid.NewGuid():N}";
            var body2 = $"TestComment2_{Guid.NewGuid():N}";

            //InsertPageComment flushes the page's cache internally (EfPageRepository.InsertPageComment's own doc
            //comment) - the read below must therefore reflect both new comments, not the primed, stale list.
            await pageRepo.InsertPageComment(page.Id, admin.UserId, body1);
            await pageRepo.InsertPageComment(page.Id, admin.UserId, body2);

            var comment1Id = 0;
            var comment2Id = 0;

            try
            {
                Assert.Equal(countBefore + 2, await pageRepo.GetTotalPageCommentCount(page.Id));

                //Ordered by CreatedDate descending, so both new comments (the newest on the page) always land on
                //page 1 regardless of how many older comments this page already has.
                var commentsAfterInsert = await pageRepo.GetPageCommentsPaged(page.Navigation, 1);
                var comment1 = Assert.Single(commentsAfterInsert, c => c.Body == body1);
                var comment2 = Assert.Single(commentsAfterInsert, c => c.Body == body2);
                Assert.Equal(admin.UserId, comment1.UserId);
                Assert.Equal(page.Id, comment1.PageId);
                comment1Id = comment1.Id;
                comment2Id = comment2.Id;

                //DeletePageCommentByUserAndId only deletes when the given user actually authored the comment.
                await pageRepo.DeletePageCommentByUserAndId(page.Id, Guid.NewGuid(), comment2Id);
                Assert.Equal(countBefore + 2, await pageRepo.GetTotalPageCommentCount(page.Id));

                await pageRepo.DeletePageCommentByUserAndId(page.Id, admin.UserId, comment2Id);
                Assert.Equal(countBefore + 1, await pageRepo.GetTotalPageCommentCount(page.Id));

                //DeletePageCommentById deletes regardless of author.
                await pageRepo.DeletePageCommentById(page.Id, comment1Id);
                Assert.Equal(countBefore, await pageRepo.GetTotalPageCommentCount(page.Id));
            }
            finally
            {
                //Best-effort cleanup in case an assertion above failed mid-sequence - deleting an already-deleted
                //comment id is a harmless no-op (EfPageRepository.DeletePageCommentById's own ExecuteDeleteAsync
                //affects zero rows when there is no match), so this is always safe to call unconditionally.
                if (comment1Id != 0)
                {
                    await pageRepo.DeletePageCommentById(page.Id, comment1Id);
                }
                if (comment2Id != 0)
                {
                    await pageRepo.DeletePageCommentById(page.Id, comment2Id);
                }
            }
        }

        [Fact]
        public async Task UpsertCurrentPageEditor_GetCurrentPageEditors_DeleteCurrentPageEditor_RoundTrip()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var usersRepo = fixture.Artifacts.DatabaseManager.UsersRepository;

            var page = await GetSeededTestPageAsync(pageRepo);
            var admin = await usersRepo.GetAccountProfileByNavigation(TwNavigation.Clean(Constants.DEFAULTACCOUNT))
                ?? throw new Exception($"Could not find the seeded '{Constants.DEFAULTACCOUNT}' account profile.");

            var accountName = $"TestEditor_{Guid.NewGuid():N}";
            var accountNameUpdated = $"TestEditorUpdated_{Guid.NewGuid():N}";

            try
            {
                await pageRepo.UpsertCurrentPageEditor(page.Id, admin.UserId, accountName);
                var editorsAfterInsert = await pageRepo.GetCurrentPageEditors(page.Id, windowMinutes: 5);
                Assert.Contains(accountName, editorsAfterInsert);

                //Upserting again for the same (pageId, userId) exercises the "row already exists" update path,
                //not just the insert path - EfPageRepository.UpsertCurrentPageEditor's own doc comment.
                await pageRepo.UpsertCurrentPageEditor(page.Id, admin.UserId, accountNameUpdated);
                var editorsAfterUpdate = await pageRepo.GetCurrentPageEditors(page.Id, windowMinutes: 5);
                Assert.Contains(accountNameUpdated, editorsAfterUpdate);
                Assert.DoesNotContain(accountName, editorsAfterUpdate);

                //A window that has already elapsed (a negative window, i.e. "only rows from the future") excludes
                //this just-written row.
                Assert.DoesNotContain(accountNameUpdated, await pageRepo.GetCurrentPageEditors(page.Id, windowMinutes: -1));
            }
            finally
            {
                await pageRepo.DeleteCurrentPageEditor(page.Id, admin.UserId);
            }

            var editorsAfterDelete = await pageRepo.GetCurrentPageEditors(page.Id, windowMinutes: 5);
            Assert.DoesNotContain(accountNameUpdated, editorsAfterDelete);
        }
    }
}
