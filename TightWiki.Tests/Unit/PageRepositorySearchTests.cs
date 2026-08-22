using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;

namespace TightWiki.Tests.Unit
{
    /// <summary>
    /// Scenario-level integration tests for the third of four <see cref="ITwPageRepository"/> test tasks (86
    /// members total, per <c>EfPageRepository</c>'s own class-level remarks - far too many for one test file, same
    /// reasoning as <see cref="PageRepositoryMetadataTests"/>/<see cref="PageRepositoryListingTests"/>/<see
    /// cref="UsersRepositoryRoleTests"/>/<see cref="UsersRepositoryPermissionAuthTests"/>/<see
    /// cref="UsersRepositoryProfileTests"/> split their own interfaces into multiple files). Covers the 15
    /// search/tags/tokens members <see cref="PageRepositoryListingTests"/>'s own remarks call out as explicitly
    /// out of scope for that task: <see cref="ITwPageRepository.PageSearch"/>, <see
    /// cref="ITwPageRepository.PageSearchPaged"/>, <see cref="ITwPageRepository.GetSimilarPagesPaged"/>, <see
    /// cref="ITwPageRepository.GetRelatedPagesPaged"/>, <see cref="ITwPageRepository.GetBacklinkPagesPaged"/>,
    /// <see cref="ITwPageRepository.GetDeletedPageIdsByTokens"/>, <see cref="ITwPageRepository.GetPageIdsByTokens"/>,
    /// <see cref="ITwPageRepository.GetSearchTokensByPageId"/>, <see cref="ITwPageRepository.SavePageSearchTokens"/>,
    /// <see cref="ITwPageRepository.ParsePageTokens"/>, <see cref="ITwPageRepository.GetAssociatedTags"/>, <see
    /// cref="ITwPageRepository.GetPageInfoByNamespaces"/>, <see cref="ITwPageRepository.GetPageInfoByTags"/>, <see
    /// cref="ITwPageRepository.GetPageInfoByTag"/>, and <see cref="ITwPageRepository.UpdatePageTags"/>.
    /// Single-page metadata reads, autocomplete, comments, current-page-editors, and bulk/paged listings were
    /// covered by <see cref="PageRepositoryMetadataTests"/>/<see cref="PageRepositoryListingTests"/> (first two
    /// tasks). CRUD/upsert, file attachments, and delete/restore are out of scope here too - one further
    /// follow-up task.
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
    /// <b>Fuzzy search and non-determinism:</b> <see cref="ITwPageRepository.PageSearch"/>/<see
    /// cref="ITwPageRepository.PageSearchPaged"/> combine exact-token matching with phonetic (Double Metaphone)
    /// fuzzy matching by default (the shared seeded "Allow Fuzzy Matching" search setting is <c>true</c>,
    /// confirmed by direct inspection), and <see cref="ITwPageRepository.GetSimilarPagesPaged"/>'s own remarks
    /// note its reference SQL script has no deterministic <c>ORDER BY</c> at all. Every scenario below that
    /// touches these members therefore asserts presence in the result set (<c>Assert.Contains</c>) and
    /// relative/weak orderings (e.g. "matching two of the page's own tokens can only score at least as high as
    /// matching one"), never an exact score value or exact result ordering/position. The one "no match" case
    /// asserted as genuinely empty against <see cref="ITwPageRepository.PageSearchPaged"/> pins <c>allowFuzzyMatching:
    /// false</c> explicitly, sidestepping fuzzy non-determinism entirely for that one assertion rather than
    /// relying on a random token's Double Metaphone code never colliding with a real one by chance.
    /// </para>
    /// <para>
    /// <b>Why <see cref="SeededSearchPageName"/>/<see cref="SeededHeavilyReferencedPageName"/>/<see
    /// cref="SeededTokenTagRoundTripPageName"/> are three different real, already-seeded pages, never a page
    /// this class creates itself:</b> same reasoning as <see cref="PageRepositoryMetadataTests"/>/
    /// <c>StatisticsRepositoryTests</c>' own remarks - creating a page via <see
    /// cref="ITwPageRepository.UpsertPage"/> and tearing it down again is CRUD/delete territory, explicitly out
    /// of scope for this task's own brief. Three distinct pages (rather than reusing one throughout) are used
    /// specifically so the two round-tripping/mutating scenarios below (<see
    /// cref="GetSearchTokensByPageId_SavePageSearchTokens_RoundTrip"/>/<see
    /// cref="UpdatePageTags_RoundTrip_ReplacesTagsForPage_DedupsAndDropsEmpty"/>, both against <see
    /// cref="SeededTokenTagRoundTripPageName"/>) can never race against this class's own read-only assertions
    /// about a *different* page's stable token/tag content (<see cref="SeededSearchPageName"/>'s "sandbox"/
    /// "default" tokens and "Draft" tag, <see cref="SeededHeavilyReferencedPageName"/>'s tag/reference graph) -
    /// relevant because xunit runs different test classes (and therefore this class's own methods, which run
    /// sequentially with each other but concurrently with sibling classes like <see
    /// cref="PageRepositoryListingTests"/>) in parallel by default. Both mutating scenarios restore the
    /// original row set in a <c>finally</c> block for the same reason <see cref="PageRepositoryMetadataTests"/>'s
    /// own remarks give for "Sandbox :: Default".
    /// </para>
    /// <para>
    /// <see cref="ParsePageTokens_ComputesWeightedTokensFromTransformedMarkup"/> is the only scenario below that
    /// needs no seeded page and no cleanup at all: <see cref="TwEngineFixture.WikiTransform"/> (the same
    /// <c>ITwEngine.Transform</c> overload <see cref="MarkupTests"/> itself exercises directly) is a pure
    /// markup-to-<see cref="Plugin.Interfaces.ITwEngineState"/> transformation that never writes to the
    /// database - <see cref="ITwPageRepository.ParsePageTokens"/> only reads the resulting in-memory state.
    /// </para>
    /// </remarks>
    [Collection("Page Repository Search Tests")]
    public class PageRepositorySearchTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        /// <summary>
        /// The full name of the real, already-seeded page used for every <see cref="ITwPageRepository.PageSearch"/>/
        /// <see cref="ITwPageRepository.PageSearchPaged"/>/<see cref="ITwPageRepository.GetPageIdsByTokens"/>/<see
        /// cref="ITwPageRepository.GetAssociatedTags"/>/<see cref="ITwPageRepository.GetPageInfoByNamespaces"/>/
        /// <see cref="ITwPageRepository.GetPageInfoByTags"/>/<see cref="ITwPageRepository.GetPageInfoByTag"/>
        /// scenario below - same page <see cref="PageRepositoryMetadataTests"/>/<see
        /// cref="PageRepositoryListingTests"/>/<c>StatisticsRepositoryTests</c> read from (never mutated by any
        /// of them either), confirmed by direct inspection of the shipped seed content to carry the tokens
        /// "sandbox"/"default" (parsed from its own Name) and the single tag "Draft".
        /// </summary>
        private const string SeededSearchPageName = "Sandbox :: Default";

