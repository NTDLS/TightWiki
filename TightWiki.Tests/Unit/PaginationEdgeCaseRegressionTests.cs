using System.Text;
using TightWiki.Library;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;

namespace TightWiki.Tests.Unit
{
    /// <summary>
    /// Regression coverage for a provider-specific <see cref="DivideByZeroException"/> bug: every
    /// <c>EfPageRepository</c> paged-listing member that accepts a genuine caller-supplied <c>pageSize</c> used to
    /// compute <c>PaginationPageCount</c> in C# as <c>(totalCount + (pageSize - 1)) / pageSize</c> - integer
    /// division by zero whenever a caller passed <c>pageSize: 0</c>. That is not a hypothetical input: markup
    /// functions expose <c>pageSize</c> as a plain <c>int</c> parameter a page author can literally type as
    /// <c>0</c> - e.g. <c>##Revisions(Full, 0, false, "test")</c>, the exact seed markup behind
    /// <c>TightWiki.Tests/Markup/TestRevisions_000036.wiki</c> (and its <c>TestSearchList_000150</c>/
    /// <c>TestAttachments_000036</c> siblings for <c>##SearchList</c>/<c>##Attachments</c>) - and the SQLite
    /// reference implementation never throws for it: <c>LIMIT 0</c> in the reference <c>.sql</c> scripts always
    /// returns zero rows regardless of what the correlated <c>PaginationPageCount</c> subquery would itself have
    /// evaluated to (confirmed by inspection of <c>GetPageRevisionsInfoByNavigationPaged.sql</c>/
    /// <c>PageSearchPaged.sql</c>/<c>GetRelatedPagesPaged.sql</c>/
    /// <c>GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged.sql</c> - SQLite's own <c>X/0</c> evaluates to
    /// <c>NULL</c>, not an error, and that <c>NULL</c> is never actually observed by any caller since the very same
    /// query's <c>LIMIT 0</c> already discards every row carrying it before the value could reach Dapper). Once the
    /// exception itself no longer aborts the C# method before its own <c>Skip</c>/<c>Take</c> pipeline runs, that
    /// pipeline reproduces the identical "<c>Take(0)</c> always returns zero rows" behavior on its own - so the fix
    /// (a plain <c>pageSize == 0 ? 0 : ...</c> guard immediately around each ceiling-division expression in
    /// <c>EfPageRepository</c>) needs no new semantics of its own to match the SQLite reference.
    /// <para>
    /// Covers every <c>EfPageRepository</c> member whose <c>pageSize</c> is a genuine caller-supplied parameter,
    /// reachable from real markup - not merely the fixed "Pagination Size" customization setting, which no wiki
    /// page author can set to zero through any exposed input in this codebase (<c>EfPageRepository</c>'s other ten
    /// ceiling-division call sites received the identical defensive guard for consistency, per this task's own
    /// brief, but are not independently regression-tested here for that reason): <see
    /// cref="ITwPageRepository.GetPageRevisionsInfoByNavigationPaged"/> (<c>##Revisions</c>), <see
    /// cref="ITwPageRepository.PageSearchPaged"/> (<c>##SearchList</c>), <see
    /// cref="ITwPageRepository.GetSimilarPagesPaged"/> (<c>##Similar</c>), <see
    /// cref="ITwPageRepository.GetRelatedPagesPaged"/> (<c>##Related</c>), <see
    /// cref="ITwPageRepository.GetBacklinkPagesPaged"/> (<c>##Backlinks</c>), and <see
    /// cref="ITwPageRepository.GetPageFilesInfoByPageNavigationAndPageRevisionPaged"/> (<c>##Attachments</c>).
    /// </para>
    /// <para>
    /// Every scenario below first proves a non-empty baseline result exists for the exact same query at a real,
    /// small positive <c>pageSize</c> - guarding against a "<c>pageSize: 0</c> returns empty only because there was
    /// never anything to match anyway" false pass - then re-runs the identical query with <c>pageSize: 0</c> and
    /// asserts it returns an empty list without throwing. Obtained through <see cref="TwEngineFixture"/> exactly
    /// like every sibling repository test class in this project; written entirely against the provider-agnostic
    /// interface, so the same compiled test code runs against SQLite (default), SQL Server
    /// (<c>-p:DataProvider=SqlServer</c>), and Postgres (<c>-p:DataProvider=Postgres</c>).
    /// </para>
    /// <para>
    /// The first five scenarios are strictly read-only against the shared, persistent seeded database, reusing the
    /// same real seeded pages <see cref="PageRepositorySearchTests"/>/<see cref="PageRepositoryMetadataTests"/>
    /// already read from ("Sandbox :: Default", "Wiki Help :: Standard Function") for the same "stable, real seed
    /// content, never mutated by any of these tests" reasoning those classes' own remarks give.
    /// <see cref="GetPageFilesInfoByPageNavigationAndPageRevisionPaged_PageSizeZero_ReturnsEmptyList_NotThrows"/>
    /// instead creates and tears down its own GUID-named page with one real attachment in a <c>finally</c> block,
    /// mirroring <see cref="PageRepositoryCrudAttachmentTests"/>'s own "create/delete in finally" convention -
    /// no already-seeded page in this shared database is guaranteed to carry a file attachment.
    /// </para>
    /// </summary>
    [Collection("Pagination Edge Case Regression Tests")]
    public class PaginationEdgeCaseRegressionTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        /// <summary>
        /// The full name of the real, already-seeded page used for <see
        /// cref="GetPageRevisionsInfoByNavigationPaged_PageSizeZero_ReturnsEmptyList_NotThrows"/>/<see
        /// cref="PageSearchPaged_PageSizeZero_ReturnsEmptyList_NotThrows"/> - same page <see
        /// cref="PageRepositoryMetadataTests"/>/<see cref="PageRepositorySearchTests"/> already read from.
        /// </summary>
        private const string SeededRevisionsAndSearchPageName = "Sandbox :: Default";

