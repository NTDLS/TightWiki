using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Tests.Unit
{
    /// <summary>
    /// Scenario-level integration tests for the third of four <see cref="ITwPageRepository"/> test tasks (86
    /// members total, per <c>EfPageRepository</c>'s own class-level remarks - far too many for one test file, same
    /// reasoning as <see cref="PageRepositoryMetadataTests"/>/<see cref="UsersRepositoryRoleTests"/>/<see
    /// cref="UsersRepositoryPermissionAuthTests"/>/<see cref="UsersRepositoryProfileTests"/> split their own
    /// interfaces into multiple files). Covers the 10 bulk/paged-listing members <c>EfPageRepository</c>'s own
    /// class-level remarks call out as landing in phase 2b.4: <see cref="ITwPageRepository.GetMissingPagesPaged"/>,
    /// <see cref="ITwPageRepository.GetAllPagesByInstructionPaged"/>, <see
    /// cref="ITwPageRepository.GetAllNamespacePagesPaged"/>, <see cref="ITwPageRepository.GetAllPagesPaged"/>, <see
    /// cref="ITwPageRepository.GetAllDeletedPagesPaged"/>, <see cref="ITwPageRepository.GetAllNamespacesPaged"/>,
    /// <see cref="ITwPageRepository.GetAllNamespaces"/>, <see cref="ITwPageRepository.GetAllPages"/>, <see
    /// cref="ITwPageRepository.GetAllTemplatePages"/>, and <see cref="ITwPageRepository.GetAllFeatureTemplates"/>.
    /// Single-page metadata reads, autocomplete, comments, and current-page-editors were covered by <see
    /// cref="PageRepositoryMetadataTests"/> (first task). Search/tags/tokens (<see
    /// cref="ITwPageRepository.PageSearch"/>/<see cref="ITwPageRepository.PageSearchPaged"/>/<see
    /// cref="ITwPageRepository.GetSimilarPagesPaged"/>/<see cref="ITwPageRepository.GetRelatedPagesPaged"/>/<see
    /// cref="ITwPageRepository.GetBacklinkPagesPaged"/>/<see cref="ITwPageRepository.GetDeletedPageIdsByTokens"/>/
    /// <see cref="ITwPageRepository.GetPageIdsByTokens"/>/<see cref="ITwPageRepository.GetSearchTokensByPageId"/>/
    /// <see cref="ITwPageRepository.SavePageSearchTokens"/>/<see cref="ITwPageRepository.ParsePageTokens"/>/<see
    /// cref="ITwPageRepository.GetAssociatedTags"/>/<see cref="ITwPageRepository.GetPageInfoByNamespaces"/>/<see
    /// cref="ITwPageRepository.GetPageInfoByTags"/>/<see cref="ITwPageRepository.GetPageInfoByTag"/>/<see
    /// cref="ITwPageRepository.UpdatePageTags"/>), CRUD/upsert, file attachments, and delete/restore are out of
    /// scope here - three further follow-up tasks. This class does not call any of those 15 search/tags/tokens
    /// members directly as a test subject; <see cref="GetAllPagesPaged_SearchTerms_UsesTempPageIdsContainsPattern"/>
    /// below does exercise <see cref="ITwPageRepository.GetAllPagesPaged"/>'s own <c>searchTerms</c> parameter
    /// (which necessarily calls <see cref="ITwPageRepository.GetPageIdsByTokens"/> internally to resolve it) -
    /// unavoidable, since that parameter is part of this task's own in-scope method signature, same as <see
    /// cref="PageRepositoryMetadataTests.InsertPageComment_FlushesCacheForGetPageCommentsPaged_GetTotalPageCommentCount_DeletePageCommentById_DeletePageCommentByUserAndId_RoundTrip"/>
    /// exercising <see cref="ITwPageRepository.FlushPageCache"/> only as a side effect of a different, in-scope
    /// method.
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
    /// Every test below is strictly read-only against the shared, persistent seeded database (chapter 5.3 - not a
    /// fresh-empty database, and not per-test transaction rollback) - none of the 10 members in scope for this
    /// task write anything, so there is no golden-file/cleanup concern analogous to <see
    /// cref="PageRepositoryMetadataTests"/>'s own remarks about "Sandbox :: Default". Expected counts are, wherever
    /// practical, derived live from another in-scope, unfiltered listing call (e.g. <see
    /// cref="ITwPageRepository.GetAllPages"/>) rather than hardcoded against today's exact seeded row counts -
    /// the same "generous lower bound"/self-consistency idiom <c>EmojiRepositoryTests</c>' own
    /// <c>AutoCompletePage_AutoCompleteNamespace_...</c>-style tests use for the same reason (this is a shared
    /// database other concurrently-running/future test classes may also read from, and the exact seeded page count
    /// is not a contract this task should bake in as a magic number). The one exception is <see
    /// cref="GetAllPagesPaged_SearchTerms_UsesTempPageIdsContainsPattern"/>'s use of the real, already-seeded
    /// "Sandbox :: Default" page (same page <see cref="PageRepositoryMetadataTests"/>/<c>StatisticsRepositoryTests</c>
    /// target) as a known-present member of the filtered result set - unavoidable, since the whole point of that
    /// test is to prove a *specific* real page is/isn't included after filtering, not just that the count is
    /// "reasonable".
    /// </para>
    /// </remarks>
    [Collection("Page Repository Listing Tests")]
    public class PageRepositoryListingTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        /// <summary>
        /// The full name (with namespace prefix) of the real, already-seeded page <see
        /// cref="GetAllPagesPaged_SearchTerms_UsesTempPageIdsContainsPattern"/> targets - same page <see
        /// cref="PageRepositoryMetadataTests"/>/<c>StatisticsRepositoryTests</c> use, for the same reasons (see
        /// those classes' own remarks).
        /// </summary>
        private const string SeededPageName = "Sandbox :: Default";

        /// <summary>
        /// A namespace with more seeded pages than the "Pagination Size" customization setting's default (20 as
        /// of this task, confirmed by inspection of the shipped seed content - but not hardcoded as a row count
        /// anywhere below), used to exercise <see cref="ITwPageRepository.GetAllNamespacePagesPaged"/>'s
        /// multi-page/last-page/beyond-last-page boundaries. A genuine, stable namespace name shipped with every
        /// provider's seed content (the built-in wiki documentation), not incidental test data - same reasoning as
        /// hardcoding <see cref="Constants.DEFAULTACCOUNT"/> elsewhere in this test project.
        /// </summary>
        private const string MultiPageNamespace = "Wiki Help";

        /// <summary>
        /// A namespace with very few seeded pages, used as the simple/single-page counterpart to <see
        /// cref="MultiPageNamespace"/>.
        /// </summary>
        private const string SmallNamespace = "Sandbox";

        [Fact]
        public async Task GetAllPagesPaged_DefaultAndExplicitOrdering_ReturnsConsistentPagination()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var configRepo = fixture.Artifacts.DatabaseManager.ConfigurationRepository;

            var paginationSize = await configRepo.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            //Default (null) orderBy - PaginationPageCount must be consistent with the real total active-page count
            //independently obtained via the unfiltered, unpaginated ITwPageRepository.GetAllPages().
            var allPages = await pageRepo.GetAllPages();
            Assert.NotEmpty(allPages);

            var page1Default = await pageRepo.GetAllPagesPaged(1);
            Assert.NotEmpty(page1Default);
            Assert.True(page1Default.Count <= paginationSize);
            var expectedPageCount = (allPages.Count + (paginationSize - 1)) / paginationSize;
            Assert.Equal(expectedPageCount, page1Default[0].PaginationPageCount);

            //If there is more than one page of results, the first page must be completely full.
            if (expectedPageCount > 1)
            {
                Assert.Equal(paginationSize, page1Default.Count);
            }

            //Explicit ordering on numeric/date columns (collation-independent - safe to assert monotonic order
            //identically across SQLite/SQL Server/Postgres, unlike string columns whose collation/case-sensitivity
            //rules differ per provider - UsersRepositoryProfileTests' own GetAllUsersPaged(orderBy: "Account", ...)
            //case deliberately only exercises the parameter for that same reason, never asserts string order).
            var byRevisionAsc = await pageRepo.GetAllPagesPaged(1, orderBy: "Revision", orderByDirection: "asc");
            for (var i = 1; i < byRevisionAsc.Count; i++)
            {
                Assert.True(byRevisionAsc[i - 1].Revision <= byRevisionAsc[i].Revision);
            }

            var byRevisionDesc = await pageRepo.GetAllPagesPaged(1, orderBy: "Revision", orderByDirection: "desc");
            for (var i = 1; i < byRevisionDesc.Count; i++)
            {
                Assert.True(byRevisionDesc[i - 1].Revision >= byRevisionDesc[i].Revision);
            }

            var byModifiedDateAsc = await pageRepo.GetAllPagesPaged(1, orderBy: "ModifiedDate", orderByDirection: "asc");
            for (var i = 1; i < byModifiedDateAsc.Count; i++)
            {
                Assert.True(byModifiedDateAsc[i - 1].ModifiedDate <= byModifiedDateAsc[i].ModifiedDate);
            }

            //"DeletedRevisions" is likewise a numeric column (TwPage.DeletedRevisionCount, a live COUNT(0) of
            //matching DeletedPageRevisions rows per EfPageRepository.GetAllPagesPaged's own remarks/implementation)
            //despite its string-typed orderBy key - collation-independent the same as Revision/ModifiedDate above,
            //so it gets the same strict monotonic assertion rather than merely "does the parameter work".
            var byDeletedRevisionsDesc = await pageRepo.GetAllPagesPaged(1, orderBy: "DeletedRevisions", orderByDirection: "desc");
            for (var i = 1; i < byDeletedRevisionsDesc.Count; i++)
            {
                Assert.True(byDeletedRevisionsDesc[i - 1].DeletedRevisionCount >= byDeletedRevisionsDesc[i].DeletedRevisionCount);
            }

            //"Name" is a real string column, so its collation/case-sensitivity rules differ per provider - but
            //Page.Name is confirmed unique across every seeded page (110/110 distinct, verified directly against
            //the shipped SQLite seed content), so the full asc sequence must be an exact reversal of the full desc
            //sequence regardless of which provider's collation produced the order - this proves orderByDirection
            //genuinely flips the sort without this test needing to know or assert what that order actually is.
            //Deliberately gathers *every* page of both directions (not just page 1 of each) and compares the full
            //concatenated Id sequences: with 110 seeded pages and a paginationSize of 20, the total doesn't divide
            //evenly (last page only has 10 rows), so page 1 ascending (the 20 smallest names) and page 1 descending
            //(the 20 largest names) cover disjoint, non-reversed slices of the full order - only the complete
            //sequences are guaranteed to be exact reversals of one another.
            var byNameAscAll = new List<TwPage>();
            var byNameDescAll = new List<TwPage>();
            for (var p = 1; p <= expectedPageCount; p++)
            {
                byNameAscAll.AddRange(await pageRepo.GetAllPagesPaged(p, orderBy: "Name", orderByDirection: "asc"));
                byNameDescAll.AddRange(await pageRepo.GetAllPagesPaged(p, orderBy: "Name", orderByDirection: "desc"));
            }
            Assert.NotEmpty(byNameAscAll);
            Assert.Equal(byNameAscAll.Count, byNameDescAll.Count);
            Assert.Equal(byNameAscAll.Select(p => p.Id), byNameDescAll.Select(p => p.Id).Reverse());

            //"ModifiedBy" is a cross-database lookup (Users.Profile.AccountName via ModifiedByUserId) and, unlike
            //Name, is NOT unique in the seeded content - every one of the 110 seeded pages was last modified by the
            //same single "Admin" account (verified directly against the shipped seed content), so an asc/desc
            //reversal check would be comparing 110 equal keys and prove nothing about orderByDirection. Left as the
            //weaker "does the parameter work without throwing" check for that reason.
            Assert.NotEmpty(await pageRepo.GetAllPagesPaged(1, orderBy: "ModifiedBy", orderByDirection: "asc"));

            //An unrecognized orderBy throws, matching RepositoryHelpers.TransposeOrderby's own "No order by
            //mapping..." exception - same convention as PageRepositoryMetadataTests'
            //GetPageRevisionsInfoByNavigationPaged_... case. ThrowsAnyAsync<Exception> (not the exact
            //InvalidOperationException EfPageRepository throws) because the SQLite reference's own
            //RepositoryHelpers.TransposeOrderby throws a plain System.Exception instead - a pre-existing,
            //documented divergence between providers (Database-Providers-Testing-Plan.md chapter 5.5).
            await Assert.ThrowsAnyAsync<Exception>(() => pageRepo.GetAllPagesPaged(1, orderBy: "NoSuchColumn"));
        }

        [Fact]
        public async Task GetAllPagesPaged_LastPage_And_BeyondLastPage_ReturnsBoundaryResults()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var configRepo = fixture.Artifacts.DatabaseManager.ConfigurationRepository;

            var paginationSize = await configRepo.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            var page1 = await pageRepo.GetAllPagesPaged(1);
            Assert.NotEmpty(page1);
            var totalPages = page1[0].PaginationPageCount;
            Assert.True(totalPages >= 1);

            //The last real page must be non-empty and never exceed the configured page size.
            var lastPage = await pageRepo.GetAllPagesPaged(totalPages);
            Assert.NotEmpty(lastPage);
            Assert.True(lastPage.Count <= paginationSize);
            Assert.Equal(totalPages, lastPage[0].PaginationPageCount);

            //One page past the last real page returns an empty list, not a throw.
            var beyondLastPage = await pageRepo.GetAllPagesPaged(totalPages + 1);
            Assert.Empty(beyondLastPage);
        }

        /// <summary>
        /// Exercises <see cref="ITwPageRepository.GetAllPagesPaged"/>'s <c>searchTerms</c> parameter - the
        /// provider-agnostic replacement for the SQLite reference's <c>TempPageIds</c> temp table (<see
        /// cref="ITwPageRepository.GetAllDeletedPagesPaged"/> shares the identical pattern, exercised in its own
        /// dedicated assertion below). Per <c>EfPageRepository.GetAllPagesPaged</c>'s own remarks (Database-Providers-Plan.md
        /// chapter 4.4/8), the reference resolves search terms to a list of matching page IDs and feeds that list
        /// into a second temp table so the SQL can do <c>WHERE P.Id IN (SELECT ... FROM TempPageIds)</c>; the EF
        /// Core replacement keeps that ID list as a plain in-memory <c>List&lt;int&gt;</c> and filters with
        /// <c>pageIds.Contains(p.Id)</c> directly in LINQ, which EF Core translates to a parameterized <c>WHERE
        /// p.Id IN (...)</c> - functionally equivalent for a "is this ID in the set" filter. This test proves that
        /// substitution actually narrows results (not silently ignored) and correctly produces an empty result
        /// when the resolved ID set is empty, on both the active-pages (<see
        /// cref="ITwPageRepository.GetAllPagesPaged"/>) and deleted-pages (<see
        /// cref="ITwPageRepository.GetAllDeletedPagesPaged"/>) variants.
        /// </summary>
        [Fact]
        public async Task GetAllPagesPaged_SearchTerms_UsesTempPageIdsContainsPattern()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;

            var navigation = TwNamespaceNavigation.CleanAndValidate(SeededPageName);
            var seededPage = await pageRepo.GetPageInfoByNavigation(navigation)
                ?? throw new Exception($"Could not find the seeded page at navigation '{navigation}'.");

            var allActivePages = await pageRepo.GetAllPages();
            Assert.NotEmpty(allActivePages);

            //"sandbox" is a real, non-empty search token derived from the seeded page's own Name (Sandbox ::
            //Default) - guaranteed to resolve to at least this one page's Id via the real TempPageIds/Contains
            //path, without needing to call any of the excluded-from-this-task search/tokens members directly.
            var filteredBySandbox = await pageRepo.GetAllPagesPaged(1, searchTerms: ["sandbox"]);
            Assert.NotEmpty(filteredBySandbox);
            Assert.Contains(filteredBySandbox, p => p.Id == seededPage.Id);
            //Proves the Contains(...) filter genuinely narrows the result set rather than being silently ignored -
            //if it were ignored, this would equal the full unfiltered active-page count instead.
            Assert.True(filteredBySandbox.Count < allActivePages.Count);

            //A search term that can never match any page's tokens resolves the TempPageIds replacement to an empty
            //id list - pageIds.Contains(p.Id) against an empty list must produce zero rows, not throw and not
            //silently fall back to the unfiltered set.
            var unmatchedTerm = $"no-such-token-{Guid.NewGuid():N}";
            Assert.Empty(await pageRepo.GetAllPagesPaged(1, searchTerms: [unmatchedTerm]));

            //Same Contains(...) substitution pattern, against the deleted-pages schema instead
            //(EfPageRepository.GetAllDeletedPagesPaged's own remarks call out the identical caveat). An unmatched
            //term must likewise resolve to zero rows regardless of how many (if any) deleted pages currently exist.
            Assert.Empty(await pageRepo.GetAllDeletedPagesPaged(1, searchTerms: [unmatchedTerm]));
        }

        [Fact]
        public async Task GetAllNamespacePagesPaged_FiltersByNamespace_BoundariesAndUnknownNamespace()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var configRepo = fixture.Artifacts.DatabaseManager.ConfigurationRepository;

            var paginationSize = await configRepo.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            //Expected counts are derived locally from the unfiltered ITwPageRepository.GetAllPages() (TwPage's own
            //get-only Namespace property parses it from Name) rather than hardcoded, so this stays correct even if
            //the seeded content set changes size in the future.
            var allPages = await pageRepo.GetAllPages();
            var smallNamespaceExpectedCount = allPages.Count(p => p.Namespace == SmallNamespace);
            Assert.True(smallNamespaceExpectedCount > 0, $"Expected at least one seeded page in namespace '{SmallNamespace}'.");

            var smallNamespacePage1 = await pageRepo.GetAllNamespacePagesPaged(1, SmallNamespace);
            Assert.Equal(Math.Min(smallNamespaceExpectedCount, paginationSize), smallNamespacePage1.Count);
            Assert.All(smallNamespacePage1, p => Assert.Equal(SmallNamespace, p.Namespace));

            var multiPageNamespaceExpectedCount = allPages.Count(p => p.Namespace == MultiPageNamespace);
            Assert.True(multiPageNamespaceExpectedCount > paginationSize,
                $"Expected namespace '{MultiPageNamespace}' to have more seeded pages than the pagination size to exercise multi-page boundaries.");

            var multiPageNamespacePage1 = await pageRepo.GetAllNamespacePagesPaged(1, MultiPageNamespace);
            Assert.Equal(paginationSize, multiPageNamespacePage1.Count);
            Assert.All(multiPageNamespacePage1, p => Assert.Equal(MultiPageNamespace, p.Namespace));
            var expectedPageCount = (multiPageNamespaceExpectedCount + (paginationSize - 1)) / paginationSize;
            Assert.Equal(expectedPageCount, multiPageNamespacePage1[0].PaginationPageCount);

            //Last real page - non-empty, never exceeding the configured page size.
            var lastPage = await pageRepo.GetAllNamespacePagesPaged(expectedPageCount, MultiPageNamespace);
            Assert.NotEmpty(lastPage);
            Assert.True(lastPage.Count <= paginationSize);
            Assert.All(lastPage, p => Assert.Equal(MultiPageNamespace, p.Namespace));

            //One page past the last real page returns an empty list, not a throw.
            Assert.Empty(await pageRepo.GetAllNamespacePagesPaged(expectedPageCount + 1, MultiPageNamespace));

            //Numeric/date orderings are safe to assert monotonic order across providers (see
            //GetAllPagesPaged_DefaultAndExplicitOrdering_ReturnsConsistentPagination's own remarks).
            var byRevisionDesc = await pageRepo.GetAllNamespacePagesPaged(1, MultiPageNamespace, orderBy: "Revision", orderByDirection: "desc");
            for (var i = 1; i < byRevisionDesc.Count; i++)
            {
                Assert.True(byRevisionDesc[i - 1].Revision >= byRevisionDesc[i].Revision);
            }

            //An unrecognized orderBy throws (same RepositoryHelpers.TransposeOrderby convention as every other
            //orderBy-accepting member in this interface).
            await Assert.ThrowsAnyAsync<Exception>(
                () => pageRepo.GetAllNamespacePagesPaged(1, MultiPageNamespace, orderBy: "NoSuchColumn"));

            //A namespace that can never have a real page returns an empty list, not throw.
            var unknownNamespace = $"no-such-namespace-{Guid.NewGuid():N}";
            Assert.Empty(await pageRepo.GetAllNamespacePagesPaged(1, unknownNamespace));
        }

        [Fact]
        public async Task GetAllDeletedPagesPaged_GetMissingPagesPaged_GetAllPagesByInstructionPaged_ReadOnlyChecks()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var configRepo = fixture.Artifacts.DatabaseManager.ConfigurationRepository;

            var paginationSize = await configRepo.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            //GetAllDeletedPagesPaged - read-only against whatever the shared database currently has (deliberately
            //not asserted empty/non-empty either way - creating/restoring deleted pages is CRUD/delete territory,
            //explicitly out of scope for this task's own brief). Only bounds/no-throw/ordering-parameter behavior
            //is checked here.
            var deletedPage1 = await pageRepo.GetAllDeletedPagesPaged(1);
            Assert.True(deletedPage1.Count <= paginationSize);
            var deletedPageOrdered = await pageRepo.GetAllDeletedPagesPaged(1, orderBy: "Page", orderByDirection: "asc");
            Assert.True(deletedPageOrdered.Count <= paginationSize);
            await Assert.ThrowsAnyAsync<Exception>(
                () => pageRepo.GetAllDeletedPagesPaged(1, orderBy: "NoSuchColumn"));

            //GetMissingPagesPaged - unlike GetAllDeletedPagesPaged above, this reflects a genuine broken reference
            //literally baked into the seeded "Wiki Help :: Links" page's own markup (confirmed by inspection of
            //the shipped seed content), not incidental/created-by-other-tests data, so it is safe to assert
            //non-empty here across all three providers, which all ship the same seed content.
            var missingPage1 = await pageRepo.GetMissingPagesPaged(1);
            Assert.NotEmpty(missingPage1);
            Assert.True(missingPage1.Count <= paginationSize);
            Assert.All(missingPage1, m =>
            {
                Assert.True(m.SourcePageId > 0);
                Assert.False(string.IsNullOrEmpty(m.SourcePageName));
                Assert.False(string.IsNullOrEmpty(m.TargetPageNavigation));
            });
            Assert.NotEmpty(await pageRepo.GetMissingPagesPaged(1, orderBy: "TargetPage", orderByDirection: "desc"));
            await Assert.ThrowsAnyAsync<Exception>(
                () => pageRepo.GetMissingPagesPaged(1, orderBy: "NoSuchColumn"));

            //GetAllPagesByInstructionPaged - a null instruction can never match PageProcessingInstruction.Instruction
            //(a NOT NULL column), the same "= NULL never matches" reasoning documented on
            //EfPageRepository.GetAllPagesByInstructionPaged - deterministic regardless of seed content.
            Assert.Empty(await pageRepo.GetAllPagesByInstructionPaged(1, instruction: null));

            //An instruction that can never be recorded on any page likewise matches nothing.
            var unknownInstruction = $"no-such-instruction-{Guid.NewGuid():N}";
            Assert.Empty(await pageRepo.GetAllPagesByInstructionPaged(1, instruction: unknownInstruction));

            //"protect" is a genuine, documented built-in wiki processing instruction (Wiki Help :: Protect
            //Instruction is itself one of the seeded help pages), not incidental test content - same reasoning as
            //hardcoding the "Templates" namespace literal already baked into ITwPageRepository.GetAllTemplatePages
            //itself.
            var protectedPages = await pageRepo.GetAllPagesByInstructionPaged(1, instruction: "protect");
            Assert.NotEmpty(protectedPages);
            Assert.True(protectedPages.Count <= paginationSize);
        }

        [Fact]
        public async Task GetAllNamespaces_GetAllNamespacesPaged_GetAllPages_GetAllTemplatePages_GetAllFeatureTemplates_ReturnConsistentData()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var configRepo = fixture.Artifacts.DatabaseManager.ConfigurationRepository;

            var paginationSize = await configRepo.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            //GetAllNamespaces - "Builtin" houses core system pages shipped with every provider's seed content, a
            //genuine stable namespace name (same reasoning as MultiPageNamespace/SmallNamespace above), not
            //hardcoded seed-row-count trivia.
            var namespaces = await pageRepo.GetAllNamespaces();
            Assert.NotEmpty(namespaces);
            Assert.Contains("Builtin", namespaces);

            //GetAllNamespacesPaged - PaginationPageCount is computed against the distinct namespace count (not the
            //total page count), so it's cross-checked against GetAllNamespaces()'s own count directly.
            var namespacesPaged = await pageRepo.GetAllNamespacesPaged(1);
            Assert.NotEmpty(namespacesPaged);
            var expectedNamespacePageCount = (namespaces.Count + (paginationSize - 1)) / paginationSize;
            Assert.Equal(expectedNamespacePageCount, namespacesPaged[0].PaginationPageCount);
            Assert.All(namespacesPaged, n => Assert.True(n.CountOfPages > 0));

            //"Pages" ordering (numeric - CountOfPages - safe to assert monotonic order across providers).
            var namespacesByCountDesc = await pageRepo.GetAllNamespacesPaged(1, orderBy: "Pages", orderByDirection: "desc");
            for (var i = 1; i < namespacesByCountDesc.Count; i++)
            {
                Assert.True(namespacesByCountDesc[i - 1].CountOfPages >= namespacesByCountDesc[i].CountOfPages);
            }

            await Assert.ThrowsAnyAsync<Exception>(
                () => pageRepo.GetAllNamespacesPaged(1, orderBy: "NoSuchColumn"));

            //GetAllPages - a generous lower bound, not an exact seeded-row-count match (same idiom as
            //EmojiRepositoryTests.GetAllEmojis' own assertion), since this is a shared, persistent database.
            //Every returned page includes revision Body content (unlike GetAllPagesPaged's projection).
            var allPages = await pageRepo.GetAllPages();
            Assert.True(allPages.Count >= 50, $"Expected at least 50 seeded pages, found {allPages.Count}.");
            Assert.All(allPages, p => Assert.False(string.IsNullOrEmpty(p.Body)));

            //GetAllTemplatePages - filtered to the literal "Templates" namespace; the seeded content set ships no
            //pages there at baseline, so this is empty rather than merely "a subset" - still verified generically
            //(Assert.All against an empty list passes trivially and remains correct even if a future seed content
            //change adds real Templates-namespace pages).
            var templatePages = await pageRepo.GetAllTemplatePages();
            Assert.All(templatePages, p => Assert.Equal("Templates", p.Namespace));

            //GetAllFeatureTemplates - cached, describes every built-in function/instruction; a generous lower
            //bound, not an exact count, for the same reason as GetAllPages above.
            var featureTemplates = await pageRepo.GetAllFeatureTemplates();
            Assert.True(featureTemplates.Count >= 30, $"Expected at least 30 feature templates, found {featureTemplates.Count}.");
            Assert.All(featureTemplates, ft => Assert.False(string.IsNullOrEmpty(ft.Name)));
        }
    }
}
