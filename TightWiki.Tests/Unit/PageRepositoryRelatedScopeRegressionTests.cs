using TightWiki.Library;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;

namespace TightWiki.Tests.Unit
{
    /// <summary>
    /// Regression test for a scope bug in <c>TightWiki.Data.EfCore.Repositories.EfPageRepository
    /// .GetRelatedPagesPaged</c>: before the fix, it only ever selected true backlinks
    /// (<c>pr.ReferencesPageId == pageId</c>, no outlink/second-order branch at all) - identical, narrower
    /// semantics to <c>GetBacklinkPagesPaged</c> - instead of mirroring its own SQLite reference script
    /// (<c>TightWiki.Repository/Scripts/GetRelatedPagesPaged.sql</c>), which is a three-way <c>UNION</c> of
    /// backlinks, outlinks, and second-order links. This made a page with a real outlink but zero real
    /// backlinks incorrectly report itself as having no related pages at all - visible as missing
    /// <c>Related()</c> function output and, on SqlServer/Postgres, as failures in the golden-file
    /// <c>Markup\TestRelated_*.wiki.expected</c> regression cases (run via <c>MarkupTests</c>) that assume the
    /// SQLite reference's wider, union-based semantics.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why a brand-new, GUID-named page rather than a real seeded one:</b> mirrors
    /// <see cref="PageRepositoryBacklinksScopeRegressionTests"/>'s own remarks - the whole point of this
    /// scenario is a page with a known, controlled reference graph - specifically one real outlink and zero
    /// real backlinks. No already-seeded page can safely be asserted to have zero incoming backlinks (any
    /// other seeded page could reference it), so this test creates its own page via
    /// <see cref="ITwPageRepository.UpsertPage"/> whose navigation is unique to this test run - nothing else in
    /// the shared, persistent test database can already reference it, and this test itself never creates any
    /// other page that would. Its single outlink targets the real, already-seeded "Sandbox :: Default" page -
    /// the same "only ever read from and linked *to*, never mutated" link target
    /// <see cref="PageRepositoryBacklinksScopeRegressionTests"/>'s own remarks establish is safe to gain one
    /// more incoming backlink from a test page that is itself deleted again by the end of the test.
    /// </para>
    /// <para>
    /// Joins <see cref="PageRepositoryCrudAttachmentTests"/>'s own "Page Repository Crud Attachment Tests"
    /// collection rather than defining a new one, for the same reason
    /// <see cref="PageRepositoryBacklinksScopeRegressionTests"/> does - this test mutates the shared database
    /// (creates and deletes its own page) exactly like that class's own scenarios, and
    /// <c>CollectionDefinitions</c>' own remarks explain why every mutating class needs
    /// <c>DisableParallelization = true</c> against the suite's nominally-read-only, whole-table-listing
    /// classes.
    /// </para>
    /// </remarks>
    [Collection("Page Repository Crud Attachment Tests")]
    public class PageRepositoryRelatedScopeRegressionTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        /// <summary>
        /// The full name of the real, already-seeded page used as this test's sole outlink target - see this
        /// class's own remarks for why it is safe to link to (never mutated, never asserted to have an exact
        /// backlink count elsewhere).
        /// </summary>
        private const string SeededLinkTargetPageName = "Sandbox :: Default";

        private static async Task<TwPage> CreateTestPageAsync(TwEngineFixture fixture, Guid createdByUserId, string pageName, string body)
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var navigation = TwNamespaceNavigation.CleanAndValidate(pageName);

            var page = new TwPage
            {
                Name = pageName,
                Navigation = navigation,
                Body = body,
                Description = "PageRepositoryRelatedScopeRegressionTests scratch page.",
                CreatedByUserId = createdByUserId,
                ModifiedByUserId = createdByUserId,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
            };

            page.Id = await pageRepo.UpsertPage(fixture.Artifacts.Engine, fixture.Artifacts.Localizer, page, fixture.CreateWikiSession());

            return page;
        }

        private static async Task DeleteTestPageAsync(ITwPageRepository pageRepo, int pageId, Guid deletedByUserId)
        {
            await pageRepo.MovePageToDeletedById(pageId, deletedByUserId);
            await pageRepo.PurgeDeletedPageByPageId(pageId);
        }

        private static async Task<TwAccountProfile> GetAdminAsync(TwEngineFixture fixture)
        {
            var usersRepo = fixture.Artifacts.DatabaseManager.UsersRepository;
            return await usersRepo.GetAccountProfileByNavigation(TwNavigation.Clean(Constants.DEFAULTACCOUNT))
                ?? throw new Exception($"Could not find the seeded '{Constants.DEFAULTACCOUNT}' account profile.");
        }

        [Fact]
        public async Task GetRelatedPagesPaged_PageWithOutlinkButNoBacklinks_ReturnsOutlinkTarget()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var admin = await GetAdminAsync(fixture);

            //[[...]] is the internal-link markup handler (MarkupHandlers.HandleInternalLinks) - RefreshPageMetadata
            //(run by UpsertPage) resolves it into a real Pages.PageReference row via UpdatePageReferences, giving
            //this brand-new page exactly one real outlink (to SeededLinkTargetPageName) and, by construction
            //(unique GUID-based navigation, see this class's own remarks), zero real backlinks.
            var body = $"See [[{SeededLinkTargetPageName}]] for more information.\r\n";
            var page = await CreateTestPageAsync(fixture, admin.UserId, $"ZzzRelatedScope_{Guid.NewGuid():N}", body);

            try
            {
                //The core regression assertion: GetRelatedPagesPaged must answer the broader "what is related to
                //this page" question (backlinks + outlinks + second-order links), unlike GetBacklinkPagesPaged's
                //narrower "who links TO this page" - pre-fix, the backlinks-only implementation would have
                //returned an empty result here even though this page has a real outlink; the fixed method
                //(mirroring GetRelatedPagesPaged.sql's three-way UNION) must surface SeededLinkTargetPageName as
                //a related page via its outlink branch.
                var related = await pageRepo.GetRelatedPagesPaged(page.Id, pageNumber: 1, pageSize: 500);
                Assert.Contains(related, p => p.Name == SeededLinkTargetPageName);
            }
            finally
            {
                await DeleteTestPageAsync(pageRepo, page.Id, admin.UserId);
            }

            //GetPageRevisionInfoById, not GetPageInfoByNavigation - same cache-invalidation-after-delete gap
            //PageRepositoryCrudAttachmentTests' own remarks document (navigation-keyed cache entries for a
            //just-deleted page can never be reliably cleared by FlushPageCache).
            Assert.Null(await pageRepo.GetPageRevisionInfoById(page.Id));
        }
    }
}