        /// <summary>
        /// The full name of a real, already-seeded page used for every <see
        /// cref="ITwPageRepository.GetSimilarPagesPaged"/>/<see cref="ITwPageRepository.GetRelatedPagesPaged"/>/
        /// <see cref="ITwPageRepository.GetBacklinkPagesPaged"/> scenario below - confirmed by direct inspection
        /// of the shipped seed content to be referenced by dozens of other "Wiki Help" pages, reference nothing
        /// itself, and carry the four tags Help/Official/Official-Help/Wiki that dozens of other "Wiki Help"
        /// pages also carry (all of it real, stable built-in wiki documentation content, not incidental/
        /// created-by-other-tests data).
        /// </summary>
        private const string SeededHeavilyReferencedPageName = "Wiki Help :: Standard Function";

        /// <summary>
        /// The full name of a real, already-seeded page used only by the two mutating round-trip scenarios
        /// below (<see cref="GetSearchTokensByPageId_SavePageSearchTokens_RoundTrip"/>/<see
        /// cref="UpdatePageTags_RoundTrip_ReplacesTagsForPage_DedupsAndDropsEmpty"/>) - deliberately a different
        /// page than <see cref="SeededSearchPageName"/>/<see cref="SeededHeavilyReferencedPageName"/> for the
        /// race-avoidance reasons given in this class's own remarks. Confirmed by direct inspection of the
        /// shipped seed content to carry over 50 Pages.PageToken rows and 5 tags, so restoring its original
        /// token/tag rows in each scenario's own <c>finally</c> block is never a no-op edge case (<see
        /// cref="ITwPageRepository.SavePageSearchTokens"/>'s own remarks: an empty items list is a no-op, unlike
        /// <see cref="ITwPageRepository.UpdatePageTags"/>'s unconditional delete).
        /// </summary>
        private const string SeededTokenTagRoundTripPageName = "Wiki Help :: Get";

