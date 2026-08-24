using System.Text;
using TightWiki.Library;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Tests.Unit
{
    /// <summary>
    /// Scenario-level integration tests for the fourth of five <see cref="ITwPageRepository"/> test tasks (86
    /// members total, per <c>EfPageRepository</c>'s own class-level remarks - far too many for one test file, same
    /// reasoning as <see cref="PageRepositoryMetadataTests"/>/<see cref="PageRepositoryListingTests"/>/<see
    /// cref="PageRepositorySearchTests"/>/<see cref="UsersRepositoryRoleTests"/>/<see
    /// cref="UsersRepositoryPermissionAuthTests"/>/<see cref="UsersRepositoryProfileTests"/> split their own
    /// interfaces into multiple files). Covers the 5 CRUD/upsert members (<see cref="ITwPageRepository.UpsertPage"/>,
    /// <see cref="ITwPageRepository.RefreshPageMetadata"/>, <see cref="ITwPageRepository.UpdatePageProcessingInstructions"/>,
    /// <see cref="ITwPageRepository.UpdateSinglePageReference"/>, <see cref="ITwPageRepository.UpdatePageReferences"/>)
    /// and the 11 page file/attachment members (<see cref="ITwPageRepository.DetachPageRevisionAttachment"/>,
    /// <see cref="ITwPageRepository.GetOrphanedPageAttachmentsPaged"/>, <see cref="ITwPageRepository.PurgeOrphanedPageAttachments"/>,
    /// <see cref="ITwPageRepository.PurgeOrphanedPageAttachment"/>,
    /// <see cref="ITwPageRepository.GetPageFilesInfoByPageNavigationAndPageRevisionPaged"/>,
    /// <see cref="ITwPageRepository.GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation"/>,
    /// <see cref="ITwPageRepository.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation"/>,
    /// <see cref="ITwPageRepository.GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation"/>,
    /// <see cref="ITwPageRepository.GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged"/>,
    /// <see cref="ITwPageRepository.GetPageFilesInfoByPageId"/>, <see cref="ITwPageRepository.UpsertPageFile"/>)
    /// <c>EfPageRepository</c>'s own class-level remarks call out as landing in phases 2b.6/2b.7. Search/tags/
    /// tokens, bulk/paged listings, and single-page metadata reads were covered by the first three tasks (<see
    /// cref="PageRepositoryMetadataTests"/>/<see cref="PageRepositoryListingTests"/>/<see cref="PageRepositorySearchTests"/>);
    /// delete/restore (the final 15 members, phase 2b.8) is the fifth and last task - this class only ever deletes
    /// the pages it creates itself, as cleanup, never as a feature under test in its own right.
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
    /// <b>Every page this class touches is created (and torn down) by the test itself, GUID-named:</b> unlike the
    /// read-only-against-shared-seed-data pattern the first three tasks use, CRUD/upsert and attachment upload are
    /// inherently mutating, so there is no safe already-seeded page to reuse. Every scenario below creates its own
    /// page(s) via <see cref="CreateTestPageAsync"/> with a <see cref="Guid.NewGuid()"/>-suffixed name, and deletes
    /// them again via <see cref="DeleteTestPageAsync"/> (<see cref="ITwPageRepository.MovePageToDeletedById"/> +
    /// <see cref="ITwPageRepository.PurgeDeletedPageByPageId"/>) in a <c>finally</c> block, then asserts the page
    /// (and, where relevant, its attachments) are genuinely gone afterward - the same "restore/verify in a
    /// finally, assert afterward" shape <see cref="PageRepositorySearchTests"/>' own round-trip scenarios use, just
    /// with permanent deletion instead of restoring prior content. The one exception is
    /// <see cref="UpsertPage_NewPage_InsertsAtRevision1_PopulatesProcessingInstructionsAndOutgoingReferences_ViaRefreshPageMetadata"/>'s
    /// link target, which is the real, already-seeded "Sandbox :: Default" page (same page
    /// <see cref="PageRepositoryMetadataTests"/> reads from) - only ever read from and linked *to*, never mutated;
    /// gaining one more incoming <see cref="ITwPageRepository.GetBacklinkPagesPaged"/> row from a test page that
    /// itself gets deleted again cannot affect any other test's assertions about "Sandbox :: Default", since no
    /// other test file asserts an exact backlink count for it.
    /// </para>
    /// <para>
    /// <b>A real caching gotcha this class had to design around:</b> <see cref="ITwPageRepository.GetPageInfoByNavigation"/>
    /// is cached under a key built from the page's own navigation string alone
    /// (<c>[Page]:[navigation]:[GetPageInfoByNavigation]</c>). <see cref="ITwPageRepository.FlushPageCache"/> -
    /// called at the end of both <see cref="ITwPageRepository.MovePageToDeletedById"/> and
    /// <see cref="ITwPageRepository.PurgeDeletedPageByPageId"/> - looks up the page's navigation by re-querying
    /// Pages.Page for the given pageId <i>after</i> the row has already been deleted, so that lookup returns null
    /// and the navigation-keyed cache entry for a just-deleted page can never actually be cleared this way (only
    /// the pageId-keyed entries - <c>[Page]:[pageId]:...</c>, cleared unconditionally since the pageId itself is
    /// already known - are reliably invalidated post-delete). Confirmed empirically (an earlier version of this
    /// class that called <see cref="ITwPageRepository.GetPageInfoByNavigation"/> exactly once, only in the final
    /// "page is really gone" assertion, still failed intermittently under a full, highly-parallel <c>dotnet test</c>
    /// run - some *other* concurrently-running collection reading the same navigation before this test's own
    /// cleanup ran is exactly the kind of cross-collection interaction this gap makes observable). So this class
    /// never calls <see cref="ITwPageRepository.GetPageInfoByNavigation"/> for any page it creates itself at all -
    /// not even for the final "gone" check - using <see cref="ITwPageRepository.GetPageRevisionInfoById"/> instead
    /// (confirmed uncached on both the SQLite reference and <c>EfPageRepository</c>, so it is always a fresh read,
    /// immune to this gap by construction) plus pageId-keyed reads (<see cref="ITwPageRepository.GetLimitedPageInfoByIdAndRevision"/>,
    /// <see cref="ITwPageRepository.GetCurrentPageRevision"/>, <see cref="ITwPageRepository.GetPageRevisionById"/>,
    /// etc. - reliably invalidated post-delete, per this paragraph's own analysis) for every other assertion. The
    /// sibling <see cref="ITwPageRepository.GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation"/>
    /// has the same kind of gap in the other direction - <see cref="ITwPageRepository.UpsertPageFile"/> never
    /// invalidates its cache entry at all (matching the SQLite reference, per that method's own remarks) - so it is
    /// called at most once per (pageNavigation, fileNavigation, pageRevision) triple anywhere in this file too, to
    /// avoid ever observing a stale cached attachment read after a same-page-revision re-upload.
    /// </para>
    /// <para>
    /// <b>Hash-based revision bumping - the architectural core of this task:</b> both
    /// <see cref="ITwPageRepository.UpsertPage"/> (via its private <c>SavePage</c> helper) and
    /// <see cref="ITwPageRepository.UpsertPageFile"/> only ever create a new revision when a CRC32 hash of the
    /// content actually differs from what is currently stored; otherwise the call is a genuine no-op (no new
    /// Pages.PageRevision/Pages.PageFileRevision row, no revision-counter bump).
    /// <see cref="UpsertPage_ExistingPage_HashBasedChangeDetection_BumpsRevisionOnlyWhenContentActuallyChanges"/>
    /// exercises this for pages (changed body bumps; re-saving the identical Name/Namespace/Description/
    /// ChangeSummary/Body does not); <see cref="UpsertPageFile_ReuploadSameContent_HashBasedDedup_DoesNotBumpFileRevision"/>/
    /// <see cref="UpsertPageFile_ReuploadDifferentContent_BumpsFileRevision_PreservesPreviousRevisionContent"/>
    /// exercise the structurally analogous logic for file attachments (identical bytes re-uploaded is a no-op;
    /// different bytes bumps <see cref="TwPageFileAttachmentInfo.FileRevision"/> and preserves the prior revision's
    /// content, independently readable by its own explicit revision number).
    /// </para>
    /// <para>
    /// <b>Global, unscoped orphan-attachment members are deliberately avoided:</b>
    /// <see cref="ITwPageRepository.GetOrphanedPageAttachmentsPaged"/> lists orphaned file revisions across every
    /// page in the shared, persistent test database (not scoped to this class's own pages), so
    /// <see cref="DetachPageRevisionAttachment_OrphansFileRevision_GetOrphanedPageAttachmentsPaged_PurgeOrphanedPageAttachment_RemovesItPermanently"/>
    /// loops through every page of results (bounded by the method's own <see cref="TwOrphanedPageAttachment.PaginationPageCount"/>)
    /// looking for this test's own (PageFileId, FileRevision) pair, rather than assuming it lands on page 1. The
    /// bulk, fully-unscoped <see cref="ITwPageRepository.PurgeOrphanedPageAttachments"/> overload (no arguments) is
    /// never called anywhere in this file - doing so could purge an orphaned attachment belonging to a different,
    /// concurrently-running test class against this same shared database; only the single-item
    /// <see cref="ITwPageRepository.PurgeOrphanedPageAttachment"/> overload (scoped to one explicit pageFileId/
    /// revision pair this test itself created) is used.
    /// </para>
    /// </remarks>
    [Collection("Page Repository Crud Attachment Tests")]
    public class PageRepositoryCrudAttachmentTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        /// <summary>
        /// The full name of the real, already-seeded page used as the link target in
        /// <see cref="UpsertPage_NewPage_InsertsAtRevision1_PopulatesProcessingInstructionsAndOutgoingReferences_ViaRefreshPageMetadata"/> -
        /// same page <see cref="PageRepositoryMetadataTests"/> reads from, for the same "stable, real seed content,
        /// never mutated by this test" reasoning (see this class's own remarks).
        /// </summary>
        private const string SeededLinkTargetPageName = "Sandbox :: Default";

        /// <summary>
        /// Creates and saves a brand-new page via <see cref="ITwPageRepository.UpsertPage"/>, using the given
        /// exact <paramref name="pageName"/> (callers that need GUID-uniqueness build that into the name
        /// themselves - kept as an exact name rather than a prefix so
        /// <see cref="UpsertPage_NewlyCreatedPage_ResolvesPreExistingOrphanReferences_ViaUpdateSinglePageReference"/>
        /// can create a page whose navigation exactly matches a pre-computed target). Sets
        /// <see cref="TwPage.Navigation"/> explicitly before calling <see cref="ITwPageRepository.UpsertPage"/> -
        /// required for <see cref="ITwPageRepository.UpdateSinglePageReference"/>'s own orphan-reference-resolution
        /// step to work at all, since <c>SavePage</c> reads <see cref="TwPage.Navigation"/> but never writes it
        /// back onto the passed-in <see cref="TwPage"/> (same computed-from-Name convention
        /// <see cref="TwEngineFixture.WikiTransform"/> already follows).
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
                Description = "PageRepositoryCrudAttachmentTests scratch page.",
                CreatedByUserId = createdByUserId,
                ModifiedByUserId = createdByUserId,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
            };

            page.Id = await pageRepo.UpsertPage(fixture.Artifacts.Engine, fixture.Artifacts.Localizer, page, fixture.CreateWikiSession());

            return page;
        }

        /// <summary>
        /// Uploads/replaces a file attachment via <see cref="ITwPageRepository.UpsertPageFile"/>, mirroring
        /// <see cref="TwEngineFixture.AttachFile"/>'s own field population.
        /// </summary>
        private static async Task UpsertTestFileAsync(ITwPageRepository pageRepo, int pageId, Guid userId, string fileName, string fileNavigation, byte[] data)
        {
            await pageRepo.UpsertPageFile(new TwPageFileAttachment
            {
                PageId = pageId,
                Name = fileName,
                FileNavigation = fileNavigation,
                Data = data,
                Size = data.Length,
                ContentType = Utility.GetMimeType(fileName),
                CreatedDate = DateTime.UtcNow,
            }, userId);
        }

        /// <summary>
        /// Permanently removes a test page via <see cref="ITwPageRepository.MovePageToDeletedById"/> +
        /// <see cref="ITwPageRepository.PurgeDeletedPageByPageId"/> - the only way to actually make a page (and,
        /// transitively, its attachments - see <see cref="ITwPageRepository.MovePageToDeletedById"/>'s own remarks
        /// for exactly what gets cascaded) disappear. Deliberately does not itself assert anything - every caller
        /// asserts the page (and, where relevant, its attachments) are gone via
        /// <see cref="ITwPageRepository.GetPageRevisionInfoById"/>/<see cref="ITwPageRepository.GetCountOfPageAttachmentsById"/>/
        /// <see cref="ITwPageRepository.GetPageFilesInfoByPageId"/> immediately afterward, in the test body itself
        /// rather than buried in this helper, matching this class's own "cleanup in <c>finally</c>, verify
        /// afterward" convention (see this class's own remarks).
        /// </summary>
        private static async Task DeleteTestPageAsync(ITwPageRepository pageRepo, int pageId, Guid deletedByUserId)
        {
            await pageRepo.MovePageToDeletedById(pageId, deletedByUserId);
            await pageRepo.PurgeDeletedPageByPageId(pageId);
        }

        /// <summary>
        /// Loops every page of <see cref="ITwPageRepository.GetMissingPagesPaged"/> (bounded by its own
        /// <see cref="TwNonexistentPage.PaginationPageCount"/>) looking for a broken reference from
        /// <paramref name="sourcePageId"/> to <paramref name="targetNavigation"/> - see this class's own remarks
        /// for why this global, unscoped listing cannot be assumed to land this test's own entry on page 1.
        /// </summary>
        private static async Task<TwNonexistentPage?> FindMissingPageAsync(ITwPageRepository pageRepo, int sourcePageId, string targetNavigation)
        {
            var firstPage = await pageRepo.GetMissingPagesPaged(1);
            if (firstPage.Count == 0)
            {
                return null;
            }

            var totalPages = firstPage[0].PaginationPageCount;
            for (var pageNumber = 1; pageNumber <= totalPages; pageNumber++)
            {
                var items = pageNumber == 1 ? firstPage : await pageRepo.GetMissingPagesPaged(pageNumber);
                var match = items.FirstOrDefault(m => m.SourcePageId == sourcePageId
                    && string.Equals(m.TargetPageNavigation, targetNavigation, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        /// <summary>
        /// Loops every page of <see cref="ITwPageRepository.GetOrphanedPageAttachmentsPaged"/> (bounded by its own
        /// <see cref="TwOrphanedPageAttachment.PaginationPageCount"/>) looking for the given (pageFileId,
        /// fileRevision) pair - same "global, unscoped listing" reasoning as <see cref="FindMissingPageAsync"/>.
        /// </summary>
        private static async Task<TwOrphanedPageAttachment?> FindOrphanedAttachmentAsync(ITwPageRepository pageRepo, int pageFileId, int fileRevision)
        {
            var firstPage = await pageRepo.GetOrphanedPageAttachmentsPaged(1);
            if (firstPage.Count == 0)
            {
                return null;
            }

            var totalPages = firstPage[0].PaginationPageCount;
            for (var pageNumber = 1; pageNumber <= totalPages; pageNumber++)
            {
                var items = pageNumber == 1 ? firstPage : await pageRepo.GetOrphanedPageAttachmentsPaged(pageNumber);
                var match = items.FirstOrDefault(o => o.PageFileId == pageFileId && o.FileRevision == fileRevision);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static async Task<TwAccountProfile> GetAdminAsync(TwEngineFixture fixture)
        {
            var usersRepo = fixture.Artifacts.DatabaseManager.UsersRepository;
            return await usersRepo.GetAccountProfileByNavigation(TwNavigation.Clean(Constants.DEFAULTACCOUNT))
                ?? throw new Exception($"Could not find the seeded '{Constants.DEFAULTACCOUNT}' account profile.");
        }

        [Fact]
        public async Task UpsertPage_NewPage_InsertsAtRevision1_PopulatesProcessingInstructionsAndOutgoingReferences_ViaRefreshPageMetadata()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var admin = await GetAdminAsync(fixture);

            var linkTargetNavigation = TwNamespaceNavigation.CleanAndValidate(SeededLinkTargetPageName);
            var linkTarget = await pageRepo.GetPageInfoByNavigation(linkTargetNavigation)
                ?? throw new Exception($"Could not find the seeded '{SeededLinkTargetPageName}' page.");

            //@@Draft() is a TwProcessingInstructionFunctionPlugin (Demarcation "@@" - ClassificationFunctions.Draft's
            //own attribute); [[...]] is the internal-link markup handler (MarkupHandlers.HandleInternalLinks's own
            //doc comment: "[[PageName]] or [[PageName|Link Text]]").
            var body = $"@@Draft()\r\nSee [[{SeededLinkTargetPageName}]] for more information.\r\n";
            var page = await CreateTestPageAsync(fixture, admin.UserId, $"ZzzCrudNew_{Guid.NewGuid():N}", body);

            try
            {
                //Uses pageId-keyed reads throughout (never ITwPageRepository.GetPageInfoByNavigation) - see this
                //class's own remarks for why.
                var limited = await pageRepo.GetLimitedPageInfoByIdAndRevision(page.Id);
                Assert.NotNull(limited);
                Assert.Equal(1, limited!.Revision);
                Assert.Equal(page.Name, limited.Name);
                Assert.Equal(page.Description, limited.Description);
                Assert.Equal(1, await pageRepo.GetCurrentPageRevision(page.Id));
                Assert.Equal(1, await pageRepo.GetPageRevisionCountByPageId(page.Id));

                var fullRevision = await pageRepo.GetPageRevisionById(page.Id);
                Assert.NotNull(fullRevision);
                Assert.Equal(body, fullRevision!.Body);

                //RefreshPageMetadata (run by UpsertPage) parses @@Draft() into state.ProcessingInstructions and
                //writes it via UpdatePageProcessingInstructions, lower-invariant ("draft" -
                //UpdatePageProcessingInstructions's own ToLowerInvariant()) - matched here case-insensitively via
                //TwProcessingInstructionCollection.Contains.
                var instructions = await pageRepo.GetPageProcessingInstructionsByPageId(page.Id);
                Assert.True(instructions.Contains(TwInstruction.Draft));

                //The internal link handler adds an outgoing reference regardless of whether the target already
                //exists; since SeededLinkTargetPageName is a real, already-seeded page, UpdatePageReferences (run
                //by RefreshPageMetadata) resolves ReferencesPageId immediately at insert time - proven here via the
                //target's own GetBacklinkPagesPaged, which only surfaces pages with a *resolved* (non-null)
                //ReferencesPageId (EfPageRepository.GetBacklinkPagesPaged's own "backlinkIds" query).
                var backlinks = await pageRepo.GetBacklinkPagesPaged(linkTarget.Id, pageNumber: 1, pageSize: 500);
                Assert.Contains(backlinks, p => p.Id == page.Id);
            }
            finally
            {
                await DeleteTestPageAsync(pageRepo, page.Id, admin.UserId);
            }

            //GetPageRevisionInfoById, not GetPageInfoByNavigation - see this class's own remarks for why the
            //latter is never used against a page this class itself creates and deletes.
            Assert.Null(await pageRepo.GetPageRevisionInfoById(page.Id));
            Assert.Equal(0, await pageRepo.GetCountOfPageAttachmentsById(page.Id));
        }

        [Fact]
        public async Task UpsertPage_ExistingPage_HashBasedChangeDetection_BumpsRevisionOnlyWhenContentActuallyChanges()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var admin = await GetAdminAsync(fixture);

            var bodyV1 = $"@@Draft()\r\nVersion one content {Guid.NewGuid():N}.\r\n";
            var page = await CreateTestPageAsync(fixture, admin.UserId, $"ZzzCrudUpdate_{Guid.NewGuid():N}", bodyV1);

            try
            {
                Assert.Equal(1, await pageRepo.GetCurrentPageRevision(page.Id));
                Assert.True((await pageRepo.GetPageProcessingInstructionsByPageId(page.Id)).Contains(TwInstruction.Draft));

                //Change the body (drop @@Draft(), completely different text) - SavePage's own hash-based change
                //detection (a CRC32 mismatch against the previously-stored Body) must bump the revision.
                var bodyV2 = $"Version two content, completely different {Guid.NewGuid():N}.\r\n";
                page.Body = bodyV2;
                page.Id = await pageRepo.UpsertPage(fixture.Artifacts.Engine, fixture.Artifacts.Localizer, page, fixture.CreateWikiSession());

                Assert.Equal(2, await pageRepo.GetCurrentPageRevision(page.Id));
                Assert.Equal(2, await pageRepo.GetPageRevisionCountByPageId(page.Id));

                //The old revision's content is preserved verbatim, independently addressable by its own explicit
                //revision number - not overwritten in place.
                var revision1 = await pageRepo.GetPageRevisionById(page.Id, revision: 1);
                Assert.NotNull(revision1);
                Assert.Equal(bodyV1, revision1!.Body);

                var revision2 = await pageRepo.GetPageRevisionById(page.Id, revision: 2);
                Assert.NotNull(revision2);
                Assert.Equal(bodyV2, revision2!.Body);

                //Tags/instructions/references are not versioned - RefreshPageMetadata always reflects only the
                //latest revision's own markup, so @@Draft() having been dropped from bodyV2 means it is gone
                //entirely now, not merely absent "as of revision 2".
                Assert.False((await pageRepo.GetPageProcessingInstructionsByPageId(page.Id)).Contains(TwInstruction.Draft));

                //Calling UpsertPage again with the exact same Name/Namespace/Description/ChangeSummary/Body as what
                //was just saved must be a genuine no-op (SavePage's own remarks: "changed" is whichever of those
                //fields differs from the currently-stored row) - no new revision, no new Pages.PageRevision row.
                page.Id = await pageRepo.UpsertPage(fixture.Artifacts.Engine, fixture.Artifacts.Localizer, page, fixture.CreateWikiSession());

                Assert.Equal(2, await pageRepo.GetCurrentPageRevision(page.Id));
                Assert.Equal(2, await pageRepo.GetPageRevisionCountByPageId(page.Id));
            }
            finally
            {
                await DeleteTestPageAsync(pageRepo, page.Id, admin.UserId);
            }

            Assert.Null(await pageRepo.GetPageRevisionInfoById(page.Id));
        }

        [Fact]
        public async Task UpsertPage_NewlyCreatedPage_ResolvesPreExistingOrphanReferences_ViaUpdateSinglePageReference()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var admin = await GetAdminAsync(fixture);

            var targetName = $"ZzzCrudRefTarget_{Guid.NewGuid():N}";
            var targetNavigation = TwNamespaceNavigation.CleanAndValidate(targetName);

            TwPage? pageX = null;
            TwPage? pageY = null;

            try
            {
                //pageX links to targetName before any page with that navigation exists - UpdatePageReferences (run
                //by RefreshPageMetadata) can only leave ReferencesPageId null for it (EfPageRepository.UpdatePageReferences's
                //own remarks: "left null when no page with that navigation currently exists").
                var bodyX = $"References the not-yet-existing [[{targetName}]] page.\r\n";
                pageX = await CreateTestPageAsync(fixture, admin.UserId, $"ZzzCrudRefSource_{Guid.NewGuid():N}", bodyX);

                var brokenBefore = await FindMissingPageAsync(pageRepo, pageX.Id, targetNavigation);
                Assert.NotNull(brokenBefore);

                //Creating pageY - a brand-new page whose own navigation exactly matches targetNavigation - runs
                //UpsertPage's own isNewlyCreated branch, which calls UpdateSinglePageReference(page.Navigation,
                //page.Id) specifically to fix up exactly this kind of orphaned reference
                //(EfPageRepository.UpsertPage's own remarks).
                pageY = await CreateTestPageAsync(fixture, admin.UserId, targetName, "Target page content.\r\n");

                //No longer broken.
                Assert.Null(await FindMissingPageAsync(pageRepo, pageX.Id, targetNavigation));

                //Resolved to pageY's own real Id - proven via the target's own GetBacklinkPagesPaged, which only
                //surfaces pages with a *resolved* ReferencesPageId (see this class's own Fact 1 remarks).
                var backlinks = await pageRepo.GetBacklinkPagesPaged(pageY.Id, pageNumber: 1, pageSize: 500);
                Assert.Contains(backlinks, p => p.Id == pageX.Id);
            }
            finally
            {
                if (pageX != null)
                {
                    await DeleteTestPageAsync(pageRepo, pageX.Id, admin.UserId);
                }
                if (pageY != null)
                {
                    await DeleteTestPageAsync(pageRepo, pageY.Id, admin.UserId);
                }
            }

            if (pageX != null)
            {
                Assert.Null(await pageRepo.GetPageRevisionInfoById(pageX.Id));
            }
            if (pageY != null)
            {
                Assert.Null(await pageRepo.GetPageRevisionInfoById(pageY.Id));
            }
        }

        [Fact]
        public async Task UpsertPageFile_NewFile_InsertsAtRevision1_ContentAndMetadataReadable()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var admin = await GetAdminAsync(fixture);
            var page = await CreateTestPageAsync(fixture, admin.UserId, $"ZzzCrudAttachNew_{Guid.NewGuid():N}", "Attachment host page.\r\n");

            try
            {
                var fileName = "notes.txt";
                var fileNavigation = TwNavigation.Clean(fileName);
                var contentV1 = Encoding.UTF8.GetBytes($"version-one-{Guid.NewGuid():N}");

                await UpsertTestFileAsync(pageRepo, page.Id, admin.UserId, fileName, fileNavigation, contentV1);

                //GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation's own reference SQL script
                //never selects FileRevision/FileNavigation/PageNavigation at all (only Id/PageId/Name/ContentType/
                //Size/CreatedDate) - a confirmed, pre-existing divergence from EfPageRepository's own version of
                //this method, which explicitly populates all three regardless (per its own doc comment). So only
                //the columns the reference script actually selects are asserted here; FileRevision is read from
                //GetPageFilesInfoByPageId below instead, whose own script does select PFR.Revision as FileRevision
                //for both providers.
                var info = await pageRepo.GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(page.Navigation, fileNavigation);
                Assert.NotNull(info);
                Assert.Equal(contentV1.Length, info!.Size);
                Assert.Equal(page.Id, info.PageId);
                Assert.Equal(fileName, info.Name);

                //Never cached - safe to call more than once against the same (page, file, revision) triple.
                var contentByFileRevision = await pageRepo.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(page.Navigation, fileNavigation, fileRevision: 1);
                Assert.NotNull(contentByFileRevision);
                Assert.Equal(contentV1, contentByFileRevision!.Data);

                //Exercised exactly once here - this overload IS cached under (pageNavigation, fileNavigation,
                //pageRevision), and UpsertPageFile deliberately never invalidates that cache entry (matching the
                //SQLite reference - see this class's own remarks), so it is never called again for this same triple
                //anywhere in this test.
                var contentByPageRevision = await pageRepo.GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation(page.Navigation, fileNavigation);
                Assert.NotNull(contentByPageRevision);
                Assert.Equal(contentV1, contentByPageRevision!.Data);

                Assert.Equal(1, await pageRepo.GetCountOfPageAttachmentsById(page.Id));

                var filesForPage = await pageRepo.GetPageFilesInfoByPageId(page.Id);
                var current = Assert.Single(filesForPage, f => f.FileNavigation == fileNavigation);
                Assert.Equal(1, current.FileRevision);
            }
            finally
            {
                await DeleteTestPageAsync(pageRepo, page.Id, admin.UserId);
            }

            Assert.Null(await pageRepo.GetPageRevisionInfoById(page.Id));
            //MovePageToDeletedById cascades every Pages.PageFile/Pages.PageFileRevision/Pages.PageRevisionAttachment
            //row for the page into the DeletedPages schema, then deletes them from the Pages schema (see
            //MovePageToDeletedById's own remarks) - a non-zero count here would mean an attachment somehow survived
            //its owning page's deletion.
            Assert.Equal(0, await pageRepo.GetCountOfPageAttachmentsById(page.Id));
            Assert.Empty(await pageRepo.GetPageFilesInfoByPageId(page.Id));
        }

        [Fact]
        public async Task UpsertPageFile_ReuploadSameContent_HashBasedDedup_DoesNotBumpFileRevision()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var admin = await GetAdminAsync(fixture);
            var page = await CreateTestPageAsync(fixture, admin.UserId, $"ZzzCrudAttachDedup_{Guid.NewGuid():N}", "Attachment dedup host page.\r\n");

            try
            {
                var fileName = "dedup.bin";
                var fileNavigation = TwNavigation.Clean(fileName);
                var content = Encoding.UTF8.GetBytes($"identical-payload-{Guid.NewGuid():N}");

                await UpsertTestFileAsync(pageRepo, page.Id, admin.UserId, fileName, fileNavigation, content);
                //FileRevision is read via GetPageFilesInfoByPageId, not
                //GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation - see this class's own
                //Fact 4 remarks for the confirmed, pre-existing provider divergence in the latter's own reference
                //SQL script (it never selects FileRevision at all).
                var filesAfterFirstUpload = await pageRepo.GetPageFilesInfoByPageId(page.Id);
                var currentAfterFirstUpload = Assert.Single(filesAfterFirstUpload, f => f.FileNavigation == fileNavigation);
                Assert.Equal(1, currentAfterFirstUpload.FileRevision);

                //Re-upload the exact same bytes - UpsertPageFile's own CRC32 hash comparison against the file
                //revision currently attached to the page's current revision finds no difference, so this is a
                //no-op: no new Pages.PageFileRevision row, no Pages.PageFile.Revision bump
                //(EfPageRepository.UpsertPageFile's own remarks).
                await UpsertTestFileAsync(pageRepo, page.Id, admin.UserId, fileName, fileNavigation, content);
                var filesAfterReupload = await pageRepo.GetPageFilesInfoByPageId(page.Id);
                var currentAfterReupload = Assert.Single(filesAfterReupload, f => f.FileNavigation == fileNavigation);
                Assert.Equal(1, currentAfterReupload.FileRevision);

                //Only one revision was ever actually recorded.
                var revisions = await pageRepo.GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged(page.Navigation, fileNavigation, pageNumber: 1);
                Assert.Single(revisions);
            }
            finally
            {
                await DeleteTestPageAsync(pageRepo, page.Id, admin.UserId);
            }

            Assert.Null(await pageRepo.GetPageRevisionInfoById(page.Id));
            Assert.Equal(0, await pageRepo.GetCountOfPageAttachmentsById(page.Id));
        }

        [Fact]
        public async Task UpsertPageFile_ReuploadDifferentContent_BumpsFileRevision_PreservesPreviousRevisionContent()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var admin = await GetAdminAsync(fixture);
            var page = await CreateTestPageAsync(fixture, admin.UserId, $"ZzzCrudAttachBump_{Guid.NewGuid():N}", "Attachment revision-bump host page.\r\n");

            try
            {
                var fileName = "bump.bin";
                var fileNavigation = TwNavigation.Clean(fileName);
                var contentV1 = Encoding.UTF8.GetBytes($"payload-version-one-{Guid.NewGuid():N}");
                var contentV2 = Encoding.UTF8.GetBytes($"payload-version-two-totally-different-{Guid.NewGuid():N}");

                await UpsertTestFileAsync(pageRepo, page.Id, admin.UserId, fileName, fileNavigation, contentV1);
                await UpsertTestFileAsync(pageRepo, page.Id, admin.UserId, fileName, fileNavigation, contentV2);

                //GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation's own Size/PageId/Name
                //columns are reliable (its own reference SQL script does select them) - only FileRevision is
                //asserted via GetPageFilesInfoByPageId below instead (see this class's own Fact 4 remarks).
                var info = await pageRepo.GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(page.Navigation, fileNavigation);
                Assert.NotNull(info);
                Assert.Equal(contentV2.Length, info!.Size);

                //Both revisions remain independently readable by their own explicit FileRevision (this overload is
                //never cached, unlike its "current page revision" sibling - see this class's own remarks) - the old
                //content was not overwritten, a new Pages.PageFileRevision row was inserted alongside it.
                var readV1 = await pageRepo.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(page.Navigation, fileNavigation, fileRevision: 1);
                Assert.NotNull(readV1);
                Assert.Equal(contentV1, readV1!.Data);

                var readV2 = await pageRepo.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(page.Navigation, fileNavigation, fileRevision: 2);
                Assert.NotNull(readV2);
                Assert.Equal(contentV2, readV2!.Data);

                var revisions = await pageRepo.GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged(page.Navigation, fileNavigation, pageNumber: 1);
                Assert.Equal(2, revisions.Count);

                //Only the latest (FileRevision 2) is "currently attached" - both listing members restrict to
                //FileRevision == PageFile.Revision.
                var filesForPage = await pageRepo.GetPageFilesInfoByPageId(page.Id);
                var current = Assert.Single(filesForPage, f => f.FileNavigation == fileNavigation);
                Assert.Equal(2, current.FileRevision);

                var pagedFiles = await pageRepo.GetPageFilesInfoByPageNavigationAndPageRevisionPaged(page.Navigation, pageNumber: 1);
                var pagedCurrent = Assert.Single(pagedFiles, f => f.FileNavigation == fileNavigation);
                Assert.Equal(2, pagedCurrent.FileRevision);
            }
            finally
            {
                await DeleteTestPageAsync(pageRepo, page.Id, admin.UserId);
            }

            Assert.Null(await pageRepo.GetPageRevisionInfoById(page.Id));
            Assert.Equal(0, await pageRepo.GetCountOfPageAttachmentsById(page.Id));
        }

        [Fact]
        public async Task DetachPageRevisionAttachment_OrphansFileRevision_GetOrphanedPageAttachmentsPaged_PurgeOrphanedPageAttachment_RemovesItPermanently()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;
            var admin = await GetAdminAsync(fixture);
            var page = await CreateTestPageAsync(fixture, admin.UserId, $"ZzzCrudAttachOrphan_{Guid.NewGuid():N}", "Attachment detach/orphan host page.\r\n");

            try
            {
                var fileName = "orphan.bin";
                var fileNavigation = TwNavigation.Clean(fileName);
                var content = Encoding.UTF8.GetBytes($"orphan-candidate-{Guid.NewGuid():N}");

                await UpsertTestFileAsync(pageRepo, page.Id, admin.UserId, fileName, fileNavigation, content);

                //pageFileId comes from GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation (its
                //own Id/PageId/Name/Size columns are reliable), but fileRevision is read from
                //GetPageFilesInfoByPageId instead - see this class's own Fact 4 remarks for the confirmed,
                //pre-existing provider divergence in the former's own reference SQL script (it never selects
                //FileRevision at all, so relying on it here would silently search for the wrong file revision below).
                var infoBeforeDetach = await pageRepo.GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(page.Navigation, fileNavigation);
                Assert.NotNull(infoBeforeDetach);
                var pageFileId = infoBeforeDetach!.Id;

                var filesBeforeDetach = await pageRepo.GetPageFilesInfoByPageId(page.Id);
                var currentBeforeDetach = Assert.Single(filesBeforeDetach, f => f.FileNavigation == fileNavigation);
                var fileRevision = currentBeforeDetach.FileRevision;
                Assert.Equal(1, fileRevision);

                //Removes the Pages.PageRevisionAttachment row linking the file to the page's current (and, for a
                //brand-new page, only) revision - the underlying Pages.PageFileRevision row itself is untouched,
                //but is now genuinely orphaned (no Pages.PageRevisionAttachment references it anymore).
                await pageRepo.DetachPageRevisionAttachment(page.Navigation, fileNavigation, pageRevision: 1);

                //No longer attached to the page's current revision.
                Assert.DoesNotContain(await pageRepo.GetPageFilesInfoByPageId(page.Id), f => f.FileNavigation == fileNavigation);

                var orphan = await FindOrphanedAttachmentAsync(pageRepo, pageFileId, fileRevision);
                Assert.NotNull(orphan);
                Assert.Equal(fileNavigation, orphan!.FileNavigation);
                Assert.Equal(page.Navigation, orphan.PageNavigation);

                //Purges just this one orphaned file revision (and its owning Pages.PageFile row, since this was its
                //only revision - PurgeOrphanedPageAttachment's own remarks) - deliberately not the unscoped
                //PurgeOrphanedPageAttachments() bulk overload (see this class's own remarks).
                await pageRepo.PurgeOrphanedPageAttachment(pageFileId, fileRevision);

                Assert.Null(await pageRepo.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(page.Navigation, fileNavigation, fileRevision));
                Assert.Null(await FindOrphanedAttachmentAsync(pageRepo, pageFileId, fileRevision));
                Assert.Equal(0, await pageRepo.GetCountOfPageAttachmentsById(page.Id));
            }
            finally
            {
                await DeleteTestPageAsync(pageRepo, page.Id, admin.UserId);
            }

            Assert.Null(await pageRepo.GetPageRevisionInfoById(page.Id));
            Assert.Equal(0, await pageRepo.GetCountOfPageAttachmentsById(page.Id));
        }
    }
}
