using TightWiki.Library;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;

namespace TightWiki.Tests.Unit
{
    /// <summary>
    /// Scenario-level integration tests for the fifth and last of five <see cref="ITwPageRepository"/> test tasks
    /// (86 members total, per <c>EfPageRepository</c>'s own class-level remarks - far too many for one test file,
    /// same reasoning as <see cref="PageRepositoryMetadataTests"/>/<see cref="PageRepositoryListingTests"/>/<see
    /// cref="PageRepositorySearchTests"/>/<see cref="PageRepositoryCrudAttachmentTests"/>/<see
    /// cref="UsersRepositoryRoleTests"/>/<see cref="UsersRepositoryPermissionAuthTests"/>/<see
    /// cref="UsersRepositoryProfileTests"/> split their own interfaces into multiple files). Covers the delete/
    /// restore neighborhood of the interface (<see cref="ITwPageRepository.RestoreDeletedPageByPageId"/>, <see
    /// cref="ITwPageRepository.MovePageRevisionToDeletedById"/>, <see cref="ITwPageRepository.MovePageToDeletedById"/>,
    /// <see cref="ITwPageRepository.PurgeDeletedPageByPageId"/>, <see cref="ITwPageRepository.GetDeletedPageById"/>,
    /// <see cref="ITwPageRepository.GetLatestPageRevisionById"/>, <see cref="ITwPageRepository.GetPageNextRevision"/>,
    /// <see cref="ITwPageRepository.GetPagePreviousRevision"/>, <see cref="ITwPageRepository.GetDeletedPageRevisionsByIdPaged"/>,
    /// <see cref="ITwPageRepository.PurgeDeletedPageRevisionsByPageId"/>,
    /// <see cref="ITwPageRepository.PurgeDeletedPageRevisionByPageIdAndRevision"/>,
    /// <see cref="ITwPageRepository.RestoreDeletedPageRevisionByPageIdAndRevision"/>,
    /// <see cref="ITwPageRepository.GetDeletedPageRevisionById"/>, and <see cref="ITwPageRepository.GetAllDeletedPagesPaged"/>)
    /// plus <see cref="ITwPageRepository.GetCountOfPageAttachmentsById"/> as a light "page is really gone" touch,
    /// same as <see cref="PageRepositoryCrudAttachmentTests"/> already exercises it for. <c>EfPageRepository</c>'s
    /// own class-level remarks call this phase 2b.8, "transactional Pages/DeletedPages/DeletedPageRevisions
    /// moves" - the most structurally involved CRUD category after search. Written entirely against the
    /// provider-agnostic interface - no <c>#if SQLITE_PROVIDER</c>/<c>#elif SQLSERVER_PROVIDER</c>/
    /// <c>#elif POSTGRES_PROVIDER</c> branching here. The same compiled test code runs three times: <c>dotnet
    /// test</c> (SQLite, default), <c>dotnet test -p:DataProvider=SqlServer</c>, <c>dotnet test
    /// -p:DataProvider=Postgres</c> (Database-Providers-Testing-Plan.md chapter 5.5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every page this class touches is created (and permanently torn down) by the test itself, GUID-named:</b>
    /// mutating an already-seeded page's delete/restore state could break other, concurrently-running test
    /// classes against this same shared, persistent database, so - like <see cref="PageRepositoryCrudAttachmentTests"/> -
    /// every scenario below creates its own page(s) via <see cref="CreateTestPageAsync"/> and permanently removes
    /// them again via <see cref="DeleteTestPageAsync"/> in a <c>finally</c> block, regardless of what
    /// delete/restore/purge state the test body itself already left the page in.
    /// </para>
    /// <para>
    /// <b><see cref="DeleteTestPageAsync"/> cleans up all three schemas, not just Pages/DeletedPages:</b>
    /// <see cref="ITwPageRepository.PurgeDeletedPageByPageId"/> itself calls
    /// <see cref="ITwPageRepository.PurgeDeletedPageRevisionsByPageId"/> internally (matching the SQLite
    /// reference's own sequencing - see <c>EfPageRepository.PurgeDeletedPageByPageId</c>'s own remarks), so
    /// <see cref="ITwPageRepository.MovePageToDeletedById"/> + <see cref="ITwPageRepository.PurgeDeletedPageByPageId"/>
    /// is sufficient to guarantee a test page leaves no trace in Pages, DeletedPages, <i>or</i>
    /// DeletedPageRevisions - even when the test body itself already independently purged/restored individual
    /// revisions into a partial state (see e.g. the fourth and fifth scenarios below, which purge one revision
    /// directly and rely on this final cleanup call alone to remove whatever remains).
    /// <see cref="ITwPageRepository.MovePageToDeletedById"/> is also confirmed safe to call a second time against
    /// an already-fully-purged page (as the second scenario below does): it unconditionally inserts a
    /// DeletedPages.DeletionMeta row for the given pageId even when no matching Pages.Page row exists to move
    /// (matching the SQLite reference's own unconditional <c>INSERT</c>, not gated on a prior row actually being
    /// found) - but the very next <see cref="ITwPageRepository.PurgeDeletedPageByPageId"/> call in the same
    /// cleanup helper deletes that DeletionMeta row unconditionally by pageId too, so no phantom row survives
    /// past the end of the helper.
    /// </para>
    /// <para>
    /// <b>The same caching gotcha <see cref="PageRepositoryCrudAttachmentTests"/> already documented, avoided the
    /// same way:</b> <see cref="ITwPageRepository.FlushPageCache"/> - called at the end of every mutating member
    /// this class exercises - re-queries Pages.Page for the given pageId's own navigation <i>after</i> the row has
    /// already been deleted when the page itself (not just one revision) is being moved/purged, so the
    /// navigation-keyed cache entry for <see cref="ITwPageRepository.GetPageInfoByNavigation"/> can never
    /// reliably be invalidated this way. This class never calls
    /// <see cref="ITwPageRepository.GetPageInfoByNavigation"/> at all for a page it creates itself - using
    /// <see cref="ITwPageRepository.GetPageRevisionInfoById"/> instead (confirmed uncached on both the SQLite
    /// reference and <c>EfPageRepository</c>) plus pageId-keyed reads (<see cref="ITwPageRepository.GetPageRevisionById"/>,
    /// <see cref="ITwPageRepository.GetCurrentPageRevision"/>, <see cref="ITwPageRepository.GetPageRevisionCountByPageId"/>,
    /// etc. - all cached under a pageId-prefixed key that <c>FlushPageCache</c>'s own <c>ClearCategory</c>
    /// prefix-match reliably clears regardless of any per-revision/per-function suffix, per
    /// <c>MemCache.ClearCategory</c>'s own "removes cache entries that begin with the given cache key" contract)
    /// for every other assertion.
    /// </para>
    /// <para>
    /// <b>Global, unscoped purge-all members are deliberately never called here.</b>
    /// <see cref="ITwPageRepository.PurgeDeletedPages"/> and <see cref="ITwPageRepository.PurgeDeletedPageRevisions"/>
    /// (both taking no arguments, both permanently wiping every deleted page/revision across the entire shared,
    /// persistent test database) are reviewed by reading <c>EfPageRepository</c>'s own implementation only - never
    /// exercised live here, since doing so could destroy another, concurrently-running test class's own deleted
    /// page/revision fixtures. Every scenario below instead uses only the single-page/single-revision-scoped
    /// overloads (<see cref="ITwPageRepository.PurgeDeletedPageByPageId"/>,
    /// <see cref="ITwPageRepository.PurgeDeletedPageRevisionsByPageId"/>,
    /// <see cref="ITwPageRepository.PurgeDeletedPageRevisionByPageIdAndRevision"/>) - the same precedent
    /// <see cref="PageRepositoryCrudAttachmentTests"/>'s own remarks establish for
    /// <see cref="ITwPageRepository.PurgeOrphanedPageAttachments"/> vs. <see cref="ITwPageRepository.PurgeOrphanedPageAttachment"/>.
    /// </para>
    /// <para>
    /// <b><see cref="ITwPageRepository.GetDeletedPageRevisionsByIdPaged"/>, unlike its sibling paged listings, is
    /// already scoped to one page by its own <c>pageId</c> parameter</b> - not a global, unscoped listing across
    /// every page in the shared database the way <see cref="ITwPageRepository.GetMissingPagesPaged"/>/
    /// <see cref="ITwPageRepository.GetOrphanedPageAttachmentsPaged"/> are. <see cref="FindDeletedPageRevisionAsync"/>
    /// still loops every page of results defensively (bounded by the method's own
    /// <see cref="TwDeletedPageRevision.PaginationPageCount"/>) purely as a correctness safeguard against a small
    /// "Pagination Size" customization setting, not to avoid cross-test contamination the way the global listings'
    /// own finder helpers in <see cref="PageRepositoryCrudAttachmentTests"/> need to.
    /// <see cref="ITwPageRepository.GetAllDeletedPagesPaged"/> <i>is</i> a genuinely global, unscoped listing
    /// (every deleted page in the shared database, not just this class's own), so <see cref="FindDeletedPageAsync"/>
    /// loops it for the same "don't assume our own entry lands on page 1" reason
    /// <see cref="PageRepositoryCrudAttachmentTests"/>'s own finder helpers do.
    /// </para>
    /// </remarks>
    [Collection("Page Repository Delete Restore Tests")]
    public class PageRepositoryDeleteRestoreTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        /// <summary>
        /// Creates and saves a brand-new page via <see cref="ITwPageRepository.UpsertPage"/>, mirroring
        /// <see cref="PageRepositoryCrudAttachmentTests"/>'s own private helper of the same name/shape.
        /// </summary>
        private static async Task<TwPage> CreateTestPageAsync(TwEngineFixture fixture, Guid createdByUserId, string pageName, string body)
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var navigation = TwNamespaceNavigation.CleanAndValidate(pageName);