        private static async Task<TwPage> GetSeededPageAsync(ITwPageRepository pageRepo, string name)
        {
            var navigation = TwNamespaceNavigation.CleanAndValidate(name);
            return await pageRepo.GetPageInfoByNavigation(navigation)
                ?? throw new Exception($"Could not find the seeded page at navigation '{navigation}'.");
        }

        [Fact]
        public async Task PageSearch_PageSearchPaged_MatchSeededSandboxPage_ByToken()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var seededPage = await GetSeededPageAsync(pageRepo, SeededSearchPageName);

            //"sandbox" and "default" are both real, non-empty tokens parsed from the seeded page's own Name -
            //confirmed present in Pages.PageToken by direct inspection of the shipped seed content - so an
            //exact-match search on either term is guaranteed to surface this page regardless of whatever
            //fuzzy-matching/scoring differences exist between providers (score only affects ordering, never
            //inclusion, for an exact token match). Search terms are lower-cased here the same way every real
            //caller does (Utility.SplitToTokens, PageController's own search actions) - Pages.PageToken itself
            //stores lower-invariant token text (EfPageRepository.ComputeParsedPageTokens's own remarks), so an
            //un-lowered search term could fail to match purely due to casing, unrelated to this test's intent.
            var bySandbox = await pageRepo.PageSearch(["sandbox"]);
            Assert.Contains(bySandbox, p => p.Id == seededPage.Id);

            var byBoth = await pageRepo.PageSearch(["sandbox", "default"]);
            Assert.Contains(byBoth, p => p.Id == seededPage.Id);

            //Matching both of the page's own distinctive tokens can only ever score at least as high as
            //matching one - a weak, provider-agnostic ordering guarantee that doesn't depend on the exact
            //scoring formula or on fuzzy-matching contributions.
            var singleTermScore = bySandbox.Single(p => p.Id == seededPage.Id).Score;
            var bothTermsScore = byBoth.Single(p => p.Id == seededPage.Id).Score;
            Assert.True(bothTermsScore >= singleTermScore);

            //PageSearchPaged wraps the same match set with rescaled (0-100) percentage scoring and pagination
            //info - the seeded page must still be present, and pagination metadata must be internally consistent.
            var paged = await pageRepo.PageSearchPaged(["sandbox"], pageNumber: 1);
            Assert.NotEmpty(paged);
            Assert.Contains(paged, p => p.Id == seededPage.Id);
            Assert.True(paged[0].PaginationPageCount >= 1);