        /// <summary>
        /// The full name of a real, already-seeded page referenced by dozens of other "Wiki Help" pages, used for
        /// <see cref="GetSimilarPagesPaged_PageSizeZero_ReturnsEmptyList_NotThrows"/>/<see
        /// cref="GetRelatedPagesPaged_GetBacklinkPagesPaged_PageSizeZero_ReturnsEmptyList_NotThrows"/> - same page
        /// <see cref="PageRepositorySearchTests"/>'s own <c>SeededHeavilyReferencedPageName</c> targets.
        /// </summary>
        private const string SeededHeavilyReferencedPageName = "Wiki Help :: Standard Function";

        private static async Task<TwPage> GetSeededPageAsync(ITwPageRepository pageRepo, string name)
        {
            var navigation = TwNamespaceNavigation.CleanAndValidate(name);
            return await pageRepo.GetPageInfoByNavigation(navigation)
                ?? throw new Exception($"Could not find the seeded page at navigation '{navigation}'.");
        }

        private static async Task<TwAccountProfile> GetAdminAsync(TwEngineFixture fixture)
        {
            var usersRepo = fixture.Artifacts.DatabaseManager.UsersRepository;
            return await usersRepo.GetAccountProfileByNavigation(TwNavigation.Clean(Constants.DEFAULTACCOUNT))
                ?? throw new Exception($"Could not find the seeded '{Constants.DEFAULTACCOUNT}' account profile.");
        }

        [Fact]
        public async Task GetPageRevisionsInfoByNavigationPaged_PageSizeZero_ReturnsEmptyList_NotThrows()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var navigation = TwNamespaceNavigation.CleanAndValidate(SeededRevisionsAndSearchPageName);

            //Baseline: every page has at least one revision (its own current one), so a real, positive pageSize
            //must return a non-empty result - proves pageSize: 0 below isn't "empty because nothing ever matched
            //anyway".
            var baseline = await pageRepo.GetPageRevisionsInfoByNavigationPaged(navigation, pageNumber: 1, pageSize: 1);
            Assert.NotEmpty(baseline);