            var page = new TwPage
            {
                Name = pageName,
                Navigation = navigation,
                Body = body,
                Description = "PageRepositoryDeleteRestoreTests scratch page.",
                CreatedByUserId = createdByUserId,
                ModifiedByUserId = createdByUserId,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
            };

            page.Id = await pageRepo.UpsertPage(fixture.Artifacts.Engine, fixture.Artifacts.Localizer, page, fixture.CreateWikiSession());

            return page;
        }

        /// <summary>
        /// Appends a new revision to an already-saved <paramref name="page"/> by changing its
        /// <see cref="TwPage.Body"/> and calling <see cref="ITwPageRepository.UpsertPage"/> again - the changed
        /// CRC32 hash bumps the revision (<c>EfPageRepository</c>'s own <c>SavePage</c> helper), same
        /// hash-based-change-detection mechanism <see cref="PageRepositoryCrudAttachmentTests"/>'s own
        /// "existing page" scenario exercises directly.
        /// </summary>
        private static async Task AddPageRevisionAsync(TwEngineFixture fixture, TwPage page, string newBody)
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            page.Body = newBody;
            page.Id = await pageRepo.UpsertPage(fixture.Artifacts.Engine, fixture.Artifacts.Localizer, page, fixture.CreateWikiSession());
        }

        private static async Task<TwAccountProfile> GetAdminAsync(TwEngineFixture fixture)
        {
            var usersRepo = fixture.Artifacts.DatabaseManager.UsersRepository;
            return await usersRepo.GetAccountProfileByNavigation(TwNavigation.Clean(Constants.DEFAULTACCOUNT))
                ?? throw new Exception($"Could not find the seeded '{Constants.DEFAULTACCOUNT}' account profile.");
        }

        /// <summary>
        /// Permanently removes a test page - and, per this class's own remarks, any trace it may have left behind
        /// in DeletedPages/DeletedPageRevisions too, regardless of what partial delete/restore/purge state the
        /// test body left it in - via <see cref="ITwPageRepository.MovePageToDeletedById"/> +
        /// <see cref="ITwPageRepository.PurgeDeletedPageByPageId"/>. Deliberately does not itself assert
        /// anything - every caller asserts the page is gone via <see cref="ITwPageRepository.GetPageRevisionInfoById"/>/
        /// <see cref="ITwPageRepository.GetDeletedPageById"/> immediately afterward, matching
        /// <see cref="PageRepositoryCrudAttachmentTests"/>'s own "cleanup in a helper, verify in the test body"
        /// convention.
        /// </summary>
        private static async Task DeleteTestPageAsync(ITwPageRepository pageRepo, int pageId, Guid deletedByUserId)
        {
            await pageRepo.MovePageToDeletedById(pageId, deletedByUserId);
            await pageRepo.PurgeDeletedPageByPageId(pageId);
        }

        /// <summary>
        /// Loops every page of <see cref="ITwPageRepository.GetAllDeletedPagesPaged"/> (bounded by its own
        /// <see cref="TwPage.PaginationPageCount"/>) looking for <paramref name="pageId"/> - a genuinely global,
        /// unscoped listing across every deleted page in the shared database, per this class's own remarks.
        /// </summary>
        private static async Task<TwPage?> FindDeletedPageAsync(ITwPageRepository pageRepo, int pageId)
        {
            var firstPage = await pageRepo.GetAllDeletedPagesPaged(1);
            if (firstPage.Count == 0)
            {
                return null;
            }

            var totalPages = firstPage[0].PaginationPageCount;
            for (var pageNumber = 1; pageNumber <= totalPages; pageNumber++)
            {
                var items = pageNumber == 1 ? firstPage : await pageRepo.GetAllDeletedPagesPaged(pageNumber);
                var match = items.FirstOrDefault(p => p.Id == pageId);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        /// <summary>
        /// Loops every page of <see cref="ITwPageRepository.GetDeletedPageRevisionsByIdPaged"/> for
        /// <paramref name="pageId"/> (bounded by its own <see cref="TwPage.PaginationPageCount"/>) looking for
        /// <paramref name="revision"/> - already scoped to one page by its own <c>pageId</c> parameter, per this
        /// class's own remarks (the loop itself is just defensive pagination-size safety, not cross-test
        /// isolation).
        /// </summary>
        private static async Task<TwDeletedPageRevision?> FindDeletedPageRevisionAsync(ITwPageRepository pageRepo, int pageId, int revision)
        {
            var firstPage = await pageRepo.GetDeletedPageRevisionsByIdPaged(pageId, 1);
            if (firstPage.Count == 0)
            {
                return null;
            }

            var totalPages = firstPage[0].PaginationPageCount;
            for (var pageNumber = 1; pageNumber <= totalPages; pageNumber++)
            {
                var items = pageNumber == 1 ? firstPage : await pageRepo.GetDeletedPageRevisionsByIdPaged(pageId, pageNumber);
                var match = items.FirstOrDefault(r => r.Revision == revision);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        [Fact]
        public async Task MovePageToDeletedById_MovesPageToDeletedPages_RestoreDeletedPageByPageId_RestoresOriginalContent()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var admin = await GetAdminAsync(fixture);

            var body = $"Delete/restore round-trip content {Guid.NewGuid():N}.\r\n";
            var page = await CreateTestPageAsync(fixture, admin.UserId, $"ZzzDeleteRestorePage_{Guid.NewGuid():N}", body);

            try
            {
                Assert.NotNull(await pageRepo.GetPageRevisionInfoById(page.Id));
                Assert.Null(await pageRepo.GetDeletedPageById(page.Id));

                await pageRepo.MovePageToDeletedById(page.Id, admin.UserId);

                //Gone from the Pages schema (pageId-keyed read - see this class's own remarks for why
                //GetPageInfoByNavigation is never used here instead).
                Assert.Null(await pageRepo.GetPageRevisionInfoById(page.Id));
                Assert.Equal(0, await pageRepo.GetCountOfPageAttachmentsById(page.Id));

                //...and present in DeletedPages, with the original content intact.
                var deletedPage = await pageRepo.GetDeletedPageById(page.Id);
                Assert.NotNull(deletedPage);
                Assert.Equal(page.Name, deletedPage!.Name);
                Assert.Equal(body, deletedPage.Body);
                Assert.Equal(admin.AccountName, deletedPage.DeletedByUserName);
                Assert.NotEqual(default, deletedPage.DeletedDate);

                //Also discoverable through the paged "all deleted pages" listing.
                var listed = await FindDeletedPageAsync(pageRepo, page.Id);
                Assert.NotNull(listed);
                Assert.Equal(page.Name, listed!.Name);

                await pageRepo.RestoreDeletedPageByPageId(page.Id);

                //Gone from DeletedPages again...
                Assert.Null(await pageRepo.GetDeletedPageById(page.Id));

                //...and back in Pages, at the same revision, with the same content.
                var restored = await pageRepo.GetPageRevisionInfoById(page.Id);
                Assert.NotNull(restored);
                Assert.Equal(1, restored!.Revision);
                Assert.Equal(1, await pageRepo.GetCurrentPageRevision(page.Id));

                var restoredContent = await pageRepo.GetPageRevisionById(page.Id);
                Assert.NotNull(restoredContent);
                Assert.Equal(body, restoredContent!.Body);
            }
            finally
            {
                await DeleteTestPageAsync(pageRepo, page.Id, admin.UserId);
            }

            Assert.Null(await pageRepo.GetPageRevisionInfoById(page.Id));
            Assert.Null(await pageRepo.GetDeletedPageById(page.Id));
        }

        [Fact]
        public async Task MovePageToDeletedById_PurgeDeletedPageByPageId_PermanentlyRemovesFromBothSchemas()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var admin = await GetAdminAsync(fixture);

            var page = await CreateTestPageAsync(fixture, admin.UserId, $"ZzzDeleteRestorePurge_{Guid.NewGuid():N}",
                $"Purge candidate {Guid.NewGuid():N}.\r\n");

            try
            {
                await pageRepo.MovePageToDeletedById(page.Id, admin.UserId);

                //Sanity - it actually landed in DeletedPages before we permanently purge it below.
                Assert.NotNull(await pageRepo.GetDeletedPageById(page.Id));

                await pageRepo.PurgeDeletedPageByPageId(page.Id);

                Assert.Null(await pageRepo.GetDeletedPageById(page.Id));
                Assert.Null(await FindDeletedPageAsync(pageRepo, page.Id));
                Assert.Null(await pageRepo.GetPageRevisionInfoById(page.Id));
            }
            finally
            {
                //Idempotent even though the page is already fully purged above - see this class's own remarks on
                //why calling MovePageToDeletedById/PurgeDeletedPageByPageId again here is always safe.
                await DeleteTestPageAsync(pageRepo, page.Id, admin.UserId);
            }

            Assert.Null(await pageRepo.GetPageRevisionInfoById(page.Id));
            Assert.Null(await pageRepo.GetDeletedPageById(page.Id));
        }

        [Fact]
        public async Task MovePageRevisionToDeletedById_RestoreDeletedPageRevisionByPageIdAndRevision_RoundTripsHistoricalRevision_LeavesCurrentRevisionUntouched()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var admin = await GetAdminAsync(fixture);

            var bodyV1 = $"Revision one {Guid.NewGuid():N}.\r\n";
            var bodyV2 = $"Revision two, completely different {Guid.NewGuid():N}.\r\n";
            var page = await CreateTestPageAsync(fixture, admin.UserId, $"ZzzDeleteRestoreRevision_{Guid.NewGuid():N}", bodyV1);

            try
            {
                await AddPageRevisionAsync(fixture, page, bodyV2);

                Assert.Equal(2, await pageRepo.GetCurrentPageRevision(page.Id));
                Assert.Equal(2, await pageRepo.GetPageRevisionCountByPageId(page.Id));

                await pageRepo.MovePageRevisionToDeletedById(page.Id, revision: 1, admin.UserId);

                //Revision 1 is gone from Pages...
                Assert.Null(await pageRepo.GetPageRevisionById(page.Id, revision: 1));
                Assert.Null(await pageRepo.GetPageRevisionInfoById(page.Id, revision: 1));
                Assert.Equal(1, await pageRepo.GetPageRevisionCountByPageId(page.Id));

                //...but the page's current-revision pointer (and revision 2's own content) is untouched - this
                //member never moves Pages.Page.Revision itself, matching EfPageRepository's own remarks.
                Assert.Equal(2, await pageRepo.GetCurrentPageRevision(page.Id));
                var stillCurrent = await pageRepo.GetPageRevisionById(page.Id, revision: 2);
                Assert.NotNull(stillCurrent);
                Assert.Equal(bodyV2, stillCurrent!.Body);

                //...and present in DeletedPageRevisions, with the original content intact.
                var deletedRevision = await pageRepo.GetDeletedPageRevisionById(page.Id, revision: 1);
                Assert.NotNull(deletedRevision);
                Assert.Equal(bodyV1, deletedRevision!.Body);
                Assert.Equal(admin.AccountName, deletedRevision.DeletedByUserName);

                //Also discoverable through the paged "deleted revisions for this page" listing.
                Assert.NotNull(await FindDeletedPageRevisionAsync(pageRepo, page.Id, revision: 1));

                await pageRepo.RestoreDeletedPageRevisionByPageIdAndRevision(page.Id, revision: 1);

                Assert.Null(await pageRepo.GetDeletedPageRevisionById(page.Id, revision: 1));

                var restoredRevision = await pageRepo.GetPageRevisionById(page.Id, revision: 1);
                Assert.NotNull(restoredRevision);
                Assert.Equal(bodyV1, restoredRevision!.Body);

                Assert.Equal(2, await pageRepo.GetPageRevisionCountByPageId(page.Id));
                Assert.Equal(2, await pageRepo.GetCurrentPageRevision(page.Id));
            }
            finally
            {
                await DeleteTestPageAsync(pageRepo, page.Id, admin.UserId);
            }

            Assert.Null(await pageRepo.GetPageRevisionInfoById(page.Id));
        }

        [Fact]
        public async Task MovePageRevisionToDeletedById_PurgeDeletedPageRevisionByPageIdAndRevision_PermanentlyRemovesMiddleRevision_NextAndPreviousRevisionSkipTheGap()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var admin = await GetAdminAsync(fixture);

            var bodyV1 = $"Revision one {Guid.NewGuid():N}.\r\n";
            var bodyV2 = $"Revision two {Guid.NewGuid():N}.\r\n";
            var bodyV3 = $"Revision three {Guid.NewGuid():N}.\r\n";
            var page = await CreateTestPageAsync(fixture, admin.UserId, $"ZzzDeleteRestoreMiddleRevision_{Guid.NewGuid():N}", bodyV1);

            try
            {
                await AddPageRevisionAsync(fixture, page, bodyV2);
                await AddPageRevisionAsync(fixture, page, bodyV3);

                Assert.Equal(3, await pageRepo.GetCurrentPageRevision(page.Id));
                Assert.Equal(3, await pageRepo.GetPageRevisionCountByPageId(page.Id));

                //Before deleting anything - the ordinary, contiguous next/previous chain.
                Assert.Equal(2, await pageRepo.GetPageNextRevision(page.Id, 1));
                Assert.Equal(3, await pageRepo.GetPageNextRevision(page.Id, 2));
                Assert.Equal(0, await pageRepo.GetPageNextRevision(page.Id, 3));
                Assert.Equal(0, await pageRepo.GetPagePreviousRevision(page.Id, 1));
                Assert.Equal(1, await pageRepo.GetPagePreviousRevision(page.Id, 2));
                Assert.Equal(2, await pageRepo.GetPagePreviousRevision(page.Id, 3));

                await pageRepo.MovePageRevisionToDeletedById(page.Id, revision: 2, admin.UserId);

                Assert.Null(await pageRepo.GetPageRevisionById(page.Id, revision: 2));

                var deletedRevision = await pageRepo.GetDeletedPageRevisionById(page.Id, revision: 2);
                Assert.NotNull(deletedRevision);
                Assert.Equal(bodyV2, deletedRevision!.Body);

                //Revision 2 is a gap now - next/previous skip straight from 1 to 3.
                Assert.Equal(3, await pageRepo.GetPageNextRevision(page.Id, 1));
                Assert.Equal(1, await pageRepo.GetPagePreviousRevision(page.Id, 3));

                //The current revision (3) is untouched, since revision 2 was never the current one.
                var latest = await pageRepo.GetLatestPageRevisionById(page.Id);
                Assert.NotNull(latest);
                Assert.Equal(bodyV3, latest!.Body);
                Assert.Equal(3, await pageRepo.GetCurrentPageRevision(page.Id));

                await pageRepo.PurgeDeletedPageRevisionByPageIdAndRevision(page.Id, revision: 2);

                //Permanently gone - never restored, no longer recoverable from DeletedPageRevisions either.
                Assert.Null(await pageRepo.GetDeletedPageRevisionById(page.Id, revision: 2));
                Assert.Null(await pageRepo.GetPageRevisionById(page.Id, revision: 2));

                //Revisions 1 and 3 remain, entirely unaffected by purging the revision 2 gap.
                Assert.Equal(2, await pageRepo.GetPageRevisionCountByPageId(page.Id));
                Assert.Equal(3, await pageRepo.GetCurrentPageRevision(page.Id));
                var revision1 = await pageRepo.GetPageRevisionById(page.Id, revision: 1);
                Assert.NotNull(revision1);
                Assert.Equal(bodyV1, revision1!.Body);
            }
            finally
            {
                await DeleteTestPageAsync(pageRepo, page.Id, admin.UserId);
            }

            Assert.Null(await pageRepo.GetPageRevisionInfoById(page.Id));
        }

        [Fact]
        public async Task PurgeDeletedPageRevisionsByPageId_RemovesAllDeletedRevisionsForOnePage_LeavesOtherPagesDeletedRevisionsIntact()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var admin = await GetAdminAsync(fixture);

            var pageA = await CreateTestPageAsync(fixture, admin.UserId, $"ZzzDeleteRestoreScopeA_{Guid.NewGuid():N}",
                $"Page A revision one {Guid.NewGuid():N}.\r\n");
            var pageB = await CreateTestPageAsync(fixture, admin.UserId, $"ZzzDeleteRestoreScopeB_{Guid.NewGuid():N}",
                $"Page B revision one {Guid.NewGuid():N}.\r\n");

            try
            {
                await AddPageRevisionAsync(fixture, pageA, $"Page A revision two {Guid.NewGuid():N}.\r\n");
                await AddPageRevisionAsync(fixture, pageA, $"Page A revision three {Guid.NewGuid():N}.\r\n");
                await AddPageRevisionAsync(fixture, pageB, $"Page B revision two {Guid.NewGuid():N}.\r\n");

                //pageA: move its two historical revisions (1 and 2) to DeletedPageRevisions, keeping revision 3
                //current.
                await pageRepo.MovePageRevisionToDeletedById(pageA.Id, revision: 1, admin.UserId);
                await pageRepo.MovePageRevisionToDeletedById(pageA.Id, revision: 2, admin.UserId);

                //pageB: move its one historical revision (1) too, keeping revision 2 current.
                await pageRepo.MovePageRevisionToDeletedById(pageB.Id, revision: 1, admin.UserId);

                Assert.NotNull(await pageRepo.GetDeletedPageRevisionById(pageA.Id, revision: 1));
                Assert.NotNull(await pageRepo.GetDeletedPageRevisionById(pageA.Id, revision: 2));
                Assert.NotNull(await pageRepo.GetDeletedPageRevisionById(pageB.Id, revision: 1));

                await pageRepo.PurgeDeletedPageRevisionsByPageId(pageA.Id);

                //pageA's deleted revisions are gone entirely...
                Assert.Null(await pageRepo.GetDeletedPageRevisionById(pageA.Id, revision: 1));
                Assert.Null(await pageRepo.GetDeletedPageRevisionById(pageA.Id, revision: 2));

                //...but pageB's own deleted revision is untouched - this call is scoped to pageA alone.
                Assert.NotNull(await pageRepo.GetDeletedPageRevisionById(pageB.Id, revision: 1));

                //pageA's still-live revision 3 is unaffected - purging deleted *revisions* never touches the
                //page's own current row.
                Assert.Equal(3, await pageRepo.GetCurrentPageRevision(pageA.Id));
                Assert.NotNull(await pageRepo.GetPageRevisionById(pageA.Id, revision: 3));
            }
            finally
            {
                //Cleans up pageB's own still-live deleted revision 1 too, per this class's own remarks on
                //DeleteTestPageAsync purging all three schemas regardless of the partial state the test left
                //things in.
                await DeleteTestPageAsync(pageRepo, pageA.Id, admin.UserId);
                await DeleteTestPageAsync(pageRepo, pageB.Id, admin.UserId);
            }

            Assert.Null(await pageRepo.GetPageRevisionInfoById(pageA.Id));
            Assert.Null(await pageRepo.GetPageRevisionInfoById(pageB.Id));
            Assert.Null(await pageRepo.GetDeletedPageRevisionById(pageB.Id, revision: 1));
        }
    }
}