            //A small explicit pageSize is honored.
            var pagedSmall = await pageRepo.PageSearchPaged(["sandbox"], pageNumber: 1, pageSize: 1);
            Assert.True(pagedSmall.Count <= 1);
        }

        [Fact]
        public async Task PageSearch_PageSearchPaged_EmptyForNoSearchTerms_AndPinnedExactUnmatchedTerm()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;

            //Deterministic short-circuit (searchTerms.Count == 0), unrelated to fuzzy matching.
            Assert.Empty(await pageRepo.PageSearch([]));
            Assert.Empty(await pageRepo.PageSearchPaged([], pageNumber: 1));

            //allowFuzzyMatching: false is pinned explicitly here (overriding the shared seeded "Allow Fuzzy
            //Matching" = true setting) so this assertion is genuinely deterministic - without it, a
            //sufficiently-unlucky Double Metaphone collision between this random term and some real seeded
            //token could theoretically produce a non-empty (if low-scoring) fuzzy match, which is exactly the
            //non-determinism this class's own remarks call out. Exact-only matching against a random guid-based
            //term can never match any real Pages.PageToken row.
            var unmatchedTerm = $"nosuchtoken{Guid.NewGuid():N}";
            Assert.Empty(await pageRepo.PageSearchPaged([unmatchedTerm], pageNumber: 1, allowFuzzyMatching: false));
        }

        [Fact]
        public async Task GetSimilarPagesPaged_IncludesSelf_ForFullyTaggedSeededPage()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var page = await GetSeededPageAsync(pageRepo, SeededHeavilyReferencedPageName);

            //GetSimilarPagesPaged.sql has no "P.Id <> @PageId" filter anywhere (a literal, preserved quirk
            //documented on EfPageRepository.GetSimilarPagesPaged's own remarks) - a page is always 100% similar
            //to its own tags, so similarity: 100 against the seeded page's own 4 tags (shared by dozens of
            //other "Wiki Help" pages, confirmed by direct inspection of the shipped seed content) must include
            //the page itself. pageSize is set generously above the real match count so every match lands on
            //page 1, avoiding any dependency on the (unspecified/non-deterministic across providers, per this
            //method's own remarks - "no explicit ORDER BY exists in the reference script itself") ordering of ties.
            var similar = await pageRepo.GetSimilarPagesPaged(page.Id, similarity: 100, pageNumber: 1, pageSize: 500);
            Assert.Contains(similar, p => p.Id == page.Id);
            //A generous lower bound, not hardcoded to today's exact seeded count.
            Assert.True(similar.Count > 10,
                $"Expected many pages to share all 4 tags with '{SeededHeavilyReferencedPageName}', found {similar.Count}.");
            Assert.Equal(1, similar[0].PaginationPageCount);

            //A similarity threshold above 100 can never be satisfied (the percentage is capped at 100 - matched
            //tag count can never exceed the root page's own total tag count), deterministic regardless of seed content.
            Assert.Empty(await pageRepo.GetSimilarPagesPaged(page.Id, similarity: 101, pageNumber: 1));
        }

        [Fact]
        public async Task GetRelatedPagesPaged_GetBacklinkPagesPaged_ExcludeSelf_ForHeavilyReferencedSeededPage()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var page = await GetSeededPageAsync(pageRepo, SeededHeavilyReferencedPageName);

            //Confirmed by direct inspection of the shipped seed content: dozens of "Wiki Help :: ..." pages
            //reference this page's own navigation in their markup (a real, stable cross-reference baked into
            //the built-in wiki documentation, not incidental/created-by-other-tests data - same reasoning as
            //PageRepositoryListingTests' own GetMissingPagesPaged reliance on "Wiki Help :: Links").
            var related = await pageRepo.GetRelatedPagesPaged(page.Id, pageNumber: 1, pageSize: 500);
            Assert.NotEmpty(related);
            Assert.DoesNotContain(related, p => p.Id == page.Id);
            Assert.Equal(1, related[0].PaginationPageCount);

            var backlinks = await pageRepo.GetBacklinkPagesPaged(page.Id, pageNumber: 1, pageSize: 500);
            Assert.NotEmpty(backlinks);
            Assert.DoesNotContain(backlinks, p => p.Id == page.Id);

            //GetBacklinkPagesPaged is the union of backlinks/outlinks/second-order-links
            //(GetBacklinkPagesPaged's own remarks); GetRelatedPagesPaged is only the first of those three
            //branches ("despite the method's name, this is the same 'who links here' relationship as
            //GetBacklinkPagesPaged's own first branch", per GetRelatedPagesPaged's own remarks) - so the
            //backlink set can never be smaller than the related set for the same page.
            Assert.True(backlinks.Count >= related.Count);
        }

        [Fact]
        public async Task GetSimilarPagesPaged_GetRelatedPagesPaged_GetBacklinkPagesPaged_UnknownPageOrBeyondLastPage_ReturnEmpty()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var page = await GetSeededPageAsync(pageRepo, SeededHeavilyReferencedPageName);

            //A page id that can never have a real row matches nothing in any of the three methods, not throw.
            var unknownPageId = int.MaxValue - 1;
            Assert.Empty(await pageRepo.GetSimilarPagesPaged(unknownPageId, similarity: 0, pageNumber: 1));
            Assert.Empty(await pageRepo.GetRelatedPagesPaged(unknownPageId, pageNumber: 1));
            Assert.Empty(await pageRepo.GetBacklinkPagesPaged(unknownPageId, pageNumber: 1));

            //One page past each method's own last real page returns an empty list, not a throw - same boundary
            //convention as every other paged member in this interface (PageRepositoryListingTests' own
            //GetAllPagesPaged_LastPage_And_BeyondLastPage_ReturnsBoundaryResults).
            var related = await pageRepo.GetRelatedPagesPaged(page.Id, pageNumber: 1, pageSize: 500);
            Assert.NotEmpty(related);
            Assert.Empty(await pageRepo.GetRelatedPagesPaged(page.Id, pageNumber: related[0].PaginationPageCount + 1, pageSize: 500));

            var backlinks = await pageRepo.GetBacklinkPagesPaged(page.Id, pageNumber: 1, pageSize: 500);
            Assert.NotEmpty(backlinks);
            Assert.Empty(await pageRepo.GetBacklinkPagesPaged(page.Id, pageNumber: backlinks[0].PaginationPageCount + 1, pageSize: 500));
        }

        [Fact]
        public async Task GetPageIdsByTokens_MatchesAllTokensSemantics()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var page = await GetSeededPageAsync(pageRepo, SeededSearchPageName);

            //"sandbox" and "default" are both real Pages.PageToken rows for the seeded page (parsed from its
            //own Name, confirmed by direct inspection of the shipped seed content) - a page must carry *every*
            //token in the list to match (an AND-style "does this page contain all of these tokens" match,
            //unlike PageSearch's score-based ranking - EfPageRepository.GetPageIdsByTokens's own remarks).
            var idsBothTokens = await pageRepo.GetPageIdsByTokens(["sandbox", "default"]);
            Assert.Contains(page.Id, idsBothTokens);

            //Pairing a real token with one that can never exist on any page means no page can satisfy "has
            //every token" - deterministic regardless of seed content, unlike a pure fuzzy-search assertion
            //(GetPageIdsByTokens has no fuzzy component at all).
            var unmatchedToken = $"no-such-token-{Guid.NewGuid():N}";
            Assert.Empty(await pageRepo.GetPageIdsByTokens(["sandbox", unmatchedToken]));

            //Null/empty input short-circuits to an empty list.
            Assert.Empty(await pageRepo.GetPageIdsByTokens(null));
            Assert.Empty(await pageRepo.GetPageIdsByTokens([]));

            //A documented quirk (EfPageRepository.GetPageIdsByTokens's own remarks): a null/empty entry
            //anywhere in the token list can never be satisfied by any real Pages.PageToken row, so the whole
            //call resolves to empty - even though "sandbox" alone would otherwise match real pages.
            Assert.Empty(await pageRepo.GetPageIdsByTokens(["sandbox", ""]));
        }

        [Fact]
        public async Task GetDeletedPageIdsByTokens_EmptyAgainstSharedSeededDatabase_AndNullEmptyInputs()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;

            //Read-only against whatever the shared database currently has - the seeded content ships with zero
            //deleted pages (confirmed by direct inspection), so this is empty rather than merely "a subset",
            //same reasoning as PageRepositoryListingTests' own GetAllDeletedPagesPaged_... case deliberately not
            //asserting non-empty either way. Creating/deleting a page to populate DeletedPages.PageToken is
            //CRUD/delete territory, explicitly out of scope for this task.
            Assert.Empty(await pageRepo.GetDeletedPageIdsByTokens(["sandbox", "default"]));
            Assert.Empty(await pageRepo.GetDeletedPageIdsByTokens(null));
            Assert.Empty(await pageRepo.GetDeletedPageIdsByTokens([]));
        }

        [Fact]
        public async Task GetSearchTokensByPageId_SavePageSearchTokens_RoundTrip()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var page = await GetSeededPageAsync(pageRepo, SeededTokenTagRoundTripPageName);

            var originalTokens = await pageRepo.GetSearchTokensByPageId(page.Id);
            //Required so the finally block's restore call is guaranteed to actually delete the temporary rows
            //this test inserts below, not silently leave them behind (SavePageSearchTokens treats an empty
            //items list as a no-op, per its own remarks - see this class's own SeededTokenTagRoundTripPageName remarks).
            Assert.NotEmpty(originalTokens);

            var testTokens = new List<TwPageToken>
            {
                new() { PageId = page.Id, Token = "ztesttokenalpha", DoubleMetaphone = "ZTST-ALPHA", Weight = 1.5 },
                new() { PageId = page.Id, Token = "ztesttokenbeta", DoubleMetaphone = "ZTST-BETA", Weight = 2.5 },
            };

            try
            {
                await pageRepo.SavePageSearchTokens(testTokens);

                var afterSave = await pageRepo.GetSearchTokensByPageId(page.Id);
                //Delete-then-insert per affected page (SavePageSearchTokens's own remarks) - the page's tokens
                //are replaced wholesale, not merged, so only the two test tokens remain.
                Assert.Equal(2, afterSave.Count);
                var alpha = Assert.Single(afterSave, t => t.Token == "ztesttokenalpha");
                Assert.Equal("ZTST-ALPHA", alpha.DoubleMetaphone);
                Assert.Equal(1.5, alpha.Weight, 5);
                var beta = Assert.Single(afterSave, t => t.Token == "ztesttokenbeta");
                Assert.Equal(2.5, beta.Weight, 5);
            }
            finally
            {
                await pageRepo.SavePageSearchTokens(originalTokens);
            }

            var restored = await pageRepo.GetSearchTokensByPageId(page.Id);
            Assert.Equal(originalTokens.Count, restored.Count);
            Assert.DoesNotContain(restored, t => t.Token == "ztesttokenalpha");

            //An empty items list is a no-op (SavePageSearchTokens's own remarks) - proven here by calling it
            //after the restore above and confirming nothing changed, rather than e.g. wiping the page's tokens.
            await pageRepo.SavePageSearchTokens([]);
            Assert.Equal(originalTokens.Count, (await pageRepo.GetSearchTokensByPageId(page.Id)).Count);

            //A page id that can never have a real row returns an empty collection, not throw.
            Assert.Empty(await pageRepo.GetSearchTokensByPageId(int.MaxValue - 1));
        }

        [Fact]
        public async Task GetAssociatedTags_ReturnsCoOccurringTags_ForSeededDraftTag()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var page = await GetSeededPageAsync(pageRepo, SeededSearchPageName);

            //"draft" (the cleaned Navigation, not necessarily the literal "Draft" spelling - GetAssociatedTags
            //matches against Pages.PageTag.Navigation, EfPageRepository.GetAssociatedTags's own remarks) is a
            //real tag applied to the seeded page itself plus at least one other seeded page (confirmed by
            //direct inspection of the shipped seed content) - a genuine "tags that co-occur with this tag"
            //result, not incidental test data.
            var associated = await pageRepo.GetAssociatedTags("draft");
            Assert.NotEmpty(associated);
            var draftGroup = Assert.Single(associated, t => t.Tag == "Draft");
            Assert.True(draftGroup.PageCount >= 2,
                $"Expected 'Draft' to be applied to at least 2 seeded pages, found {draftGroup.PageCount}.");

            //A tag navigation that can never be applied to any page returns an empty list, not throw.
            Assert.Empty(await pageRepo.GetAssociatedTags($"no-such-tag-{Guid.NewGuid():N}"));

            //Sanity: the page this test otherwise targets does carry the "draft" tag - ties this assertion back
            //to a real, independently-verifiable page rather than trusting the co-occurrence count alone.
            Assert.Contains(await pageRepo.GetPageTagsById(page.Id), t => t.Tag == "Draft");
        }

        [Fact]
        public async Task GetPageInfoByNamespaces_GetPageInfoByTags_GetPageInfoByTag_ReturnMatchingPages()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var page = await GetSeededPageAsync(pageRepo, SeededSearchPageName);

            var byNamespace = await pageRepo.GetPageInfoByNamespaces([page.Namespace]);
            Assert.Contains(byNamespace, p => p.Id == page.Id);
            Assert.All(byNamespace, p => Assert.Equal(page.Namespace, p.Namespace));

            Assert.Empty(await pageRepo.GetPageInfoByNamespaces([$"no-such-namespace-{Guid.NewGuid():N}"]));

            //GetPageInfoByTag delegates to GetPageInfoByTags with a single-element list
            //(EfPageRepository.GetPageInfoByTag's own remarks) - both must resolve to the exact same page set
            //for the same tag, and both clean the raw literal spelling ("Draft", not "draft") the same way internally.
            var byTag = await pageRepo.GetPageInfoByTag("Draft");
            var byTags = await pageRepo.GetPageInfoByTags(["Draft"]);
            Assert.Equal(byTag.Select(p => p.Id).OrderBy(id => id), byTags.Select(p => p.Id).OrderBy(id => id));
            Assert.Contains(byTag, p => p.Id == page.Id);

            //A tag shared by dozens of pages (confirmed by direct inspection of the shipped seed content) still
            //resolves correctly through the same two-step Contains(...) pattern.
            var byOfficialTag = await pageRepo.GetPageInfoByTags(["official"]);
            Assert.True(byOfficialTag.Count > 50, $"Expected 'official' to be applied to many seeded pages, found {byOfficialTag.Count}.");

            Assert.Empty(await pageRepo.GetPageInfoByTag($"no-such-tag-{Guid.NewGuid():N}"));
        }

        [Fact]
        public async Task UpdatePageTags_RoundTrip_ReplacesTagsForPage_DedupsAndDropsEmpty()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var page = await GetSeededPageAsync(pageRepo, SeededTokenTagRoundTripPageName);

            var originalTags = await pageRepo.GetPageTagsById(page.Id);
            Assert.NotEmpty(originalTags);

            var dupBase = $"DupTag{Guid.NewGuid():N}";
            var secondTag = $"SecondTag{Guid.NewGuid():N}";

            try
            {
                //TwNavigation.Clean lower-invariants everything (TwNavigation.Clean's own implementation), so
                //dupBase and dupBase.ToUpperInvariant() clean to the identical Navigation - UpdatePageTags's own
                //DistinctBy(Navigation) must keep only the first literal spelling (dupBase), and its own
                //"Coalesce(Tag,'')<>''" filter must drop the literal empty-string entry entirely.
                await pageRepo.UpdatePageTags(page.Id, [dupBase, dupBase.ToUpperInvariant(), secondTag, ""]);

                //UpdatePageTags itself never clears GetPageTagsById's own MemCache.Category.Page cache entry -
                //confirmed against both the EF and SQLite reference implementations, neither calls
                //MemCache.ClearCategory anywhere in UpdatePageTags itself. Only the higher-level
                //RefreshPageMetadata orchestration (UpsertPage's own caller, CRUD/upsert territory explicitly
                //out of scope for this task) clears it, after calling UpdatePageTags. Since this scenario reads
                //GetPageTagsById *before* writing (warming the cache with the original tags) and calls
                //UpdatePageTags directly rather than through RefreshPageMetadata, it must flush the cache itself
                //via the in-scope FlushPageCache - matching the effect the interface's own orchestration would
                //normally provide - or every read below would silently observe the stale, pre-update cached list.
                await pageRepo.FlushPageCache(page.Id);

                var afterUpdate = await pageRepo.GetPageTagsById(page.Id);
                Assert.Equal(2, afterUpdate.Count);
                Assert.Contains(afterUpdate, t => t.Tag == dupBase);
                Assert.DoesNotContain(afterUpdate, t => t.Tag == dupBase.ToUpperInvariant());
                Assert.Contains(afterUpdate, t => t.Tag == secondTag);

                //GetPageInfoByTag keys off the same cleaned Navigation UpdatePageTags itself wrote, proving the
                //write is visible through the read side of this same task's own in-scope members. Unlike
                //GetPageTagsById, GetPageInfoByTags/GetPageInfoByTag are not cached at all, so no flush is needed here.
                Assert.Contains(await pageRepo.GetPageInfoByTag(dupBase), p => p.Id == page.Id);
            }
            finally
            {
                //An empty (post-filtering) tags list still unconditionally deletes the page's existing tags
                //(UpdatePageTags's own remarks) - restoring the *original* literal tag text below is therefore
                //always safe/complete, even if originalTags itself happened to be empty (not the case for this
                //seeded page - see this class's own SeededTokenTagRoundTripPageName remarks).
                await pageRepo.UpdatePageTags(page.Id, originalTags.Select(t => t.Tag).ToList());
                //Same stale-cache reasoning as above - required so the assertion below (and any later test in
                //this shared, persistent database) observes the real, just-restored tag set.
                await pageRepo.FlushPageCache(page.Id);
            }

            var restored = await pageRepo.GetPageTagsById(page.Id);
            Assert.Equal(originalTags.Select(t => t.Tag).OrderBy(t => t), restored.Select(t => t.Tag).OrderBy(t => t));
        }

        [Fact]
        public async Task ParsePageTokens_ComputesWeightedTokensFromTransformedMarkup()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;

            //fixture.WikiTransform never persists anything to the database (TightWiki.Engine's own Transform is
            //a pure markup -> ITwEngineState transformation, the same one MarkupTests itself exercises directly)
            //- this scenario needs no seeded page and no cleanup, unlike every other scenario in this class.
            var state = await fixture.WikiTransform("ZzzTokenTestPage", "Elephant Elephant Giraffe");

            var tokens = await pageRepo.ParsePageTokens(state);

            //"elephant"/"giraffe" are nonsense test words that cannot collide with the real shared seeded "Word
            //Exclusions" search setting (do,of,it,i,is,or,and,but,the,a,for,also,be,as,that,this,to,on,are,if,in
            //- confirmed by direct inspection - none of which are equal to either word) nor with anything the
            //page's own Name ("ZzzTokenTestPage", possibly also split into zzz/token/test/page by the shared
            //seeded "Split Camel Case" setting) contributes - ParsePageTokens aggregates by literal token text,
            //so distinct spellings simply add separate entries. Only ITwEngineState.HtmlResult (weight
            //multiplier 1) contributes "elephant"/"giraffe" here - Description/Tags are both empty for a bare
            //WikiTransform call, and ComputeParsedPageTokens drops whitespace-only tokens, so their weight
            //multipliers (1.2/1.4) never apply.
            var elephant = Assert.Single(tokens, t => t.Token == "elephant");
            Assert.Equal(2.0, elephant.Weight, 5);
            Assert.False(string.IsNullOrEmpty(elephant.DoubleMetaphone));

            var giraffe = Assert.Single(tokens, t => t.Token == "giraffe");
            Assert.Equal(1.0, giraffe.Weight, 5);
        }
    }
}