            //Before the fix, this threw DivideByZeroException from (totalCount + (pageSize.Value - 1)) /
            //pageSize.Value; it must now return an empty list, matching the SQLite reference's own "LIMIT 0
            //always returns zero rows" behavior (GetPageRevisionsInfoByNavigationPaged.sql).
            var zeroPageSize = await pageRepo.GetPageRevisionsInfoByNavigationPaged(navigation, pageNumber: 1, pageSize: 0);
            Assert.Empty(zeroPageSize);
        }

        [Fact]
        public async Task PageSearchPaged_PageSizeZero_ReturnsEmptyList_NotThrows()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;

            //"sandbox" is a real token parsed from the seeded page's own Name (PageRepositorySearchTests' own
            //SeededSearchPageName remarks) - guaranteed to match at a real, positive pageSize.
            var baseline = await pageRepo.PageSearchPaged(["sandbox"], pageNumber: 1, pageSize: 1);
            Assert.NotEmpty(baseline);

            var zeroPageSize = await pageRepo.PageSearchPaged(["sandbox"], pageNumber: 1, pageSize: 0);
            Assert.Empty(zeroPageSize);
        }

        [Fact]
        public async Task GetSimilarPagesPaged_PageSizeZero_ReturnsEmptyList_NotThrows()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var page = await GetSeededPageAsync(pageRepo, SeededHeavilyReferencedPageName);

            //A page is always 100% similar to its own tags (GetSimilarPagesPaged.sql has no "P.Id <> @PageId"
            //filter - EfPageRepository.GetSimilarPagesPaged's own remarks) - guaranteed non-empty at a real,
            //positive pageSize.
            var baseline = await pageRepo.GetSimilarPagesPaged(page.Id, similarity: 100, pageNumber: 1, pageSize: 1);
            Assert.NotEmpty(baseline);

            var zeroPageSize = await pageRepo.GetSimilarPagesPaged(page.Id, similarity: 100, pageNumber: 1, pageSize: 0);
            Assert.Empty(zeroPageSize);
        }

        [Fact]
        public async Task GetRelatedPagesPaged_GetBacklinkPagesPaged_PageSizeZero_ReturnsEmptyList_NotThrows()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var page = await GetSeededPageAsync(pageRepo, SeededHeavilyReferencedPageName);

            //Confirmed by direct inspection of the shipped seed content: dozens of "Wiki Help :: ..." pages
            //reference this page's own navigation (PageRepositorySearchTests' own remarks) - guaranteed non-empty
            //at a real, positive pageSize for both members.
            var relatedBaseline = await pageRepo.GetRelatedPagesPaged(page.Id, pageNumber: 1, pageSize: 1);
            Assert.NotEmpty(relatedBaseline);
            Assert.Empty(await pageRepo.GetRelatedPagesPaged(page.Id, pageNumber: 1, pageSize: 0));

            var backlinksBaseline = await pageRepo.GetBacklinkPagesPaged(page.Id, pageNumber: 1, pageSize: 1);
            Assert.NotEmpty(backlinksBaseline);
            Assert.Empty(await pageRepo.GetBacklinkPagesPaged(page.Id, pageNumber: 1, pageSize: 0));
        }

        [Fact]
        public async Task GetPageFilesInfoByPageNavigationAndPageRevisionPaged_PageSizeZero_ReturnsEmptyList_NotThrows()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var admin = await GetAdminAsync(fixture);

            var pageName = $"ZzzPaginationZero_{Guid.NewGuid():N}";
            var navigation = TwNamespaceNavigation.CleanAndValidate(pageName);
            var page = new TwPage
            {
                Name = pageName,
                Navigation = navigation,
                Body = "Pagination edge-case host page.\r\n",
                Description = "PaginationEdgeCaseRegressionTests scratch page.",
                CreatedByUserId = admin.UserId,
                ModifiedByUserId = admin.UserId,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
            };
            page.Id = await pageRepo.UpsertPage(fixture.Artifacts.Engine, fixture.Artifacts.Localizer, page, fixture.CreateWikiSession());

            try
            {
                var fileName = "zero.bin";
                var fileNavigation = TwNavigation.Clean(fileName);
                var content = Encoding.UTF8.GetBytes($"payload-{Guid.NewGuid():N}");

                await pageRepo.UpsertPageFile(new TwPageFileAttachment
                {
                    PageId = page.Id,
                    Name = fileName,
                    FileNavigation = fileNavigation,
                    Data = content,
                    Size = content.Length,
                    ContentType = Utility.GetMimeType(fileName),
                    CreatedDate = DateTime.UtcNow,
                }, admin.UserId);

                //Baseline: the attachment just uploaded above must be present at a real, positive pageSize.
                var baseline = await pageRepo.GetPageFilesInfoByPageNavigationAndPageRevisionPaged(page.Navigation, pageNumber: 1, pageSize: 1);
                Assert.NotEmpty(baseline);

                //Before the fix, this threw DivideByZeroException from (totalCount + (pageSize.Value - 1)) /
                //pageSize.Value; it must now return an empty list, matching the SQLite reference's own "LIMIT 0
                //always returns zero rows" behavior (GetPageFilesInfoByPageNavigationAndPageRevisionPaged.sql).
                var zeroPageSize = await pageRepo.GetPageFilesInfoByPageNavigationAndPageRevisionPaged(page.Navigation, pageNumber: 1, pageSize: 0);
                Assert.Empty(zeroPageSize);
            }
            finally
            {
                await pageRepo.MovePageToDeletedById(page.Id, admin.UserId);
                await pageRepo.PurgeDeletedPageByPageId(page.Id);
            }

            Assert.Null(await pageRepo.GetPageRevisionInfoById(page.Id));
        }
    }
}
