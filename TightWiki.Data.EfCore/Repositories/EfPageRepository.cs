using DuoVia.FuzzyStrings;
using Microsoft.EntityFrameworkCore;
using NTDLS.Helpers;
using TightWiki.Library.Caching;
using TightWiki.Library.Security;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;
using DeletedPagesEntities = TightWiki.Data.EfCore.Entities.DeletedPages;
using DeletedPageRevisionsEntities = TightWiki.Data.EfCore.Entities.DeletedPageRevisions;
using PagesEntities = TightWiki.Data.EfCore.Entities.Pages;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Data.EfCore.Repositories
{
    /// <summary>
    /// Provider-agnostic (SQL Server/Postgres, per Database-Providers-Plan.md chapter 3) LINQ-over-EF-Core
    /// implementation of <see cref="ITwPageRepository"/>. Lives in the shared <c>TightWiki.Data.EfCore</c>
    /// project rather than a per-provider driver project, for the same reason as <see cref="EfConfigurationRepository"/>/
    /// <see cref="EfLoggingRepository"/>/<see cref="EfEmojiRepository"/>/<see cref="EfStatisticsRepository"/> (see
    /// those classes' doc comments): plain LINQ against <see cref="TightWikiDbContext"/> needs no provider-specific
    /// code here at all. Originally landed as a <c>SqlServerPageRepository</c> stub under
    /// <c>TightWiki.Data.EfCore.SqlServer/Repositories/</c> in phase 2a.1; moved here (still a stub) in phase 2b.1.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Complete as of phase 2b.8 (Database-Providers-Plan.md) - all 86 interface members are implemented for
    /// real; none throw <see cref="NotImplementedException"/> anymore. The final 15 - the most complex remaining
    /// category, transactional row-moves between the Pages/DeletedPages/DeletedPageRevisions schemas plus two
    /// small metadata-read leftovers - (<see cref="GetPageProcessingInstructionsByPageId"/>,
    /// <see cref="GetPageTagsById"/>, <see cref="TruncateAllPageRevisions"/>,
    /// <see cref="RestoreDeletedPageByPageId"/>, <see cref="MovePageRevisionToDeletedById"/>,
    /// <see cref="MovePageToDeletedById"/>, <see cref="PurgeDeletedPageByPageId"/>, <see cref="PurgeDeletedPages"/>,
    /// <see cref="GetDeletedPageById"/>, <see cref="GetDeletedPageRevisionsByIdPaged"/>,
    /// <see cref="PurgeDeletedPageRevisions"/>, <see cref="PurgeDeletedPageRevisionsByPageId"/>,
    /// <see cref="PurgeDeletedPageRevisionByPageIdAndRevision"/>,
    /// <see cref="RestoreDeletedPageRevisionByPageIdAndRevision"/>, and <see cref="GetDeletedPageRevisionById"/> -
    /// see <see cref="MovePageToDeletedById"/>'s and <see cref="RestoreDeletedPageByPageId"/>'s remarks for what
    /// exactly gets copied/discarded on each side of a soft-delete/restore, and <see cref="PurgeDeletedPages"/>'s
    /// remarks for a confirmed, deliberately-not-"fixed" asymmetry between the single-page and purge-all
    /// reference scripts) landed in phase 2b.8, now that a single consolidated <see cref="TightWikiDbContext"/>
    /// makes what used to be cross-database <c>ATTACH</c>-based row copies (Pages/DeletedPages/
    /// DeletedPageRevisions are three separate physical SQLite files in the reference) a plain
    /// read-then-insert-then-delete within one EF Core transaction.
    /// </para>
    /// <para>
    /// The history of how the rest got here: the first 11 (autocomplete, page-cache flushing, page comments,
    /// current-page-editors) were implemented for real in phase 2b.2; a further 19 page/revision metadata-read
    /// members (<see cref="GetPageRevisionInfoById"/>, <see cref="GetPageNavigationByPageId"/>,
    /// <see cref="GetTopRecentlyModifiedPagesInfoByUserId"/>, <see cref="GetTopRecentlyModifiedPagesInfo"/>,
    /// <see cref="GetTopRecentlyCreatedPagesInfo"/>, <see cref="GetTopViewedPagesInfo"/>,
    /// <see cref="GetTopEditedPagesInfo"/>, <see cref="GetPageRevisionsInfoByNavigationPaged"/>,
    /// <see cref="GetCurrentPageRevision"/>, <see cref="GetLimitedPageInfoByIdAndRevision"/>,
    /// <see cref="GetPageInfoByNavigation"/>, <see cref="GetPageRevisionCountByPageId"/>,
    /// <see cref="GetPageNextRevision"/>, <see cref="GetPagePreviousRevision"/>, <see cref="GetPageRevisionById"/>,
    /// <see cref="GetLatestPageRevisionById"/>, both <see cref="GetPageRevisionByNavigation(TwNamespaceNavigation, int?)"/>
    /// overloads, and <see cref="GetCountOfPageAttachmentsById"/> - see each method's own doc comment for the
    /// specific SQLite script it mirrors) landed in phase 2b.3. A further 10 bulk/paged-listing members
    /// (<see cref="GetMissingPagesPaged"/>, <see cref="GetAllPagesByInstructionPaged"/>,
    /// <see cref="GetAllNamespacePagesPaged"/>, <see cref="GetAllPagesPaged"/>, <see cref="GetAllDeletedPagesPaged"/>,
    /// <see cref="GetAllNamespacesPaged"/>, <see cref="GetAllNamespaces"/>, <see cref="GetAllPages"/>,
    /// <see cref="GetAllTemplatePages"/>, and <see cref="GetAllFeatureTemplates"/> - see
    /// <see cref="GetAllPagesPaged"/>'s remarks for the <c>TempPageIds</c>/<c>list.Contains(...)</c> temp-table
    /// replacement pattern introduced here and reused in later phases) landed in phase 2b.4. A further 15
    /// search/tags/tokens members - the most complex category in the interface -
    /// (<see cref="PageSearch"/>, <see cref="PageSearchPaged"/>, <see cref="GetSimilarPagesPaged"/>,
    /// <see cref="GetRelatedPagesPaged"/>, <see cref="GetBacklinkPagesPaged"/>, <see cref="GetDeletedPageIdsByTokens"/>,
    /// <see cref="GetPageIdsByTokens"/>, <see cref="GetSearchTokensByPageId"/>, <see cref="SavePageSearchTokens"/>,
    /// <see cref="ParsePageTokens"/>, <see cref="GetAssociatedTags"/>, <see cref="GetPageInfoByNamespaces"/>,
    /// <see cref="GetPageInfoByTags"/>, <see cref="GetPageInfoByTag"/>, and <see cref="UpdatePageTags"/> - see
    /// <see cref="GetFuzzyPageSearchTokens"/>'s remarks for the <c>TempSearchTerms</c>/fuzzy-fan-out substitution
    /// and <see cref="ComputeParsedPageTokens"/> for the DuoVia.FuzzyStrings Double Metaphone scoring, which - like
    /// the SQLite reference - runs entirely in C#, not SQL) landed in phase 2b.5. A further 5 CRUD/upsert members -
    /// one of the most complex categories in the interface, transactional page save/orchestration -
    /// (<see cref="UpsertPage"/>, <see cref="RefreshPageMetadata"/>, <see cref="UpdatePageProcessingInstructions"/>,
    /// <see cref="UpdateSinglePageReference"/>, and <see cref="UpdatePageReferences"/> - see <see cref="SavePage"/>'s
    /// remarks for the hash-based change detection/revision-bumping logic behind <see cref="UpsertPage"/>, and
    /// <see cref="UpdatePageReferences"/>'s remarks for the <c>TempReferences</c> replacement and a confirmed,
    /// deliberately-not-reproduced bug in the SQLite reference script) landed in phase 2b.6. A further 11 page
    /// file/attachment members (<see cref="DetachPageRevisionAttachment"/>, <see cref="GetOrphanedPageAttachmentsPaged"/>,
    /// <see cref="PurgeOrphanedPageAttachments"/>, <see cref="PurgeOrphanedPageAttachment"/>,
    /// <see cref="GetPageFilesInfoByPageNavigationAndPageRevisionPaged"/>,
    /// <see cref="GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation"/>,
    /// <see cref="GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation"/>,
    /// <see cref="GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation"/>,
    /// <see cref="GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged"/>,
    /// <see cref="GetPageFilesInfoByPageId"/>, and <see cref="UpsertPageFile"/> - see <see cref="UpsertPageFile"/>'s
    /// remarks for the hash-based change detection/revision-bumping logic behind file attachments (structurally
    /// analogous to <see cref="SavePage"/>'s own page-revision logic), and
    /// <see cref="GetPageFileInfoByFileNavigation"/>/<see cref="GetPageCurrentRevisionAttachmentByFileNavigation"/>
    /// for the two SQLite-only, non-interface helpers <c>UpsertPageFile</c> depends on) landed in phase 2b.7. The
    /// final 15 members landed in phase 2b.8 - see the opening paragraph above.
    /// </para>
    /// <para>
    /// Takes a <see cref="Func{TightWikiDbContext}"/> rather than an injected context instance, mirroring
    /// <see cref="EfConfigurationRepository"/>/<see cref="EfLoggingRepository"/>/<see cref="EfEmojiRepository"/>/
    /// <see cref="EfStatisticsRepository"/> (see <see cref="EfConfigurationRepository"/>'s doc comment) -
    /// <see cref="SqlServer.SqlServerDatabaseManager"/> passes its own <c>CreateDbContext</c> method group in as
    /// that delegate. Also takes an <see cref="ITwConfigurationRepository"/> instance directly (not another
    /// <see cref="Func{TResult}"/>), mirroring the SQLite reference constructor's own
    /// <c>ConfigurationRepository configurationRepository</c> parameter and the same pattern already used by
    /// <see cref="EfLoggingRepository"/>/<see cref="EfEmojiRepository"/>/<see cref="EfStatisticsRepository"/> -
    /// <see cref="GetPageCommentsPaged"/> is the only phase-2b.2 member that needs it, to read the "Pagination
    /// Size" customization setting.
    /// </para>
    /// </remarks>
    public sealed class EfPageRepository : ITwPageRepository
    {
        private readonly Func<TightWikiDbContext> _createContext;
        private readonly ITwConfigurationRepository _configurationRepository;

        public EfPageRepository(Func<TightWikiDbContext> createContext, ITwConfigurationRepository configurationRepository)
        {
            _createContext = createContext;
            _configurationRepository = configurationRepository;
        }

        /// <summary>
        /// Mirrors AutoCompletePage.sql: pages whose <see cref="TwPage.Name"/> contains
        /// <paramref name="searchText"/> (an empty string, matching everything, if null - same as the reference's
        /// <c>searchText ?? string.Empty</c>), ordered by Name, capped at 25 rows. No caching, matching the SQLite
        /// reference. <see cref="TwPage.Namespace"/> is deliberately left unset here even though the reference SQL
        /// also selects the (persisted, redundant) Namespace column - <see cref="TwPage.Namespace"/> is a get-only
        /// property computed from <see cref="TwPage.Name"/>, so Dapper silently drops that column too when
        /// mapping the SQLite reference's result set onto the same type.
        /// </summary>
        public async Task<List<TwPage>> AutoCompletePage(string? searchText)
        {
            using var context = _createContext();

            var text = searchText ?? string.Empty;

            return await context.Pages_Pages
                .Where(p => p.Name.Contains(text))
                .OrderBy(p => p.Name)
                .Take(25)
                .Select(p => new TwPage
                {
                    Id = p.Id,
                    Navigation = p.Navigation,
                    Name = p.Name,
                })
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors AutoCompleteNamespace.sql: distinct <see cref="PagesEntities.Page.Namespace"/> values
        /// containing <paramref name="searchText"/> (an empty string, matching everything, if null), capped at 25
        /// rows. No caching, matching the SQLite reference. Ordered by the namespace value itself, rather than
        /// literally reproducing the reference script's "SELECT DISTINCT [Namespace] ... ORDER BY [Name]" - SQLite
        /// tolerates ordering by a column outside a DISTINCT projection, but that construct has no portable
        /// LINQ/SQL Server equivalent, and once only Namespace is projected there is no well-defined per-namespace
        /// "Name" left to order by anyway (a namespace groups many pages/Names).
        /// </summary>
        public async Task<List<string>> AutoCompleteNamespace(string? searchText)
        {
            using var context = _createContext();

            var text = searchText ?? string.Empty;

            return await context.Pages_Pages
                .Where(p => p.Namespace.Contains(text))
                .Select(p => p.Namespace)
                .Distinct()
                .OrderBy(n => n)
                .Take(25)
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors GetPageRevisionInfoById.sql: an inner join of Pages.Page to Pages.PageRevision for
        /// <paramref name="pageId"/>, at <paramref name="revision"/> (falling back to the page's current
        /// <see cref="PagesEntities.Page.Revision"/> when null - the script's own <c>COALESCE(@Revision, P.Revision)</c>).
        /// <see cref="TwPage.ModifiedByUserId"/>/<see cref="TwPage.ModifiedDate"/> are populated from the
        /// <see cref="PagesEntities.PageRevision"/> row (<c>PR.ModifiedByUserId</c>/<c>PR.ModifiedDate</c>), not
        /// from <see cref="PagesEntities.Page"/> itself - matching the script exactly (contrast with
        /// <see cref="GetPageRevisionById"/>, whose reference script pulls those two columns from <c>P</c>
        /// instead). No caching, matching the SQLite reference.
        /// </summary>
        public async Task<TwPage?> GetPageRevisionInfoById(int pageId, int? revision = null)
        {
            using var context = _createContext();

            return await (from p in context.Pages_Pages
                           join pr in context.Pages_PageRevisions on p.Id equals pr.PageId
                           where p.Id == pageId && pr.Revision == (revision ?? p.Revision)
                           select new TwPage
                           {
                               Id = p.Id,
                               Name = p.Name,
                               Description = p.Description,
                               Revision = pr.Revision,
                               Navigation = p.Navigation,
                               CreatedByUserId = p.CreatedByUserId,
                               CreatedDate = p.CreatedDate,
                               ModifiedByUserId = pr.ModifiedByUserId,
                               ModifiedDate = pr.ModifiedDate,
                           }).SingleOrDefaultAsync();
        }

        /// <summary>
        /// Mirrors GetPageProcessingInstructionsByPageId.sql: every Pages.PageProcessingInstruction row for
        /// <paramref name="pageId"/>, wrapped in a <see cref="TwProcessingInstructionCollection"/>. Cached under
        /// <see cref="MemCache.Category.Page"/>, same cache key shape (pageId) as the SQLite reference.
        /// </summary>
        public async Task<TwProcessingInstructionCollection> GetPageProcessingInstructionsByPageId(int pageId)
        {
            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.Page, [pageId]);

            return (await MemCache.AddOrGetAsync(cacheKey, async () =>
            {
                using var context = _createContext();

                var instructions = await context.Pages_PageProcessingInstructions
                    .Where(pi => pi.PageId == pageId)
                    .Select(pi => new TwProcessingInstruction
                    {
                        PageId = pi.PageId,
                        Instruction = pi.Instruction,
                    })
                    .ToListAsync();

                return new TwProcessingInstructionCollection
                {
                    Collection = instructions,
                };
            })).EnsureNotNull();
        }

        /// <summary>
        /// Mirrors GetPageTagsById.sql: every Pages.PageTag row for <paramref name="pageId"/>.
        /// <see cref="TwPageTag.Id"/>/<see cref="TwPageTag.PageId"/> are deliberately left unset (0) here - a
        /// literal quirk of the reference script, which selects only <c>PT.Tag, PT.Navigation</c> (neither an Id
        /// nor PageId column), and <see cref="TwPageTag"/> has no "Navigation" property for the second column to
        /// land on either - the same "Dapper silently drops what it can't map, leaves what it never received at
        /// its default" pattern as <see cref="AutoCompletePage"/>'s own doc comment. Confirmed to have no
        /// observable behavioral impact: this method's one caller (<c>TwSessionState.RefreshAsync</c>) only reads
        /// <see cref="TwPageTag.Tag"/> off the result. Cached under <see cref="MemCache.Category.Page"/>, same
        /// cache key shape (pageId) as the SQLite reference.
        /// </summary>
        public async Task<List<TwPageTag>> GetPageTagsById(int pageId)
        {
            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.Page, [pageId]);

            return (await MemCache.AddOrGetAsync(cacheKey, async () =>
            {
                using var context = _createContext();

                return await context.Pages_PageTags
                    .Where(t => t.PageId == pageId)
                    .Select(t => new TwPageTag
                    {
                        Tag = t.Tag,
                    })
                    .ToListAsync();
            })).EnsureNotNull();
        }

        /// <summary>
        /// Mirrors GetPageRevisionsInfoByNavigationPaged.sql: every Pages.PageRevision row for the page matching
        /// <paramref name="navigation"/>, inner-joined to its owning Pages.Page, LEFT OUTER JOINed to Users.Profile
        /// for both the revision's own modifier (<c>ModifiedUser</c>) and the page's original creator
        /// (<c>Createduser</c>) - via the existing <see cref="PagesEntities.PageRevision.ModifiedByUser"/>/
        /// <see cref="PagesEntities.Page.CreatedByUser"/> navigations rather than a raw cross-database
        /// <c>ATTACH</c> (the script's own <c>o.Attach("users.db", "users_db")</c>), since both schemas already
        /// live in the same <see cref="TightWikiDbContext"/>. <see cref="TwPageRevision.HigherRevisionCount"/> is
        /// computed the same way as the script's own correlated subquery (count of sibling revisions with a
        /// higher <see cref="PagesEntities.PageRevision.Revision"/>). <paramref name="pageSize"/> defaults to the
        /// "Pagination Size" customization setting, same as the reference, and
        /// <see cref="TwPageRevision.PaginationPageCount"/> is computed via the reference's own ceiling-division
        /// formula against the total (unpaginated) revision count for the page. Ordering mirrors
        /// <c>RepositoryHelpers.TransposeOrderby</c> against the script's <c>--CONFIG::</c> mapping
        /// ("Revision"/"ModifiedBy"/"ModifiedDate"/"Page"): no <paramref name="orderBy"/> falls back to the
        /// script's own un-transposed "ORDER BY PR.Revision DESC"; an unrecognized <paramref name="orderBy"/>
        /// throws, same as <c>RepositoryHelpers.TransposeOrderby</c>'s "No order by mapping..." exception (see
        /// <see cref="EfStatisticsRepository.GetPageStatisticsPaged"/> for the same pattern); direction is
        /// ascending only when <paramref name="orderByDirection"/> is exactly "asc" (case-insensitively),
        /// descending for anything else including null.
        /// </summary>
        public async Task<List<TwPageRevision>> GetPageRevisionsInfoByNavigationPaged(string navigation, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            pageSize ??= await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            using var context = _createContext();

            var joined = from p in context.Pages_Pages
                         join pr in context.Pages_PageRevisions on p.Id equals pr.PageId
                         where p.Navigation == navigation
                         select new { p, pr };

            var totalCount = await joined.CountAsync();
            var paginationPageCount = (totalCount + (pageSize.Value - 1)) / pageSize.Value;

            bool ascending = string.Equals(orderByDirection, "asc", StringComparison.InvariantCultureIgnoreCase);

            var ordered = string.IsNullOrEmpty(orderBy)
                ? joined.OrderByDescending(x => x.pr.Revision)
                : orderBy.ToUpperInvariant() switch
                {
                    "REVISION" => ascending ? joined.OrderBy(x => x.pr.Revision) : joined.OrderByDescending(x => x.pr.Revision),
                    "MODIFIEDBY" => ascending
                        ? joined.OrderBy(x => x.pr.ModifiedByUser != null ? x.pr.ModifiedByUser.AccountName : null)
                        : joined.OrderByDescending(x => x.pr.ModifiedByUser != null ? x.pr.ModifiedByUser.AccountName : null),
                    "MODIFIEDDATE" => ascending ? joined.OrderBy(x => x.pr.ModifiedDate) : joined.OrderByDescending(x => x.pr.ModifiedDate),
                    "PAGE" => ascending ? joined.OrderBy(x => x.pr.Name) : joined.OrderByDescending(x => x.pr.Name),
                    _ => throw new InvalidOperationException(
                        $"No order by mapping was found in 'GetPageRevisionsInfoByNavigationPaged.sql' for the field '{orderBy}'."),
                };

            return await ordered
                .Skip((pageNumber - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .Select(x => new TwPageRevision
                {
                    PageId = x.p.Id,
                    Name = x.pr.Name,
                    Description = x.pr.Description,
                    Revision = x.pr.Revision,
                    HighestRevision = x.p.Revision,
                    ChangeSummary = x.pr.ChangeSummary ?? string.Empty,
                    Navigation = x.p.Navigation,
                    CreatedByUserId = x.p.CreatedByUserId,
                    CreatedByUserName = x.p.CreatedByUser != null ? (x.p.CreatedByUser.AccountName ?? string.Empty) : string.Empty,
                    CreatedDate = x.p.CreatedDate,
                    ModifiedByUserId = x.pr.ModifiedByUserId,
                    ModifiedByUserName = x.pr.ModifiedByUser != null ? (x.pr.ModifiedByUser.AccountName ?? string.Empty) : string.Empty,
                    ModifiedDate = x.pr.ModifiedDate,
                    HigherRevisionCount = context.Pages_PageRevisions.Count(ipr => ipr.PageId == x.p.Id && ipr.Revision > x.pr.Revision),
                    PaginationPageSize = pageSize.Value,
                    PaginationPageCount = paginationPageCount,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors GetTopRecentlyModifiedPagesInfoByUserId.sql: every Pages.Page row modified by
        /// <paramref name="userId"/>, ordered by <see cref="PagesEntities.Page.ModifiedDate"/> descending then
        /// <see cref="PagesEntities.Page.Name"/> ascending, capped at <paramref name="topCount"/>.
        /// <see cref="TwPageRevision.PageId"/> is deliberately left unset (0) here - a literal quirk of the
        /// reference script, which selects <c>P.Id</c> but <see cref="TwPageRevision"/> has no "Id" property (only
        /// "PageId"), so Dapper silently drops that column when mapping the reference's result set onto the same
        /// type (the one caller, <c>ProfileController</c>, only reads <see cref="TwPageRevision.Navigation"/>/
        /// <see cref="TwPageRevision.Revision"/> off the result, never <see cref="TwPageRevision.PageId"/>, so this
        /// has no observable behavioral impact).
        /// </summary>
        public async Task<List<TwPageRevision>> GetTopRecentlyModifiedPagesInfoByUserId(Guid userId, int topCount)
        {
            using var context = _createContext();

            return await context.Pages_Pages
                .Where(p => p.ModifiedByUserId == userId)
                .OrderByDescending(p => p.ModifiedDate)
                .ThenBy(p => p.Name)
                .Take(topCount)
                .Select(p => new TwPageRevision
                {
                    Name = p.Name,
                    Description = p.Description,
                    Revision = p.Revision,
                    Navigation = p.Navigation,
                    CreatedByUserId = p.CreatedByUserId,
                    CreatedDate = p.CreatedDate,
                    ModifiedByUserId = p.ModifiedByUserId,
                    ModifiedDate = p.ModifiedDate,
                })
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors GetPageNavigationByPageId.sql: the <see cref="PagesEntities.Page.Navigation"/> of the page
        /// matching <paramref name="pageId"/>, or null if no such page exists (the script's
        /// <c>ExecuteScalarAsync&lt;string&gt;</c> over a query with no matching row). Same query
        /// <see cref="FlushPageCache"/> runs inline for the same reason (see that method's doc comment).
        /// </summary>
        public async Task<string?> GetPageNavigationByPageId(int pageId)
        {
            using var context = _createContext();

            return await context.Pages_Pages
                .Where(p => p.Id == pageId)
                .Select(p => (string?)p.Navigation)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Mirrors GetTopRecentlyModifiedPagesInfo.sql: every Pages.Page row inner-joined to the
        /// Pages.PageRevision row matching its own current <see cref="PagesEntities.Page.Revision"/> (so
        /// <see cref="TwPage.ModifiedByUserId"/>/<see cref="TwPage.ModifiedDate"/> come from that current
        /// revision row, not from <see cref="PagesEntities.Page"/> itself), ordered by
        /// <see cref="PagesEntities.Page.ModifiedDate"/> descending then <see cref="PagesEntities.Page.Name"/>
        /// ascending, capped at <paramref name="topCount"/>.
        /// </summary>
        public async Task<List<TwPage>> GetTopRecentlyModifiedPagesInfo(int topCount)
        {
            using var context = _createContext();

            return await context.Pages_Pages
                .Join(context.Pages_PageRevisions,
                    p => new { p.Id, p.Revision },
                    pr => new { Id = pr.PageId, pr.Revision },
                    (p, pr) => new { p, pr })
                .OrderByDescending(x => x.p.ModifiedDate)
                .ThenBy(x => x.p.Name)
                .Take(topCount)
                .Select(x => new TwPage
                {
                    Id = x.p.Id,
                    Name = x.p.Name,
                    Description = x.p.Description,
                    Revision = x.p.Revision,
                    Navigation = x.p.Navigation,
                    CreatedByUserId = x.p.CreatedByUserId,
                    CreatedDate = x.p.CreatedDate,
                    ModifiedByUserId = x.pr.ModifiedByUserId,
                    ModifiedDate = x.pr.ModifiedDate,
                })
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors GetTopRecentlyCreatedPagesInfo.sql: every Pages.Page row ordered by
        /// <see cref="PagesEntities.Page.CreatedDate"/> descending then <see cref="PagesEntities.Page.Name"/>
        /// ascending, capped at <paramref name="topCount"/>. The script's own <c>IFNULL(P.ModifiedDate,
        /// P.CreatedDate)</c> is not reproduced as a client-side fallback - <see cref="PagesEntities.Page.ModifiedDate"/>
        /// is a <c>NOT NULL</c> column in the real schema (verified against the live SQLite schema), so the
        /// column value itself is used directly.
        /// </summary>
        public async Task<List<TwPage>> GetTopRecentlyCreatedPagesInfo(int topCount)
        {
            using var context = _createContext();

            return await context.Pages_Pages
                .OrderByDescending(p => p.CreatedDate)
                .ThenBy(p => p.Name)
                .Take(topCount)
                .Select(p => new TwPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Revision = p.Revision,
                    Navigation = p.Navigation,
                    CreatedByUserId = p.CreatedByUserId,
                    CreatedDate = p.CreatedDate,
                    ModifiedByUserId = p.ModifiedByUserId,
                    ModifiedDate = p.ModifiedDate,
                })
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors GetTopViewedPagesInfo.sql: every Statistics.PageStatistics row inner-joined to its owning
        /// Pages.Page (via the existing, required <see cref="Statistics.PageStatistic.Page"/> navigation rather
        /// than the script's own cross-database <c>o.Attach("statistics.db", "statistics_db")</c>, since both
        /// schemas already live in the same <see cref="TightWikiDbContext"/>), ordered by
        /// <see cref="Statistics.PageStatistic.TotalViewCount"/> descending then <see cref="PagesEntities.Page.Name"/>
        /// ascending, capped at <paramref name="topCount"/>.
        /// </summary>
        public async Task<List<TwPage>> GetTopViewedPagesInfo(int topCount)
        {
            using var context = _createContext();

            return await context.PageStatistics
                .OrderByDescending(ps => ps.TotalViewCount)
                .ThenBy(ps => ps.Page.Name)
                .Take(topCount)
                .Select(ps => new TwPage
                {
                    Id = ps.Page.Id,
                    Name = ps.Page.Name,
                    Description = ps.Page.Description,
                    Revision = ps.Page.Revision,
                    Navigation = ps.Page.Navigation,
                    CreatedByUserId = ps.Page.CreatedByUserId,
                    CreatedDate = ps.Page.CreatedDate,
                    ModifiedByUserId = ps.Page.ModifiedByUserId,
                    ModifiedDate = ps.Page.ModifiedDate,
                    TotalViewCount = ps.TotalViewCount,
                })
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors GetTopEditedPagesInfo.sql: every Pages.Page row ordered by
        /// <see cref="PagesEntities.Page.Revision"/> descending then <see cref="PagesEntities.Page.Name"/>
        /// ascending, capped at <paramref name="topCount"/> - "most edited" meaning "highest revision number".
        /// See <see cref="GetTopRecentlyCreatedPagesInfo"/>'s doc comment for why the script's own
        /// <c>IFNULL(P.ModifiedDate, P.CreatedDate)</c> is not reproduced.
        /// </summary>
        public async Task<List<TwPage>> GetTopEditedPagesInfo(int topCount)
        {
            using var context = _createContext();

            return await context.Pages_Pages
                .OrderByDescending(p => p.Revision)
                .ThenBy(p => p.Name)
                .Take(topCount)
                .Select(p => new TwPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Revision = p.Revision,
                    Navigation = p.Navigation,
                    CreatedByUserId = p.CreatedByUserId,
                    CreatedDate = p.CreatedDate,
                    ModifiedByUserId = p.ModifiedByUserId,
                    ModifiedDate = p.ModifiedDate,
                })
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors GetFuzzyPageSearchTokens.sql: candidate Pages.PageToken rows whose
        /// <see cref="PagesEntities.PageToken.DoubleMetaphone"/> matches one of <paramref name="tokens"/>'s
        /// (deduplicated - <see cref="TwPageToken.Equals"/> compares <see cref="TwPageToken.Token"/>
        /// case-insensitively) Double Metaphone codes, fetched via <c>Contains(...)</c> (the same
        /// <c>CreateTempTableFrom</c>/temp-table replacement pattern as <see cref="GetAllPagesPaged"/>'s remarks -
        /// here against <see cref="PagesEntities.PageToken.DoubleMetaphone"/> rather than an id list), then joined
        /// against <paramref name="tokens"/> and aggregated client-side rather than in the database.
        /// </summary>
        /// <remarks>
        /// The reference script's own join predicate is <c>ST.Token != T.Token AND ST.DoubleMetaphone =
        /// T.DoubleMetaphone</c> - a single Pages.PageToken row can join to <i>multiple</i> distinct search terms
        /// that share its phonetic code but differ in literal spelling, and each such pairing contributes its own
        /// row to the SQL aggregate (so <c>SUM(T.Weight)</c> can add the same token's weight more than once, while
        /// <c>COUNT(DISTINCT T.DoubleMetaphone)</c> still counts each phonetic code only once for the match
        /// ratio). This fan-out is a genuine multi-way join against a client-side list (not just a membership
        /// test), which has no safe, guaranteed-translatable EF Core/SQL Server LINQ equivalent - unlike the
        /// simple <c>list.Contains(...)</c> substitution used everywhere else (Database-Providers-Plan.md chapter
        /// 4.4/8). So this method fetches only the DoubleMetaphone-filtered candidate rows from the database (a
        /// translatable, bounded <c>Contains</c> query) and reproduces the fan-out join and aggregation itself in
        /// memory, replicating the SQL script's arithmetic (including the weight double-counting) exactly.
        /// </remarks>
        private async Task<List<TwPageSearchToken>> GetFuzzyPageSearchTokens(List<TwPageToken> tokens, double minimumMatchScore)
        {
            var searchTerms = tokens.Distinct().ToList();
            var searchTermDoubleMetaphones = searchTerms.Select(t => t.DoubleMetaphone).Distinct().ToList();
            var tokenCount = tokens.Count;

            using var context = _createContext();

            var candidates = await context.Pages_PageTokens
                .Where(t => searchTermDoubleMetaphones.Contains(t.DoubleMetaphone))
                .Select(t => new { t.PageId, t.Token, t.DoubleMetaphone, t.Weight })
                .ToListAsync();

            var joinedRows =
                from pt in candidates
                from st in searchTerms
                where !string.Equals(st.Token, pt.Token, StringComparison.OrdinalIgnoreCase)
                      && string.Equals(st.DoubleMetaphone, pt.DoubleMetaphone, StringComparison.OrdinalIgnoreCase)
                select pt;

            return joinedRows
                .GroupBy(pt => pt.PageId)
                .Select(g => new TwPageSearchToken
                {
                    PageId = g.Key,
                    Match = g.Select(pt => pt.DoubleMetaphone).Distinct(StringComparer.OrdinalIgnoreCase).Count() / (tokenCount + 0.0),
                    Weight = g.Sum(pt => pt.Weight) * 1.0,
                    //No weight benefit on score for fuzzy matching, matching GetFuzzyPageSearchTokens.sql.
                    Score = g.Select(pt => pt.DoubleMetaphone).Distinct(StringComparer.OrdinalIgnoreCase).Count() / (tokenCount + 0.0),
                })
                .Where(t => t.Score >= minimumMatchScore)
                .OrderByDescending(t => t.Score)
                .Take(250)
                .ToList();
        }

        /// <summary>
        /// Mirrors GetExactPageSearchTokens.sql: Pages.PageToken rows whose <see cref="PagesEntities.PageToken.Token"/>
        /// exactly matches one of <paramref name="tokens"/>'s (deduplicated) token strings, fetched via
        /// <c>Contains(...)</c> (the <c>TempSearchTerms</c> replacement, same pattern as
        /// <see cref="GetFuzzyPageSearchTokens"/>), grouped by page and scored server-side - unlike the fuzzy
        /// variant, an exact match join has no fan-out (Pages.PageToken's composite primary key is
        /// (PageId, Token), so at most one row per page can match a given token string), so the aggregation
        /// translates safely to a single grouped SQL query.
        /// </summary>
        private async Task<List<TwPageSearchToken>> GetExactPageSearchTokens(List<TwPageToken> tokens, double minimumMatchScore)
        {
            var searchTermTokens = tokens.Distinct().Select(t => t.Token).ToList();
            var tokenCount = tokens.Count;

            using var context = _createContext();

            return await context.Pages_PageTokens
                .Where(t => searchTermTokens.Contains(t.Token))
                .GroupBy(t => t.PageId)
                .Select(g => new TwPageSearchToken
                {
                    PageId = g.Key,
                    Match = g.Count() / (tokenCount + 0.0),
                    Weight = g.Sum(x => x.Weight) * 1.5,
                    //Extra weight on score for exact matches, matching GetExactPageSearchTokens.sql.
                    Score = (g.Sum(x => x.Weight) * 1.5) * (g.Count() / (tokenCount + 0.0)),
                })
                .Where(t => t.Score >= minimumMatchScore)
                .OrderByDescending(t => t.Score)
                .Take(250)
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors <c>PageRepository.GetMeteredPageSearchTokens</c>: combines <see cref="GetExactPageSearchTokens"/>
        /// (always) with <see cref="GetFuzzyPageSearchTokens"/> (only when <paramref name="allowFuzzyMatching"/>),
        /// each run against half the "Minimum Match Score" search setting, then - when both ran - merges the two
        /// result sets by page (taking the max Match/Weight/Score per page, same as the reference's
        /// <c>GroupBy(...).Select(...Max...)</c>) and keeps only pages whose combined score still clears the full
        /// threshold. Cached under <see cref="MemCache.Category.Search"/>, same cache key shape (joined search
        /// terms + fuzzy flag) as the SQLite reference.
        /// </summary>
        private async Task<List<TwPageSearchToken>> GetMeteredPageSearchTokens(List<string> searchTerms, bool allowFuzzyMatching)
        {
            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.Search, [string.Join(',', searchTerms), allowFuzzyMatching]);

            return (await MemCache.AddOrGetAsync(cacheKey, async () =>
            {
                var minimumMatchScore = await _configurationRepository.Get<float>(TwConfigGroup.Search, "Minimum Match Score");

                var searchTokens = searchTerms.Select(o => new TwPageToken
                {
                    Token = o,
                    DoubleMetaphone = o.ToDoubleMetaphone(),
                }).ToList();

                if (allowFuzzyMatching)
                {
                    var allTokens = await GetExactPageSearchTokens(searchTokens, minimumMatchScore / 2.0);
                    var fuzzyTokens = await GetFuzzyPageSearchTokens(searchTokens, minimumMatchScore / 2.0);

                    allTokens.AddRange(fuzzyTokens);

                    return allTokens
                        .GroupBy(token => token.PageId)
                        .Where(group => group.Sum(g => g.Score) >= minimumMatchScore)
                        .Select(group => new TwPageSearchToken
                        {
                            PageId = group.Key,
                            Match = group.Max(g => g.Match),
                            Weight = group.Max(g => g.Weight),
                            Score = group.Max(g => g.Score),
                        }).ToList();
                }
                else
                {
                    return await GetExactPageSearchTokens(searchTokens, minimumMatchScore / 2.0);
                }
            })).EnsureNotNull();
        }

        /// <summary>
        /// Mirrors PageSearch.sql: every page matched by <see cref="GetMeteredPageSearchTokens"/> (the
        /// <c>TempSearchTerms</c> join replaced by fetching the matched pages via <c>Contains(...)</c> against
        /// their page IDs, then attaching each page's Match/Weight/Score client-side - the same substitution
        /// pattern as <see cref="GetAllPagesPaged"/>'s remarks), ordered by Score descending then Name then Id
        /// ascending, same as the reference. Returns an empty list immediately when <paramref name="searchTerms"/>
        /// is empty or no page clears the minimum match score, same short-circuits as the SQLite reference.
        /// </summary>
        public async Task<List<TwPage>> PageSearch(List<string> searchTerms)
        {
            if (searchTerms.Count == 0)
            {
                return new List<TwPage>();
            }

            bool allowFuzzyMatching = await _configurationRepository.Get<bool>(TwConfigGroup.Search, "Allow Fuzzy Matching");
            var meteredSearchTokens = await GetMeteredPageSearchTokens(searchTerms, allowFuzzyMatching);
            if (meteredSearchTokens.Count == 0)
            {
                return new List<TwPage>();
            }

            var scoreByPageId = meteredSearchTokens.ToDictionary(t => t.PageId);
            var pageIds = scoreByPageId.Keys.ToList();

            using var context = _createContext();

            var pages = await context.Pages_Pages
                .Where(p => pageIds.Contains(p.Id))
                .Select(p => new TwPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Navigation = p.Navigation,
                    Description = p.Description,
                    Revision = p.Revision,
                    CreatedByUserId = p.CreatedByUserId,
                    CreatedDate = p.CreatedDate,
                    ModifiedByUserId = p.ModifiedByUserId,
                    ModifiedDate = p.ModifiedDate,
                    CreatedByUserName = p.CreatedByUser != null ? (p.CreatedByUser.AccountName ?? string.Empty) : string.Empty,
                    ModifiedByUserName = p.ModifiedByUser != null ? (p.ModifiedByUser.AccountName ?? string.Empty) : string.Empty,
                })
                .ToListAsync();

            foreach (var page in pages)
            {
                var token = scoreByPageId[page.Id];
                page.Match = (decimal)token.Match;
                page.Weight = (decimal)token.Weight;
                page.Score = (decimal)token.Score;
            }

            return pages
                .OrderByDescending(p => p.Score)
                .ThenBy(p => p.Name)
                .ThenBy(p => p.Id)
                .ToList();
        }

        /// <summary>
        /// Mirrors PageSearchPaged.sql: the same match as <see cref="PageSearch"/>, but with Score rescaled to a
        /// percentage of the maximum score among matched pages (the reference's <c>(ST.Score / @MaximumScore) *
        /// 100.0</c>), paginated by <paramref name="pageSize"/> (defaulting to the "Pagination Size" customization
        /// setting) with <see cref="TwPage.PaginationPageCount"/> computed via the reference's own
        /// ceiling-division formula against the total (unpaginated) matched-page count. <paramref name="allowFuzzyMatching"/>
        /// defaults to the "Allow Fuzzy Matching" search setting, same as the reference.
        /// </summary>
        public async Task<List<TwPage>> PageSearchPaged(List<string> searchTerms, int pageNumber, int? pageSize = null, bool? allowFuzzyMatching = null)
        {
            if (searchTerms.Count == 0)
            {
                return new List<TwPage>();
            }

            pageSize ??= await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");
            allowFuzzyMatching ??= await _configurationRepository.Get<bool>(TwConfigGroup.Search, "Allow Fuzzy Matching");

            var meteredSearchTokens = await GetMeteredPageSearchTokens(searchTerms, allowFuzzyMatching == true);
            if (meteredSearchTokens.Count == 0)
            {
                return new List<TwPage>();
            }

            var maximumScore = meteredSearchTokens.Max(t => t.Score);
            var scoreByPageId = meteredSearchTokens.ToDictionary(t => t.PageId);
            var pageIds = scoreByPageId.Keys.ToList();

            using var context = _createContext();

            var pages = await context.Pages_Pages
                .Where(p => pageIds.Contains(p.Id))
                .Select(p => new TwPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Navigation = p.Navigation,
                    Description = p.Description,
                    Revision = p.Revision,
                    CreatedByUserId = p.CreatedByUserId,
                    CreatedDate = p.CreatedDate,
                    ModifiedByUserId = p.ModifiedByUserId,
                    ModifiedDate = p.ModifiedDate,
                    CreatedByUserName = p.CreatedByUser != null ? (p.CreatedByUser.AccountName ?? string.Empty) : string.Empty,
                    ModifiedByUserName = p.ModifiedByUser != null ? (p.ModifiedByUser.AccountName ?? string.Empty) : string.Empty,
                })
                .ToListAsync();

            var paginationPageCount = (pages.Count + (pageSize.Value - 1)) / pageSize.Value;

            foreach (var page in pages)
            {
                var token = scoreByPageId[page.Id];
                page.Match = (decimal)token.Match;
                page.Weight = (decimal)token.Weight;
                page.Score = (decimal)((token.Score / maximumScore) * 100.0);
                page.PaginationPageCount = paginationPageCount;
            }

            return pages
                .OrderByDescending(p => p.Score)
                .ThenBy(p => p.Name)
                .ThenBy(p => p.Id)
                .Skip((pageNumber - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .ToList();
        }

        /// <summary>
        /// Mirrors GetSimilarPagesPaged.sql: pages sharing at least <paramref name="similarity"/> percent of
        /// <paramref name="pageId"/>'s own tags (percentage = shared tag count / <paramref name="pageId"/>'s
        /// total tag count * 100), including <paramref name="pageId"/> itself (always 100% similar to its own
        /// tags) - a literal quirk of the reference script (no <c>P.Id &lt;&gt; @PageId</c> filter anywhere in
        /// it), preserved rather than "fixed" here. The reference's self-join against <c>PageTag</c> (via a
        /// <c>TempTags</c>-less <c>LEFT OUTER JOIN</c>) is replaced by a two-step query (candidate page IDs via
        /// <c>Contains(...)</c>, matching the shared <c>list.Contains(...)</c> temp-table substitution pattern)
        /// rather than a single join, since EF Core has no portable equivalent of the reference's own
        /// self-referencing join combined with a <c>HAVING</c> percentage threshold against a correlated-subquery
        /// denominator. No explicit <c>ORDER BY</c> exists in the reference script itself (rows come back in
        /// whatever order SQLite happens to produce for the <c>IN (...)</c> filter, typically primary-key/rowid
        /// order) - ordered here by <see cref="PagesEntities.Page.Id"/> ascending as the closest deterministic
        /// equivalent. Paginated by <paramref name="pageSize"/> (defaulting to the "Pagination Size" customization
        /// setting); <see cref="TwRelatedPage.PaginationPageCount"/> is computed via the reference's own
        /// ceiling-division formula against the total (unpaginated) matched-page count.
        /// </summary>
        public async Task<List<TwRelatedPage>> GetSimilarPagesPaged(int pageId, int similarity, int pageNumber, int? pageSize = null)
        {
            pageSize ??= await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            using var context = _createContext();

            var rootTags = await context.Pages_PageTags
                .Where(t => t.PageId == pageId)
                .Select(t => t.Tag)
                .ToListAsync();

            List<int> matchingPageIds;
            if (rootTags.Count == 0)
            {
                matchingPageIds = new List<int>();
            }
            else
            {
                var totalRootTagCount = rootTags.Count;

                matchingPageIds = await context.Pages_PageTags
                    .Where(t => rootTags.Contains(t.Tag))
                    .GroupBy(t => t.PageId)
                    .Where(g => (g.Count() / (double)totalRootTagCount) * 100.0 >= similarity)
                    .Select(g => g.Key)
                    .ToListAsync();
            }

            var query = context.Pages_Pages.Where(p => matchingPageIds.Contains(p.Id));

            var totalCount = await query.CountAsync();
            var paginationPageCount = (totalCount + (pageSize.Value - 1)) / pageSize.Value;

            return await query
                .OrderBy(p => p.Id)
                .Skip((pageNumber - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .Select(p => new TwRelatedPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Navigation = p.Navigation,
                    Description = p.Description,
                    PaginationPageSize = pageSize.Value,
                    PaginationPageCount = paginationPageCount,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors GetRelatedPagesPaged.sql: every page that references (links to) <paramref name="pageId"/> -
        /// despite the method's name, this is the same "who links here" relationship as
        /// <see cref="GetBacklinkPagesPaged"/>'s own first branch, just without the outlink/second-order-link
        /// branches - excluding self-references, ordered by <see cref="PagesEntities.Page.Name"/> ascending.
        /// Paginated by <paramref name="pageSize"/> (defaulting to the "Pagination Size" customization setting);
        /// <see cref="TwRelatedPage.PaginationPageCount"/> is computed via the reference's own ceiling-division
        /// formula against the total (unpaginated) matched-page count.
        /// </summary>
        public async Task<List<TwRelatedPage>> GetRelatedPagesPaged(int pageId, int pageNumber, int? pageSize = null)
        {
            pageSize ??= await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            using var context = _createContext();

            var query = from pr in context.PageReferences
                        join p in context.Pages_Pages on pr.PageId equals p.Id
                        where pr.ReferencesPageId == pageId && pr.PageId != pr.ReferencesPageId
                        select p;

            var totalCount = await query.CountAsync();
            var paginationPageCount = (totalCount + (pageSize.Value - 1)) / pageSize.Value;

            return await query
                .OrderBy(p => p.Name)
                .Skip((pageNumber - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .Select(p => new TwRelatedPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Navigation = p.Navigation,
                    Description = p.Description,
                    PaginationPageSize = pageSize.Value,
                    PaginationPageCount = paginationPageCount,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors GetBacklinkPagesPaged.sql: the union of three page sets related to <paramref name="pageId"/>
        /// via Pages.PageReference - pages that reference it (backlinks), pages it references (outlinks), and
        /// pages that reference the same targets it references (second-order links) - each excluding
        /// <paramref name="pageId"/> itself, deduplicated (the reference script's <c>UNION</c>, not <c>UNION
        /// ALL</c>), ordered by <see cref="PagesEntities.Page.Name"/> ascending. The reference script's single
        /// three-way <c>UNION</c> query (plus a <c>COUNT(*) OVER()</c> window function for pagination) has no safe
        /// single-query EF Core/SQL Server LINQ translation, so each branch is resolved to a page-ID list via its
        /// own translatable query (the same <c>Contains(...)</c> temp-table substitution pattern as
        /// <see cref="GetAllPagesPaged"/>'s remarks) and the three lists are combined and deduplicated client-side
        /// before the final paginated page query. Paginated by <paramref name="pageSize"/> (defaulting to the
        /// "Pagination Size" customization setting); <see cref="TwRelatedPage.PaginationPageCount"/> is computed
        /// via the reference's own ceiling-division formula (functionally equivalent to its window-function
        /// count) against the total (unpaginated) combined-page count.
        /// </summary>
        public async Task<List<TwRelatedPage>> GetBacklinkPagesPaged(int pageId, int pageNumber, int? pageSize = null)
        {
            pageSize ??= await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            using var context = _createContext();

            //Backlinks: pages that reference pageId.
            var backlinkIds = await context.PageReferences
                .Where(pr => pr.ReferencesPageId == pageId && pr.PageId != pageId)
                .Select(pr => pr.PageId)
                .ToListAsync();

            //Outlinks: pages referenced by pageId.
            var outlinkIds = await context.PageReferences
                .Where(pr => pr.PageId == pageId && pr.ReferencesPageId != null && pr.ReferencesPageId != pageId)
                .Select(pr => pr.ReferencesPageId!.Value)
                .ToListAsync();

            //Second order links: pages that reference the same targets pageId references.
            var outgoingTargetIds = await context.PageReferences
                .Where(pr => pr.PageId == pageId && pr.ReferencesPageId != null)
                .Select(pr => pr.ReferencesPageId!.Value)
                .Distinct()
                .ToListAsync();

            List<int> secondOrderIds;
            if (outgoingTargetIds.Count == 0)
            {
                secondOrderIds = new List<int>();
            }
            else
            {
                secondOrderIds = await context.PageReferences
                    .Where(pr => pr.ReferencesPageId != null
                        && outgoingTargetIds.Contains(pr.ReferencesPageId!.Value)
                        && pr.PageId != pageId)
                    .Select(pr => pr.PageId)
                    .ToListAsync();
            }

            var combinedIds = backlinkIds.Concat(outlinkIds).Concat(secondOrderIds).Distinct().ToList();

            var query = context.Pages_Pages.Where(p => combinedIds.Contains(p.Id));

            var totalCount = await query.CountAsync();
            var paginationPageCount = (totalCount + (pageSize.Value - 1)) / pageSize.Value;

            return await query
                .OrderBy(p => p.Name)
                .Skip((pageNumber - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .Select(p => new TwRelatedPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Navigation = p.Navigation,
                    Description = p.Description,
                    PaginationPageSize = pageSize.Value,
                    PaginationPageCount = paginationPageCount,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors <c>PageRepository.FlushPageCache</c>: clears every <see cref="MemCache.Category.Page"/> cache
        /// entry whose key starts with the page's navigation, and every one whose key starts with its id - the
        /// same two <see cref="MemCache.ClearCategory(MemCacheKey)"/> calls as the SQLite reference. Unlike the
        /// reference, this resolves the page's navigation with a direct, local query (mirroring
        /// GetPageNavigationByPageId.sql itself) rather than calling the public
        /// <see cref="GetPageNavigationByPageId"/> interface member - a historical artifact of phase 2b.2 (when
        /// <see cref="GetPageNavigationByPageId"/> was still a <see cref="NotImplementedException"/> stub and
        /// several of that phase's own methods, <see cref="InsertPageComment"/>/<see cref="DeletePageCommentById"/>/
        /// <see cref="DeletePageCommentByUserAndId"/>, already called this method and would otherwise always have
        /// failed), preserved as-is now that both are real: the two implementations are equivalent queries anyway.
        /// When <paramref name="navigation"/> is supplied, it's used directly instead of the local query above -
        /// callers that delete the page's row before flushing (e.g. <see cref="MovePageToDeletedById"/>) must pass
        /// it explicitly, since a query after the row is gone would return null and leave the navigation-keyed
        /// cache entry stale.
        /// </summary>
        public async Task FlushPageCache(int pageId, string? navigation = null)
        {
            var pageNavigation = navigation;

            if (pageNavigation is null)
            {
                using var context = _createContext();

                pageNavigation = await context.Pages_Pages
                    .Where(p => p.Id == pageId)
                    .Select(p => (string?)p.Navigation)
                    .FirstOrDefaultAsync();
            }

            MemCache.ClearCategory(MemCacheKey.Build(MemCache.Category.Page, [pageNavigation]));
            MemCache.ClearCategory(MemCacheKey.Build(MemCache.Category.Page, [pageId]));
        }

        /// <summary>
        /// Mirrors InsertPageComment.sql: inserts a new Pages.PageComment row (<see cref="PagesEntities.PageComment.CreatedDate"/>
        /// stamped to now, in UTC), then flushes this page's cache via <see cref="FlushPageCache"/> - same as the
        /// SQLite reference.
        /// </summary>
        public async Task InsertPageComment(int pageId, Guid userId, string body)
        {
            using var context = _createContext();

            context.Pages_PageComments.Add(new PagesEntities.PageComment
            {
                PageId = pageId,
                UserId = userId,
                Body = body,
                CreatedDate = DateTime.UtcNow,
            });

            await context.SaveChangesAsync();

            await FlushPageCache(pageId);
        }

        /// <summary>
        /// Mirrors DeletePageCommentById.sql via EF Core's LINQ bulk <c>ExecuteDeleteAsync</c> (fully
        /// provider-portable, no raw SQL needed), then flushes this page's cache via <see cref="FlushPageCache"/> -
        /// same as the SQLite reference.
        /// </summary>
        public async Task DeletePageCommentById(int pageId, int commentId)
        {
            using var context = _createContext();

            await context.Pages_PageComments
                .Where(c => c.PageId == pageId && c.Id == commentId)
                .ExecuteDeleteAsync();

            await FlushPageCache(pageId);
        }

        /// <summary>
        /// Mirrors DeletePageCommentByUserAndId.sql via EF Core's LINQ bulk <c>ExecuteDeleteAsync</c> - like the
        /// reference, only deletes the comment if <paramref name="userId"/> also matches its author, then flushes
        /// this page's cache via <see cref="FlushPageCache"/> regardless of whether a row was actually deleted -
        /// same as the SQLite reference.
        /// </summary>
        public async Task DeletePageCommentByUserAndId(int pageId, Guid userId, int commentId)
        {
            using var context = _createContext();

            await context.Pages_PageComments
                .Where(c => c.PageId == pageId && c.UserId == userId && c.Id == commentId)
                .ExecuteDeleteAsync();

            await FlushPageCache(pageId);
        }

        /// <summary>
        /// Mirrors GetTotalPageCommentCount.sql: the count of Pages.PageComment rows for <paramref name="pageId"/>.
        /// </summary>
        public async Task<int> GetTotalPageCommentCount(int pageId)
        {
            using var context = _createContext();

            return await context.Pages_PageComments.CountAsync(c => c.PageId == pageId);
        }

        /// <summary>
        /// Mirrors GetPageCommentsPaged.sql: an inner join of Pages.PageComment to its owning Pages.Page (filtered
        /// by <paramref name="navigation"/>) LEFT OUTER JOINed to Users.Profile for the comment author - via the
        /// existing <see cref="PagesEntities.PageComment.Page"/>/<see cref="PagesEntities.PageComment.User"/>
        /// navigations rather than a raw cross-database <c>ATTACH</c> (the SQLite reference's
        /// <c>o.Attach("users.db", "users_db")</c>), since both schemas already live in the same
        /// <see cref="TightWikiDbContext"/> - ordered by <see cref="PagesEntities.PageComment.CreatedDate"/>
        /// descending, paginated by the "Pagination Size" customization setting.
        /// <see cref="TwPageComment.PaginationPageCount"/> is computed via the reference's own ceiling-division
        /// formula (<c>(Count(0) + (@PageSize - 1)) / @PageSize</c>) against the total (unpaginated) comment count
        /// for the page. <see cref="TwPageComment.UserNavigation"/> is deliberately populated from
        /// <c>Profile.AccountName</c>, not <c>Profile.Navigation</c> - a literal quirk of the reference script
        /// (<c>U.AccountName as UserNavigation</c>), preserved rather than "fixed" here. Cached under
        /// <see cref="MemCache.Category.Page"/>, same cache key shape (navigation + page number + page size) as
        /// the SQLite reference.
        /// </summary>
        public async Task<List<TwPageComment>> GetPageCommentsPaged(string navigation, int pageNumber)
        {
            var paginationSize = await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.Page, [navigation, pageNumber, paginationSize]);

            return (await MemCache.AddOrGetAsync(cacheKey, async () =>
            {
                using var context = _createContext();

                var commentsForPage = context.Pages_PageComments.Where(c => c.Page.Navigation == navigation);

                var totalCommentCount = await commentsForPage.CountAsync();
                var paginationPageCount = (totalCommentCount + (paginationSize - 1)) / paginationSize;

                return await commentsForPage
                    .OrderByDescending(c => c.CreatedDate)
                    .Skip((pageNumber - 1) * paginationSize)
                    .Take(paginationSize)
                    .Select(c => new TwPageComment
                    {
                        Id = c.Id,
                        PageId = c.PageId,
                        CreatedDate = c.CreatedDate,
                        UserId = c.UserId,
                        Body = c.Body,
                        UserName = c.User != null ? (c.User.AccountName ?? string.Empty) : string.Empty,
                        UserNavigation = c.User != null ? (c.User.AccountName ?? string.Empty) : string.Empty,
                        PageName = c.Page.Name,
                        PaginationPageCount = paginationPageCount,
                    })
                    .ToListAsync();
            })).EnsureNotNull();
        }

        /// <summary>
        /// Mirrors GetMissingPagesPaged.sql: every Pages.PageReference row whose target does not resolve to an
        /// existing page (<see cref="PagesEntities.PageReference.ReferencesPageId"/> is null), read via the
        /// existing, required <see cref="PagesEntities.PageReference.Page"/> navigation for the source page's
        /// fields rather than a manual join. Paginated by the "Pagination Size" customization setting, same as the
        /// reference, and <see cref="TwNonexistentPage.PaginationPageCount"/> is computed via the reference's own
        /// ceiling-division formula against the total (unpaginated) count of broken references. Ordering mirrors
        /// <c>RepositoryHelpers.TransposeOrderby</c> against the script's <c>--CONFIG::</c> mapping ("SourcePage"/
        /// "TargetPage"): no <paramref name="orderBy"/> falls back to the script's own un-transposed "ORDER BY
        /// P.[Name]" (always ascending, ignoring <paramref name="orderByDirection"/> - a literal quirk of the
        /// reference script, which hardcodes no direction on its own default ORDER BY); an unrecognized
        /// <paramref name="orderBy"/> throws, same pattern as <c>RepositoryHelpers.TransposeOrderby</c>'s "No
        /// order by mapping..." exception (see <see cref="GetPageRevisionsInfoByNavigationPaged"/> for the
        /// existing convention this follows).
        /// </summary>
        public async Task<List<TwNonexistentPage>> GetMissingPagesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            using var context = _createContext();

            var query = context.PageReferences.Where(pr => pr.ReferencesPageId == null);

            var totalCount = await query.CountAsync();
            var paginationPageCount = (totalCount + (paginationSize - 1)) / paginationSize;

            bool ascending = string.Equals(orderByDirection, "asc", StringComparison.InvariantCultureIgnoreCase);

            var ordered = string.IsNullOrEmpty(orderBy)
                ? query.OrderBy(pr => pr.Page.Name)
                : orderBy.ToUpperInvariant() switch
                {
                    "SOURCEPAGE" => ascending ? query.OrderBy(pr => pr.Page.Name) : query.OrderByDescending(pr => pr.Page.Name),
                    "TARGETPAGE" => ascending ? query.OrderBy(pr => pr.ReferencesPageName) : query.OrderByDescending(pr => pr.ReferencesPageName),
                    _ => throw new InvalidOperationException(
                        $"No order by mapping was found in 'GetMissingPagesPaged.sql' for the field '{orderBy}'."),
                };

            return await ordered
                .Skip((pageNumber - 1) * paginationSize)
                .Take(paginationSize)
                .Select(pr => new TwNonexistentPage
                {
                    SourcePageId = pr.PageId,
                    SourcePageName = pr.Page.Name,
                    SourcePageNavigation = pr.Page.Navigation,
                    TargetPageName = pr.ReferencesPageName,
                    TargetPageNavigation = pr.ReferencesPageNavigation,
                    PaginationPageCount = paginationPageCount,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors UpdateSinglePageReference.sql: resolves every existing Pages.PageReference row (across every
        /// page, not just <paramref name="pageId"/>'s own outgoing references) whose
        /// <see cref="PagesEntities.PageReference.ReferencesPageNavigation"/> matches <paramref name="pageNavigation"/>,
        /// setting its <see cref="PagesEntities.PageReference.ReferencesPageId"/> to <paramref name="pageId"/> -
        /// the "fix up orphaned references once the page they point to actually gets created" step
        /// <see cref="UpsertPage"/> runs for newly-created pages. No transaction, matching the SQLite reference (a
        /// bare single-statement <c>UPDATE</c>). Flushes this page's cache via <see cref="FlushPageCache"/>
        /// afterward, same as the SQLite reference.
        /// </summary>
        public async Task UpdateSinglePageReference(string pageNavigation, int pageId)
        {
            using var context = _createContext();

            await context.PageReferences
                .Where(pr => pr.ReferencesPageNavigation == pageNavigation)
                .ExecuteUpdateAsync(setters => setters.SetProperty(pr => pr.ReferencesPageId, pageId));

            await FlushPageCache(pageId);
        }

        /// <summary>
        /// Mirrors UpdatePageReferences.sql: replaces all Pages.PageReference rows for <paramref name="pageId"/>
        /// (its own outgoing references) with <paramref name="referencesPageNavigations"/> (deduplicated -
        /// <see cref="TwPageReference.Equals"/> compares <see cref="TwPageReference.Navigation"/>
        /// case-insensitively, same as the reference's own <c>referencesPageNavigations.Distinct()</c> before
        /// building <c>TempReferences</c>), wrapped in a single transaction (the reference's own <c>BEGIN
        /// TRANSACTION</c>/<c>COMMIT TRANSACTION</c>) - delete-then-insert, not a diff/merge. Each inserted row's
        /// <see cref="PagesEntities.PageReference.ReferencesPageId"/> is resolved via a <c>Contains(...)</c>
        /// lookup against <see cref="PagesEntities.Page.Navigation"/> (the <c>TempReferences</c>
        /// left-outer-join replacement, same pattern as <see cref="GetAllPagesPaged"/>'s remarks), materialized
        /// into a case-insensitive dictionary since <see cref="PagesEntities.Page.Navigation"/> carries a
        /// case-insensitive collation (<see cref="TightWiki.Data.EfCore.Configurations.Pages.PageReferenceConfiguration"/>) - left null when no page with
        /// that navigation currently exists (an orphaned/broken reference, later resolved by
        /// <see cref="UpdateSinglePageReference"/> if a page with that navigation is subsequently created).
        /// <see cref="PagesEntities.PageReference.ReferencesPageName"/> is built as the reference script's own
        /// <c>Coalesce(Ref.[Namespace] || ' :: ', '') || Ref.Name</c> - since <see cref="TwPageReference.Namespace"/>
        /// is a non-nullable property that is always either empty or a real namespace (never literally null in
        /// any reachable code path), this <c>Coalesce</c>'s null-branch is unreachable, so the literal,
        /// faithfully-reproduced result always includes the <c>" :: "</c> separator, even for un-namespaced
        /// references (a pre-existing, user-visible quirk of the reference script - e.g. a reference to a page
        /// named "SandBox" with no namespace stores <c>" :: SandBox"</c>, not <c>"SandBox"</c> - preserved here
        /// rather than "fixed", per this class's established convention of not correcting merely-odd-looking but
        /// faithfully-reproducible reference behavior). An empty (post-dedup) <paramref name="referencesPageNavigations"/>
        /// list still deletes the page's existing references (matching the reference's unconditional <c>DELETE
        /// FROM PageReference WHERE PageId = @PageId</c>) and simply inserts nothing. Flushes this page's cache
        /// via <see cref="FlushPageCache"/> afterward, same as the SQLite reference.
        /// </summary>
        /// <remarks>
        /// <b>⚠ Confirmed bug in the SQLite reference, deliberately not reproduced here.</b> After the delete/insert
        /// above, the reference script runs a second statement - <c>UPDATE PageReference SET
        /// ReferencesPageNavigation = I.Navigation FROM (SELECT DISTINCT Id, P.Navigation FROM PageReference as PR
        /// INNER JOIN [Page] as P ON P.Id = PR.ReferencesPageId WHERE P.Id = 77) AS I WHERE I.Id =
        /// PageReference.ReferencesPageId</c> - a hardcoded, unparameterized literal <c>77</c> in place of what
        /// every surrounding line of the same script (and the join it drives) makes clear should have been
        /// <c>@PageId</c> (confirmed unchanged since the very first SQLite port of this script via <c>git log
        /// --follow</c>, and unique to this one script - no other script in <c>TightWiki.Repository/Scripts/</c>
        /// filters on a bare numeric literal this way, the same class of pre-existing bug documented on
        /// <see cref="EfEmojiRepository.UpsertEmoji"/>'s own remarks). As written, this second statement is inert
        /// for every real page save except the one-in-a-database-lifetime coincidence that a page with Id exactly
        /// 77 is being referenced. The "evidently intended" fix reads as syncing
        /// <see cref="PagesEntities.PageReference.ReferencesPageNavigation"/> on every <i>other</i> page's
        /// reference <i>to</i> <paramref name="pageId"/> (e.g. after a rename) - but since this phase's task did
        /// not ask for that behavior and it would be new, untested functionality beyond anything the reference
        /// actually does for any real page today, it is not speculatively implemented here. This method
        /// reproduces only the unambiguous delete/insert half of the script, which is a complete, faithful mirror
        /// of the reference's real (non-dead-code) behavior.
        /// </remarks>
        public async Task UpdatePageReferences(int pageId, List<TwPageReference> referencesPageNavigations)
        {
            var distinctReferences = referencesPageNavigations.Distinct().ToList();

            using var context = _createContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            await context.PageReferences
                .Where(pr => pr.PageId == pageId)
                .ExecuteDeleteAsync();

            if (distinctReferences.Count > 0)
            {
                var referencedNavigations = distinctReferences.Select(r => r.Navigation).ToList();

                var resolvedPages = await context.Pages_Pages
                    .Where(p => referencedNavigations.Contains(p.Navigation))
                    .Select(p => new { p.Navigation, p.Id })
                    .ToListAsync();

                var resolvedPageIdByNavigation = resolvedPages
                    .ToDictionary(p => p.Navigation, p => p.Id, StringComparer.OrdinalIgnoreCase);

                context.PageReferences.AddRange(distinctReferences.Select(r => new PagesEntities.PageReference
                {
                    PageId = pageId,
                    ReferencesPageName = r.Namespace + " :: " + r.Name,
                    ReferencesPageNavigation = r.Navigation,
                    ReferencesPageId = resolvedPageIdByNavigation.TryGetValue(r.Navigation, out var resolvedPageId)
                        ? resolvedPageId
                        : null,
                }));

                await context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            await FlushPageCache(pageId);
        }

        /// <summary>
        /// Mirrors GetAllPagesByInstructionPaged.sql: every Pages.Page row that has the given
        /// <paramref name="instruction"/> recorded in Pages.PageProcessingInstruction, read via the existing,
        /// required <see cref="PagesEntities.PageProcessingInstruction.Page"/> navigation rather than a manual
        /// join, with <see cref="TwPage.CreatedByUserName"/>/<see cref="TwPage.ModifiedByUserName"/> resolved via
        /// the existing <see cref="PagesEntities.Page.CreatedByUser"/>/<see cref="PagesEntities.Page.ModifiedByUser"/>
        /// navigations rather than the script's own cross-database <c>o.Attach("users.db", "users_db")</c> (both
        /// schemas already live in the same <see cref="TightWikiDbContext"/>). When <paramref name="instruction"/>
        /// is null, no rows match - EF Core rewrites the equality against a null parameter into a null-safe
        /// comparison, and <see cref="PagesEntities.PageProcessingInstruction.Instruction"/> is a <c>NOT NULL</c>
        /// column, same net effect as the reference script's literal <c>WHERE PPI.Instruction = @Instruction</c>
        /// with a null parameter. Ordered by <see cref="PagesEntities.Page.Name"/> then
        /// <see cref="PagesEntities.Page.Id"/> ascending (this method takes no <c>orderBy</c> parameter - the
        /// reference script has no <c>--CUSTOM_ORDER_BEGIN::</c> section), paginated by the "Pagination Size"
        /// customization setting. <see cref="TwPage.PaginationPageCount"/> is computed via the reference's own
        /// ceiling-division formula against the total (unpaginated) count of matching pages.
        /// </summary>
        public async Task<List<TwPage>> GetAllPagesByInstructionPaged(int pageNumber, string? instruction = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            using var context = _createContext();

            var query = context.Pages_PageProcessingInstructions
                .Where(pi => pi.Instruction == instruction)
                .Select(pi => pi.Page);

            var totalCount = await query.CountAsync();
            var paginationPageCount = (totalCount + (paginationSize - 1)) / paginationSize;

            return await query
                .OrderBy(p => p.Name)
                .ThenBy(p => p.Id)
                .Skip((pageNumber - 1) * paginationSize)
                .Take(paginationSize)
                .Select(p => new TwPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Navigation = p.Navigation,
                    Description = p.Description,
                    Revision = p.Revision,
                    CreatedByUserId = p.CreatedByUserId,
                    CreatedDate = p.CreatedDate,
                    ModifiedByUserId = p.ModifiedByUserId,
                    ModifiedDate = p.ModifiedDate,
                    CreatedByUserName = p.CreatedByUser != null ? (p.CreatedByUser.AccountName ?? string.Empty) : string.Empty,
                    ModifiedByUserName = p.ModifiedByUser != null ? (p.ModifiedByUser.AccountName ?? string.Empty) : string.Empty,
                    PaginationPageCount = paginationPageCount,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors GetDeletedPageIdsByTokens.sql: the IDs of every soft-deleted page whose DeletedPages.PageToken
        /// rows cover every distinct non-empty token in <paramref name="tokens"/> - an AND-style "does this page
        /// contain all of these tokens" match (unlike <see cref="PageSearch"/>'s score-based ranking). The
        /// reference script's own arithmetic (<c>HAVING Count(0) = @TokenCount</c>, where <c>@TokenCount</c> is
        /// <paramref name="tokens"/>'s raw, non-deduplicated length) is algebraically equivalent to "every element
        /// of <paramref name="tokens"/> - counting duplicates - is a non-empty token the page actually has": if
        /// <paramref name="tokens"/> contains any null/empty entry, no page can ever satisfy the count (the
        /// script's own <c>WHERE Coalesce(TT.[value], '') &lt;&gt; ''</c> guarantees an empty entry can never join
        /// to a matching Pages.PageToken row, so it can never contribute to the required total), so that case is
        /// short-circuited to an empty result here rather than issuing a query that could never match anything.
        /// Otherwise, this reduces to "the page has every distinct token" - resolved via <c>Contains(...)</c> (the
        /// <c>TempTokens</c> replacement, same pattern as <see cref="GetAllPagesPaged"/>'s remarks) plus a grouped
        /// count check that relies on DeletedPages.PageToken's composite primary key (PageId, Token) to guarantee
        /// at most one matching row per page per distinct token.
        /// </summary>
        public async Task<List<int>> GetDeletedPageIdsByTokens(List<string>? tokens)
        {
            if (tokens == null || tokens.Count == 0)
            {
                return new List<int>();
            }

            if (tokens.Any(string.IsNullOrEmpty))
            {
                return new List<int>();
            }

            var distinctTokens = tokens.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            using var context = _createContext();

            return await context.DeletedPages_PageTokens
                .Where(t => distinctTokens.Contains(t.Token))
                .GroupBy(t => t.PageId)
                .Where(g => g.Count() == distinctTokens.Count)
                .Select(g => g.Key)
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors GetPageIdsByTokens.sql: the same "page contains every distinct non-empty token" match as
        /// <see cref="GetDeletedPageIdsByTokens"/>, against active Pages.PageToken rows instead of soft-deleted
        /// DeletedPages.PageToken rows - see that method's remarks for the full explanation of the
        /// <c>HAVING Count(0) = @TokenCount</c> equivalence and the empty-token short-circuit.
        /// </summary>
        public async Task<List<int>> GetPageIdsByTokens(List<string>? tokens)
        {
            if (tokens == null || tokens.Count == 0)
            {
                return new List<int>();
            }

            if (tokens.Any(string.IsNullOrEmpty))
            {
                return new List<int>();
            }

            var distinctTokens = tokens.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            using var context = _createContext();

            return await context.Pages_PageTokens
                .Where(t => distinctTokens.Contains(t.Token))
                .GroupBy(t => t.PageId)
                .Where(g => g.Count() == distinctTokens.Count)
                .Select(g => g.Key)
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors GetAllNamespacePagesPaged.sql: every Pages.Page row whose <see cref="PagesEntities.Page.Namespace"/>
        /// matches <paramref name="namespaceName"/>, with <see cref="TwPage.CreatedByUserName"/>/
        /// <see cref="TwPage.ModifiedByUserName"/> resolved via the existing <see cref="PagesEntities.Page.CreatedByUser"/>/
        /// <see cref="PagesEntities.Page.ModifiedByUser"/> navigations rather than a raw cross-database
        /// <c>ATTACH</c>, same substitution as <see cref="GetAllPagesByInstructionPaged"/>. Paginated by the
        /// "Pagination Size" customization setting; <see cref="TwPage.PaginationPageCount"/> is computed via the
        /// reference's own ceiling-division formula against the total (unpaginated) count of pages in the
        /// namespace. Ordering mirrors <c>RepositoryHelpers.TransposeOrderby</c> against the script's
        /// <c>--CONFIG::</c> mapping ("Name"/"Revision"/"ModifiedBy"/"ModifiedDate"): no <paramref name="orderBy"/>
        /// falls back to the script's own un-transposed "ORDER BY P.[Name]" (always ascending, same quirk as
        /// <see cref="GetMissingPagesPaged"/>); an unrecognized <paramref name="orderBy"/> throws, same pattern as
        /// <see cref="GetPageRevisionsInfoByNavigationPaged"/>.
        /// </summary>
        public async Task<List<TwPage>> GetAllNamespacePagesPaged(int pageNumber, string namespaceName, string? orderBy = null, string? orderByDirection = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            using var context = _createContext();

            var query = context.Pages_Pages.Where(p => p.Namespace == namespaceName);

            var totalCount = await query.CountAsync();
            var paginationPageCount = (totalCount + (paginationSize - 1)) / paginationSize;

            bool ascending = string.Equals(orderByDirection, "asc", StringComparison.InvariantCultureIgnoreCase);

            var ordered = string.IsNullOrEmpty(orderBy)
                ? query.OrderBy(p => p.Name)
                : orderBy.ToUpperInvariant() switch
                {
                    "NAME" => ascending ? query.OrderBy(p => p.Name) : query.OrderByDescending(p => p.Name),
                    "REVISION" => ascending ? query.OrderBy(p => p.Revision) : query.OrderByDescending(p => p.Revision),
                    "MODIFIEDBY" => ascending
                        ? query.OrderBy(p => p.ModifiedByUser != null ? p.ModifiedByUser.AccountName : null)
                        : query.OrderByDescending(p => p.ModifiedByUser != null ? p.ModifiedByUser.AccountName : null),
                    "MODIFIEDDATE" => ascending ? query.OrderBy(p => p.ModifiedDate) : query.OrderByDescending(p => p.ModifiedDate),
                    _ => throw new InvalidOperationException(
                        $"No order by mapping was found in 'GetAllNamespacePagesPaged.sql' for the field '{orderBy}'."),
                };

            return await ordered
                .Skip((pageNumber - 1) * paginationSize)
                .Take(paginationSize)
                .Select(p => new TwPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Navigation = p.Navigation,
                    Description = p.Description,
                    Revision = p.Revision,
                    CreatedByUserId = p.CreatedByUserId,
                    CreatedDate = p.CreatedDate,
                    ModifiedByUserId = p.ModifiedByUserId,
                    ModifiedDate = p.ModifiedDate,
                    CreatedByUserName = p.CreatedByUser != null ? (p.CreatedByUser.AccountName ?? string.Empty) : string.Empty,
                    ModifiedByUserName = p.ModifiedByUser != null ? (p.ModifiedByUser.AccountName ?? string.Empty) : string.Empty,
                    PaginationPageCount = paginationPageCount,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors GetAllPagesPaged.sql (and, when <paramref name="searchTerms"/> is non-empty,
        /// GetAllPagesByPageIdPaged.sql): every Pages.Page row, with <see cref="TwPage.CreatedByUserName"/>/
        /// <see cref="TwPage.ModifiedByUserName"/> resolved via the existing <see cref="PagesEntities.Page.CreatedByUser"/>/
        /// <see cref="PagesEntities.Page.ModifiedByUser"/> navigations rather than the scripts' own cross-database
        /// <c>o.Attach("users.db", "users_db")</c> (both schemas already live in the same
        /// <see cref="TightWikiDbContext"/>). <see cref="TwPage.DeletedRevisionCount"/> is computed the same way
        /// as both scripts' own correlated subquery against DeletedPageRevisions.PageRevision, resolved here via
        /// <see cref="TightWikiDbContext.DeletedPageRevisions_PageRevisions"/> rather than the scripts' own
        /// <c>o.Attach("deletedpagerevisions.db", "deletedpagerevisions_db")</c> - same cross-schema-navigation
        /// substitution, just against a schema with no navigation property defined for this particular
        /// relationship.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>TempPageIds replacement (Database-Providers-Plan.md chapter 4.4/8):</b> the reference implementation
        /// resolves <paramref name="searchTerms"/> to a list of matching page IDs via
        /// <see cref="GetPageIdsByTokens"/> (itself backed by a <c>TempTokens</c> temp table - out of scope for
        /// this method, landing in phase 2b.5), then feeds that ID list into GetAllPagesByPageIdPaged.sql via a
        /// second temp table, <c>CreateTempTableFrom("TempPageIds", pageIds)</c>, so the SQL can do
        /// <c>WHERE P.Id IN (SELECT PID.Value FROM TempPageIds as PID)</c>. EF Core/SQL Server has no equivalent
        /// concept of an ad-hoc session-scoped temp table reachable from LINQ, so this is replaced with the
        /// simplest portable equivalent: keep <c>pageIds</c> as a plain in-memory <see cref="List{T}"/> of
        /// <see cref="int"/> and filter with <c>pageIds.Contains(p.Id)</c> directly in the LINQ query. EF Core
        /// translates this to a parameterized <c>WHERE p.Id IN (...)</c> (or an equivalent translation for large
        /// lists), which is functionally equivalent to the temp table's own <c>IN (SELECT ... FROM TempPageIds)</c>
        /// for a plain "is this ID in the set" filter - just without a physical table backing the set. This same
        /// <c>list.Contains(...)</c> pattern is the one to reuse for every other <c>CreateTempTableFrom</c> call in
        /// the reference (<c>TempTokens</c>, <c>TempTags</c>, <c>TempNamespaces</c>, <c>TempReferences</c>,
        /// <c>TempInstructions</c> - phases 2b.5/2b.6).
        /// </para>
        /// <para>
        /// <see cref="GetPageIdsByTokens"/> was still a <see cref="NotImplementedException"/> stub when this
        /// method itself landed in phase 2b.4 - a known, documented limitation at the time, since resolved by
        /// <see cref="GetPageIdsByTokens"/>'s own real implementation in phase 2b.5, with no further changes
        /// needed here for the <paramref name="searchTerms"/>-filtered path to start working.
        /// </para>
        /// <para>
        /// Ordering mirrors <c>RepositoryHelpers.TransposeOrderby</c> against the scripts' shared <c>--CONFIG::</c>
        /// mapping ("DeletedRevisions"/"Name"/"Revision"/"ModifiedBy"/"ModifiedDate"): no <paramref name="orderBy"/>
        /// falls back to the scripts' own un-transposed "ORDER BY P.[Name]" (always ascending, same quirk as
        /// <see cref="GetMissingPagesPaged"/>); an unrecognized <paramref name="orderBy"/> throws, same pattern as
        /// <see cref="GetPageRevisionsInfoByNavigationPaged"/>. Paginated by the "Pagination Size" customization
        /// setting; <see cref="TwPage.PaginationPageCount"/> is computed via the scripts' own ceiling-division
        /// formula against the total (unpaginated, but already ID-filtered when <paramref name="searchTerms"/> is
        /// given) page count.
        /// </para>
        /// </remarks>
        public async Task<List<TwPage>> GetAllPagesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, List<string>? searchTerms = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            List<int>? pageIds = null;
            if (searchTerms?.Count > 0)
            {
                pageIds = await GetPageIdsByTokens(searchTerms);
            }

            using var context = _createContext();

            IQueryable<PagesEntities.Page> query = context.Pages_Pages;

            if (pageIds != null)
            {
                query = query.Where(p => pageIds.Contains(p.Id));
            }

            var totalCount = await query.CountAsync();
            var paginationPageCount = (totalCount + (paginationSize - 1)) / paginationSize;

            bool ascending = string.Equals(orderByDirection, "asc", StringComparison.InvariantCultureIgnoreCase);

            var ordered = string.IsNullOrEmpty(orderBy)
                ? query.OrderBy(p => p.Name)
                : orderBy.ToUpperInvariant() switch
                {
                    "DELETEDREVISIONS" => ascending
                        ? query.OrderBy(p => context.DeletedPageRevisions_PageRevisions.Count(pr => pr.PageId == p.Id))
                        : query.OrderByDescending(p => context.DeletedPageRevisions_PageRevisions.Count(pr => pr.PageId == p.Id)),
                    "NAME" => ascending ? query.OrderBy(p => p.Name) : query.OrderByDescending(p => p.Name),
                    "REVISION" => ascending ? query.OrderBy(p => p.Revision) : query.OrderByDescending(p => p.Revision),
                    "MODIFIEDBY" => ascending
                        ? query.OrderBy(p => p.ModifiedByUser != null ? p.ModifiedByUser.AccountName : null)
                        : query.OrderByDescending(p => p.ModifiedByUser != null ? p.ModifiedByUser.AccountName : null),
                    "MODIFIEDDATE" => ascending ? query.OrderBy(p => p.ModifiedDate) : query.OrderByDescending(p => p.ModifiedDate),
                    _ => throw new InvalidOperationException(
                        $"No order by mapping was found in 'GetAllPagesPaged.sql' for the field '{orderBy}'."),
                };

            return await ordered
                .Skip((pageNumber - 1) * paginationSize)
                .Take(paginationSize)
                .Select(p => new TwPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Navigation = p.Navigation,
                    Description = p.Description,
                    Revision = p.Revision,
                    CreatedByUserId = p.CreatedByUserId,
                    CreatedDate = p.CreatedDate,
                    ModifiedByUserId = p.ModifiedByUserId,
                    ModifiedDate = p.ModifiedDate,
                    CreatedByUserName = p.CreatedByUser != null ? (p.CreatedByUser.AccountName ?? string.Empty) : string.Empty,
                    ModifiedByUserName = p.ModifiedByUser != null ? (p.ModifiedByUser.AccountName ?? string.Empty) : string.Empty,
                    DeletedRevisionCount = context.DeletedPageRevisions_PageRevisions.Count(pr => pr.PageId == p.Id),
                    PaginationPageCount = paginationPageCount,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors GetAllDeletedPagesPaged.sql (and, when <paramref name="searchTerms"/> is non-empty,
        /// GetAllDeletedPagesByPageIdPaged.sql): every DeletedPages.Page row, inner-joined to its
        /// DeletedPages.DeletionMeta row - a manual join (no navigation property exists between the two entities),
        /// matching the scripts' own <c>INNER JOIN DeletionMeta as DM ON DM.PageId = P.Id</c> - LEFT OUTER JOINed
        /// to Users.Profile three times (creator, modifier, deleter) via the existing
        /// <see cref="DeletedPagesEntities.Page.CreatedByUser"/>/<see cref="DeletedPagesEntities.Page.ModifiedByUser"/>/
        /// <see cref="DeletedPagesEntities.DeletionMeta.DeletedByUser"/> navigations rather than the scripts' own
        /// cross-database <c>ATTACH</c>. <paramref name="searchTerms"/> is resolved to a page-ID filter the same
        /// way, and with the same <c>TempPageIds</c>-replacement caveat (delegates to the still-unimplemented
        /// <see cref="GetDeletedPageIdsByTokens"/>), as documented on <see cref="GetAllPagesPaged"/> - see that
        /// method's remarks for the full explanation of the <c>list.Contains(...)</c> substitution pattern.
        /// Ordering mirrors <c>RepositoryHelpers.TransposeOrderby</c> against the scripts' shared <c>--CONFIG::</c>
        /// mapping ("Page"): no <paramref name="orderBy"/> falls back to the scripts' own un-transposed "ORDER BY
        /// P.[Name]" (always ascending, same quirk as <see cref="GetMissingPagesPaged"/>); an unrecognized
        /// <paramref name="orderBy"/> throws, same pattern as <see cref="GetPageRevisionsInfoByNavigationPaged"/>.
        /// Paginated by the "Pagination Size" customization setting; <see cref="TwPage.PaginationPageCount"/> is
        /// computed via the scripts' own ceiling-division formula against the total (unpaginated, but already
        /// ID-filtered when <paramref name="searchTerms"/> is given) deleted-page count.
        /// <see cref="TwPage.DeletedByUserId"/> is deliberately left unset here - a literal quirk of both
        /// reference scripts, which select <c>DeletedUser.AccountName as DeletedByUserName</c> but never the raw
        /// <c>DM.DeletedByUserID</c> column itself.
        /// </summary>
        public async Task<List<TwPage>> GetAllDeletedPagesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, List<string>? searchTerms = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            List<int>? pageIds = null;
            if (searchTerms?.Count > 0)
            {
                pageIds = await GetDeletedPageIdsByTokens(searchTerms);
            }

            using var context = _createContext();

            var joined = from p in context.DeletedPages_Pages
                         join dm in context.DeletedPages_DeletionMetas on p.Id equals dm.PageId
                         select new { p, dm };

            if (pageIds != null)
            {
                joined = joined.Where(x => pageIds.Contains(x.p.Id));
            }

            var totalCount = await joined.CountAsync();
            var paginationPageCount = (totalCount + (paginationSize - 1)) / paginationSize;

            bool ascending = string.Equals(orderByDirection, "asc", StringComparison.InvariantCultureIgnoreCase);

            var ordered = string.IsNullOrEmpty(orderBy)
                ? joined.OrderBy(x => x.p.Name)
                : orderBy.ToUpperInvariant() switch
                {
                    "PAGE" => ascending ? joined.OrderBy(x => x.p.Name) : joined.OrderByDescending(x => x.p.Name),
                    _ => throw new InvalidOperationException(
                        $"No order by mapping was found in 'GetAllDeletedPagesPaged.sql' for the field '{orderBy}'."),
                };

            return await ordered
                .Skip((pageNumber - 1) * paginationSize)
                .Take(paginationSize)
                .Select(x => new TwPage
                {
                    Id = x.p.Id,
                    Name = x.p.Name,
                    Navigation = x.p.Navigation,
                    Description = x.p.Description,
                    Revision = x.p.Revision,
                    CreatedByUserId = x.p.CreatedByUserId,
                    CreatedDate = x.p.CreatedDate,
                    ModifiedByUserId = x.p.ModifiedByUserId,
                    ModifiedDate = x.p.ModifiedDate,
                    CreatedByUserName = x.p.CreatedByUser != null ? (x.p.CreatedByUser.AccountName ?? string.Empty) : string.Empty,
                    ModifiedByUserName = x.p.ModifiedByUser != null ? (x.p.ModifiedByUser.AccountName ?? string.Empty) : string.Empty,
                    DeletedByUserName = x.dm.DeletedByUser != null ? (x.dm.DeletedByUser.AccountName ?? string.Empty) : string.Empty,
                    DeletedDate = x.dm.DeletedDate ?? default,
                    PaginationPageCount = paginationPageCount,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors GetAllNamespacesPaged.sql: every distinct <see cref="PagesEntities.Page.Namespace"/> value
        /// grouped with a count of pages in that namespace. Paginated by the "Pagination Size" customization
        /// setting; <see cref="TwNamespaceStat.PaginationPageCount"/> is computed via the reference's own
        /// ceiling-division formula against the total count of distinct namespaces (not the total page count).
        /// Ordering mirrors <c>RepositoryHelpers.TransposeOrderby</c> against the script's <c>--CONFIG::</c>
        /// mapping ("Name"/"Pages"): no <paramref name="orderBy"/> falls back to the script's own un-transposed
        /// "ORDER BY P.[Namespace]" (always ascending, same quirk as <see cref="GetMissingPagesPaged"/>); an
        /// unrecognized <paramref name="orderBy"/> throws, same pattern as
        /// <see cref="GetPageRevisionsInfoByNavigationPaged"/>.
        /// </summary>
        public async Task<List<TwNamespaceStat>> GetAllNamespacesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            using var context = _createContext();

            var grouped = context.Pages_Pages
                .GroupBy(p => p.Namespace)
                .Select(g => new { Namespace = g.Key, CountOfPages = g.Count() });

            var distinctNamespaceCount = await context.Pages_Pages.Select(p => p.Namespace).Distinct().CountAsync();
            var paginationPageCount = (distinctNamespaceCount + (paginationSize - 1)) / paginationSize;

            bool ascending = string.Equals(orderByDirection, "asc", StringComparison.InvariantCultureIgnoreCase);

            var ordered = string.IsNullOrEmpty(orderBy)
                ? grouped.OrderBy(g => g.Namespace)
                : orderBy.ToUpperInvariant() switch
                {
                    "NAME" => ascending ? grouped.OrderBy(g => g.Namespace) : grouped.OrderByDescending(g => g.Namespace),
                    "PAGES" => ascending ? grouped.OrderBy(g => g.CountOfPages) : grouped.OrderByDescending(g => g.CountOfPages),
                    _ => throw new InvalidOperationException(
                        $"No order by mapping was found in 'GetAllNamespacesPaged.sql' for the field '{orderBy}'."),
                };

            return await ordered
                .Skip((pageNumber - 1) * paginationSize)
                .Take(paginationSize)
                .Select(g => new TwNamespaceStat
                {
                    Namespace = g.Namespace,
                    CountOfPages = g.CountOfPages,
                    PaginationPageCount = paginationPageCount,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors GetAllNamespaces.sql: every distinct <see cref="PagesEntities.Page.Namespace"/> value, no
        /// ordering (matching the reference's plain "SELECT DISTINCT [Namespace] FROM [Page]").
        /// </summary>
        public async Task<List<string>> GetAllNamespaces()
        {
            using var context = _createContext();

            return await context.Pages_Pages
                .Select(p => p.Namespace)
                .Distinct()
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors GetAllPages.sql: every Pages.Page row inner-joined to the Pages.PageRevision row matching its
        /// own current <see cref="PagesEntities.Page.Revision"/> (same join shape as
        /// <see cref="GetTopRecentlyModifiedPagesInfo"/>/<see cref="GetLatestPageRevisionById"/>), including the
        /// revision <see cref="PagesEntities.PageRevision.Body"/>. No ordering, matching the reference.
        /// </summary>
        public async Task<List<TwPage>> GetAllPages()
        {
            using var context = _createContext();

            return await (from p in context.Pages_Pages
                           join pr in context.Pages_PageRevisions on new { p.Id, p.Revision } equals new { Id = pr.PageId, pr.Revision }
                           select new TwPage
                           {
                               Id = p.Id,
                               Name = p.Name,
                               Description = p.Description,
                               Body = pr.Body,
                               Revision = pr.Revision,
                               Navigation = p.Navigation,
                               CreatedByUserId = p.CreatedByUserId,
                               CreatedDate = p.CreatedDate,
                               ModifiedByUserId = p.ModifiedByUserId,
                               ModifiedDate = p.ModifiedDate,
                           }).ToListAsync();
        }

        /// <summary>
        /// Mirrors GetAllTemplatePages.sql: the same join as <see cref="GetAllPages"/>, additionally filtered to
        /// pages whose <see cref="PagesEntities.Page.Namespace"/> is exactly "Templates" - a literal, hardcoded
        /// string in the reference script, preserved verbatim here.
        /// </summary>
        public async Task<List<TwPage>> GetAllTemplatePages()
        {
            using var context = _createContext();

            return await (from p in context.Pages_Pages
                           join pr in context.Pages_PageRevisions on new { p.Id, p.Revision } equals new { Id = pr.PageId, pr.Revision }
                           where p.Namespace == "Templates"
                           select new TwPage
                           {
                               Id = p.Id,
                               Name = p.Name,
                               Description = p.Description,
                               Body = pr.Body,
                               Revision = pr.Revision,
                               Navigation = p.Navigation,
                               CreatedByUserId = p.CreatedByUserId,
                               CreatedDate = p.CreatedDate,
                               ModifiedByUserId = p.ModifiedByUserId,
                               ModifiedDate = p.ModifiedDate,
                           }).ToListAsync();
        }

        /// <summary>
        /// Mirrors GetAllFeatureTemplates.sql: every Pages.FeatureTemplate row, LEFT OUTER JOINed to its
        /// associated help Pages.Page via the existing <see cref="PagesEntities.FeatureTemplate.Page"/> navigation
        /// rather than a manual join, for <see cref="TwFeatureTemplate.HelpPageNavigation"/>. Cached under
        /// <see cref="MemCache.Category.Configuration"/> with no extra key segments, same as the SQLite reference.
        /// <see cref="TwFeatureTemplate.PageId"/> defaults to 0 when
        /// <see cref="PagesEntities.FeatureTemplate.PageId"/> is null (the reference script selects the nullable
        /// column directly into this non-nullable model property; this is the closest portable equivalent).
        /// </summary>
        public async Task<List<TwFeatureTemplate>> GetAllFeatureTemplates()
        {
            return (await MemCache.AddOrGetAsync(MemCacheKeyFunction.Build(MemCache.Category.Configuration), async () =>
            {
                using var context = _createContext();

                return await context.FeatureTemplates
                    .Select(ft => new TwFeatureTemplate
                    {
                        Name = ft.Name,
                        Type = ft.Type,
                        PageId = ft.PageId ?? 0,
                        Description = ft.Description ?? string.Empty,
                        TemplateText = ft.TemplateText ?? string.Empty,
                        HelpPageNavigation = ft.Page != null ? ft.Page.Navigation : string.Empty,
                    }).ToListAsync();
            })).EnsureNotNull();
        }

        /// <summary>
        /// Mirrors UpdatePageProcessingInstructions.sql: replaces all Pages.PageProcessingInstruction rows for
        /// <paramref name="pageId"/> with <paramref name="instructions"/> (each lower-invarianted then
        /// deduplicated - the reference's own <c>instructions.Select(o => o.ToLowerInvariant()).Distinct()</c>
        /// before building <c>TempInstructions</c> - then dropping any entry that is null/empty, the reference
        /// script's own <c>WHERE Coalesce(TI.[value], '') &lt;&gt; ''</c> insert-time filter), wrapped in a
        /// single transaction (the reference's own <c>BEGIN TRANSACTION</c>/<c>COMMIT TRANSACTION</c>) -
        /// delete-then-insert, not a diff/merge, same pattern as <see cref="UpdatePageTags"/>. An empty
        /// (post-filtering) <paramref name="instructions"/> list still deletes the page's existing instructions
        /// (matching the reference's unconditional <c>DELETE FROM PageProcessingInstruction WHERE PageId =
        /// @PageId</c>) and simply inserts nothing. Unlike <see cref="UpdatePageTags"/>, flushes this page's
        /// cache via <see cref="FlushPageCache"/> afterward - matching the SQLite reference, whose
        /// <c>PageRepository.UpdatePageProcessingInstructions</c> (unlike <c>UpdatePageTags</c>'s) does call it
        /// here.
        /// </summary>
        public async Task UpdatePageProcessingInstructions(int pageId, List<string> instructions)
        {
            var distinctInstructions = instructions
                .Select(i => i.ToLowerInvariant())
                .Distinct()
                .Where(i => !string.IsNullOrEmpty(i))
                .ToList();

            using var context = _createContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            await context.Pages_PageProcessingInstructions
                .Where(pi => pi.PageId == pageId)
                .ExecuteDeleteAsync();

            if (distinctInstructions.Count > 0)
            {
                context.Pages_PageProcessingInstructions.AddRange(distinctInstructions.Select(i => new PagesEntities.PageProcessingInstruction
                {
                    PageId = pageId,
                    Instruction = i,
                }));

                await context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            await FlushPageCache(pageId);
        }

        /// <summary>
        /// Mirrors GetPageRevisionById.sql: an inner join of Pages.Page to Pages.PageRevision for
        /// <paramref name="pageId"/>, at <paramref name="revision"/> (falling back to the page's current
        /// <see cref="PagesEntities.Page.Revision"/> when null), including the revision <see cref="PagesEntities.PageRevision.Body"/>.
        /// <see cref="TwPage.ModifiedByUserId"/>/<see cref="TwPage.ModifiedDate"/> are populated from
        /// <see cref="PagesEntities.Page"/> itself (<c>P.ModifiedByUserId</c>/<c>P.ModifiedDate</c>), not from the
        /// joined <see cref="PagesEntities.PageRevision"/> row - matching the script exactly (contrast with
        /// <see cref="GetPageRevisionInfoById"/>, whose reference script pulls those two columns from <c>PR</c>
        /// instead). Cached under <see cref="MemCache.Category.Page"/>, same cache key shape (pageId + revision)
        /// as the SQLite reference.
        /// </summary>
        public async Task<TwPage?> GetPageRevisionById(int pageId, int? revision = null)
        {
            return await MemCache.AddOrGetAsync(MemCacheKeyFunction.Build(MemCache.Category.Page, [pageId, revision]), async () =>
            {
                using var context = _createContext();

                return await (from p in context.Pages_Pages
                               join pr in context.Pages_PageRevisions on p.Id equals pr.PageId
                               where p.Id == pageId && pr.Revision == (revision ?? p.Revision)
                               select new TwPage
                               {
                                   Id = p.Id,
                                   Name = p.Name,
                                   Description = p.Description,
                                   Body = pr.Body,
                                   Revision = pr.Revision,
                                   Navigation = p.Navigation,
                                   CreatedByUserId = p.CreatedByUserId,
                                   CreatedDate = p.CreatedDate,
                                   ModifiedByUserId = p.ModifiedByUserId,
                                   ModifiedDate = p.ModifiedDate,
                               }).SingleOrDefaultAsync();
            });
        }

        /// <summary>
        /// Mirrors GetSearchTokensByPageId.sql: every Pages.PageToken row for <paramref name="pageId"/>.
        /// </summary>
        public async Task<List<TwPageToken>> GetSearchTokensByPageId(int pageId)
        {
            using var context = _createContext();

            return await context.Pages_PageTokens
                .Where(t => t.PageId == pageId)
                .Select(t => new TwPageToken
                {
                    PageId = t.PageId,
                    Token = t.Token,
                    DoubleMetaphone = t.DoubleMetaphone,
                    Weight = t.Weight,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors SavePageSearchTokens.sql: replaces the Pages.PageToken rows for every page represented in
        /// <paramref name="items"/> (deduplicated - <see cref="TwPageToken.Equals"/> compares PageId and Token
        /// case-insensitively, same as the reference's <c>items.Distinct()</c> before building <c>TempTokens</c>)
        /// with exactly the given rows, wrapped in a single transaction (the reference script's own
        /// <c>BEGIN TRANSACTION</c>/<c>COMMIT TRANSACTION</c>) - delete-then-insert per affected page, not a
        /// diff/merge. An empty <paramref name="items"/> list is a no-op, matching the reference (an empty
        /// <c>TempTokens</c> deletes nothing and inserts nothing).
        /// </summary>
        public async Task SavePageSearchTokens(List<TwPageToken> items)
        {
            var distinctItems = items.Distinct().ToList();

            if (distinctItems.Count == 0)
            {
                return;
            }

            var pageIds = distinctItems.Select(i => i.PageId).Distinct().ToList();

            using var context = _createContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            await context.Pages_PageTokens
                .Where(t => pageIds.Contains(t.PageId))
                .ExecuteDeleteAsync();

            context.Pages_PageTokens.AddRange(distinctItems.Select(i => new PagesEntities.PageToken
            {
                PageId = i.PageId,
                Token = i.Token,
                DoubleMetaphone = i.DoubleMetaphone,
                Weight = i.Weight,
            }));

            await context.SaveChangesAsync();

            await transaction.CommitAsync();
        }

        /// <summary>
        /// Mirrors TruncateAllPageRevisions.sql: for every page, permanently deletes every Pages.PageRevision/
        /// Pages.PageRevisionAttachment/Pages.PageFileRevision row except the one representing that page's
        /// current revision/file-revision, then purges whatever Pages.PageFileRevision/Pages.PageFile rows are
        /// left with no remaining Pages.PageRevisionAttachment reference at all, then resets every remaining
        /// Revision/PageRevision/FileRevision counter (on Pages.Page/Pages.PageRevision/Pages.PageRevisionAttachment/
        /// Pages.PageFileRevision/Pages.PageFile) back to 1 - i.e. every page is left with exactly one revision
        /// (renumbered 1), and every attachment still carries whatever its single surviving file revision was
        /// (also renumbered 1). A no-op unless <paramref name="confirm"/> is exactly "YES" (same guard as the
        /// SQLite reference - "Are you REALLY sure?"). One transaction, matching the SQLite reference's own
        /// <c>o.BeginTransaction()</c>/<c>Commit()</c>/<c>Rollback()</c>.
        /// </summary>
        /// <remarks>
        /// The reference script identifies "the current revision/file-revision to keep" via its own
        /// <c>GROUP BY ... MAX(Revision)</c> subqueries, re-evaluated fresh (against the then-current, shrinking
        /// table contents) before each of its four DELETE statements. This instead resolves "current" the same
        /// way <see cref="GetPageFilesInfoByPageNavigationAndPageRevisionPaged"/>'s own "--Latest file revision."
        /// predicate does (<c>pra.FileRevision == pra.PageFile.Revision</c>) - via the maintained
        /// <see cref="PagesEntities.Page.Revision"/>/<see cref="PagesEntities.PageFile.Revision"/> pointers
        /// themselves, rather than re-deriving the same value via a grouped subquery each time. <see cref="SavePage"/>/
        /// <see cref="UpsertPageFile"/> (specifically their own "reassociate/carry forward only what's still
        /// current" logic) already guarantee these two ways of identifying "the current revision" agree for any
        /// page/file this application has ever written, and this substitution reuses the same, already-proven
        /// navigation-based <c>ExecuteDeleteAsync</c> predicate style as <see cref="DetachPageRevisionAttachment"/>
        /// (phase 2b.7) rather than nesting a <c>GroupBy</c>/<c>Max</c> subquery inside each delete's own
        /// predicate.
        /// </remarks>
        public async Task TruncateAllPageRevisions(string confirm)
        {
            if (confirm != "YES") //Are you REALLY sure?
            {
                return;
            }

            using var context = _createContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                //Deleting non-current page revisions.
                await context.Pages_PageRevisions
                    .Where(pr => context.Pages_Pages.Any(p => p.Id == pr.PageId && p.Revision > pr.Revision))
                    .ExecuteDeleteAsync();

                //Deleting non-current attachments (by file revision).
                await context.Pages_PageRevisionAttachments
                    .Where(a => a.PageFile.Revision > a.FileRevision)
                    .ExecuteDeleteAsync();

                //Deleting non-current page revision attachments (by page revision).
                await context.Pages_PageRevisionAttachments
                    .Where(a => a.Page.Revision > a.PageRevision)
                    .ExecuteDeleteAsync();

                //Deleting non-current page file revisions.
                await context.Pages_PageFileRevisions
                    .Where(fr => fr.PageFile.Revision > fr.Revision)
                    .ExecuteDeleteAsync();

                //Delete orphaned PageFileRevision (no PageRevisionAttachment references it at all anymore).
                await context.Pages_PageFileRevisions
                    .Where(fr => !context.Pages_PageRevisionAttachments.Any(a => a.PageFileId == fr.PageFileId))
                    .ExecuteDeleteAsync();

                //Delete orphaned PageFile.
                await context.Pages_PageFiles
                    .Where(f => !context.Pages_PageRevisionAttachments.Any(a => a.PageFileId == f.Id))
                    .ExecuteDeleteAsync();

                //Assuming everything else worked, set all of the revisions back to 1.
                await context.Pages_Pages.ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Revision, 1));
                await context.Pages_PageRevisions.ExecuteUpdateAsync(setters => setters.SetProperty(pr => pr.Revision, 1));
                await context.Pages_PageRevisionAttachments.ExecuteUpdateAsync(setters => setters
                    .SetProperty(a => a.PageRevision, 1)
                    .SetProperty(a => a.FileRevision, 1));
                await context.Pages_PageFileRevisions.ExecuteUpdateAsync(setters => setters.SetProperty(fr => fr.Revision, 1));
                await context.Pages_PageFiles.ExecuteUpdateAsync(setters => setters.SetProperty(f => f.Revision, 1));

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Mirrors GetCurrentPageRevision.sql: the <see cref="PagesEntities.Page.Revision"/> of the page matching
        /// <paramref name="pageId"/>, or 0 if no such page exists (the script's <c>ExecuteScalarAsync&lt;int&gt;</c>
        /// over a query with no matching row). Cached under <see cref="MemCache.Category.Page"/>, same cache key
        /// shape (pageId) as the SQLite reference.
        /// </summary>
        public async Task<int> GetCurrentPageRevision(int pageId)
        {
            return await MemCache.AddOrGetAsync(MemCacheKeyFunction.Build(MemCache.Category.Page, [pageId]), async () =>
            {
                using var context = _createContext();

                return await context.Pages_Pages
                    .Where(p => p.Id == pageId)
                    .Select(p => p.Revision)
                    .FirstOrDefaultAsync();
            });
        }

        /// <summary>
        /// Mirrors GetLimitedPageInfoByIdAndRevision.sql: an inner join of Pages.Page to Pages.PageRevision for
        /// <paramref name="pageId"/>, at <paramref name="revision"/> (falling back to the page's current
        /// <see cref="PagesEntities.Page.Revision"/> when null), excluding the revision body -
        /// <see cref="TwPage.MostCurrentRevision"/> is populated from <see cref="PagesEntities.Page.Revision"/>
        /// and <see cref="TwPage.Revision"/>/<see cref="TwPage.DataHash"/> from the joined
        /// <see cref="PagesEntities.PageRevision"/> row. <see cref="TwPage.Namespace"/> is deliberately left
        /// unset here even though the reference script also selects the (persisted, redundant) Namespace column -
        /// see <see cref="AutoCompletePage"/>'s doc comment for why. Cached under
        /// <see cref="MemCache.Category.Page"/>, same cache key shape (pageId + revision) as the SQLite reference.
        /// </summary>
        public async Task<TwPage?> GetLimitedPageInfoByIdAndRevision(int pageId, int? revision = null)
        {
            return await MemCache.AddOrGetAsync(MemCacheKeyFunction.Build(MemCache.Category.Page, [pageId, revision]), async () =>
            {
                using var context = _createContext();

                return await (from p in context.Pages_Pages
                               join pr in context.Pages_PageRevisions on p.Id equals pr.PageId
                               where p.Id == pageId && pr.Revision == (revision ?? p.Revision)
                               select new TwPage
                               {
                                   Id = p.Id,
                                   Name = p.Name,
                                   Description = p.Description,
                                   Navigation = p.Navigation,
                                   Revision = pr.Revision,
                                   DataHash = pr.DataHash,
                                   MostCurrentRevision = p.Revision,
                                   CreatedByUserId = p.CreatedByUserId,
                                   CreatedDate = p.CreatedDate,
                                   ModifiedByUserId = p.ModifiedByUserId,
                                   ModifiedDate = p.ModifiedDate,
                               }).SingleOrDefaultAsync();
            });
        }

        /// <summary>
        /// Mirrors GetPageInfoByNavigation.sql: page metadata (excluding content) for the page matching
        /// <paramref name="navigation"/>, or null if no such page exists. Cached under
        /// <see cref="MemCache.Category.Page"/>, same cache key shape (navigation) as the SQLite reference.
        /// </summary>
        public async Task<TwPage?> GetPageInfoByNavigation(string navigation)
        {
            return await MemCache.AddOrGetAsync(MemCacheKeyFunction.Build(MemCache.Category.Page, [navigation]), async () =>
            {
                using var context = _createContext();

                return await context.Pages_Pages
                    .Where(p => p.Navigation == navigation)
                    .Select(p => new TwPage
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Navigation = p.Navigation,
                        Revision = p.Revision,
                        CreatedByUserId = p.CreatedByUserId,
                        CreatedDate = p.CreatedDate,
                        ModifiedByUserId = p.ModifiedByUserId,
                        ModifiedDate = p.ModifiedDate,
                    }).SingleOrDefaultAsync();
            });
        }

        /// <summary>
        /// Mirrors GetPageRevisionCountByNavigation.sql (the script backing this method despite the mismatched
        /// filename - a pre-existing quirk of the SQLite reference, preserved here only in the sense that the
        /// query itself, "count of Pages.PageRevision rows for <paramref name="pageId"/>", is replicated exactly):
        /// the total number of revisions for the page. Cached under <see cref="MemCache.Category.Page"/>, same
        /// cache key shape (pageId) as the SQLite reference.
        /// </summary>
        public async Task<int> GetPageRevisionCountByPageId(int pageId)
        {
            return await MemCache.AddOrGetAsync(MemCacheKeyFunction.Build(MemCache.Category.Page, [pageId]), async () =>
            {
                using var context = _createContext();

                return await context.Pages_PageRevisions.CountAsync(pr => pr.PageId == pageId);
            });
        }

        /// <summary>
        /// SQL Server-specific helper used by <see cref="RestoreDeletedPageByPageId"/> - EF Core's SqlServer
        /// provider does <b>not</b> automatically wrap <c>SET IDENTITY_INSERT ON/OFF</c> around a
        /// <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> call that inserts an explicit value into a
        /// store-generated identity column (confirmed empirically against a live SQL Server/LocalDB database - a
        /// single <c>SaveChangesAsync</c> batching inserts across several tables, only some of which need
        /// <c>IDENTITY_INSERT</c>, fails outright with "Cannot insert explicit value for identity column..."). So
        /// this brackets one identity-column table's own <c>SaveChangesAsync</c> call with the necessary raw SQL
        /// toggle - a no-op (no rows queued, nothing to save) if <paramref name="hasPendingInserts"/> is false.
        /// Guarded by <see cref="TightWikiDbContext.Database"/>'s provider name (the same check
        /// <see cref="TightWikiDbContext.StripNonSqliteNoCaseCollation"/> already uses for the analogous "this is
        /// genuinely SQL-Server-only" situation) rather than referencing the
        /// <c>Microsoft.EntityFrameworkCore.SqlServer</c> package's own <c>IsSqlServer()</c> extension, since this
        /// shared, provider-agnostic project deliberately carries no PackageReference to any specific relational
        /// provider (see this class's own type-level doc comment). Postgres' <c>GENERATED BY DEFAULT AS
        /// IDENTITY</c> columns (Npgsql's own default for <c>int</c> keys) accept an explicit value with no
        /// special session state at all, so a future Postgres driver reusing this same shared code needs no
        /// equivalent branch here.
        /// </summary>
        private static async Task SaveChangesWithIdentityInsertAsync(TightWikiDbContext context, string schemaQualifiedTable, bool hasPendingInserts)
        {
            bool needsToggle = hasPendingInserts
                && context.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true;

            //Table names can't be parameterized in T-SQL, and schemaQualifiedTable is always one of this method's
            //own hardcoded caller-supplied literals ("[Pages].[Page]" etc. - never external/user input), so
            //there is nothing here for the EF1002/EF1003 SQL-injection analyzer to actually protect against -
            //suppressed per its own suggested "make sure the value is sanitized and suppress the warning".
#pragma warning disable EF1002 // Possible SQL injection vulnerability.
            if (needsToggle)
            {
                await context.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT {schemaQualifiedTable} ON");
                try
                {
                    await context.SaveChangesAsync();
                }
                finally
                {
                    await context.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT {schemaQualifiedTable} OFF");
                }
            }
            else
            {
                await context.SaveChangesAsync();
            }
#pragma warning restore EF1002
        }

        /// <summary>
        /// Mirrors RestoreDeletedPageByPageId.sql: the inverse of <see cref="MovePageToDeletedById"/> - copies
        /// the DeletedPages.Page/PageRevision/PageFile/PageFileRevision/PageRevisionAttachment/PageComment rows
        /// for <paramref name="pageId"/> back into the corresponding Pages schema tables, then deletes every
        /// DeletedPages row for the page (including DeletionMeta/PageTag/PageToken/PageProcessingInstruction -
        /// discarded, not restored, a literal quirk of the reference script, which never copies those three
        /// tables back into the Pages schema; a subsequent <see cref="UpsertPage"/>/<see cref="RefreshPageMetadata"/>
        /// on the restored page rebuilds them). One transaction, matching the SQLite reference's own
        /// <c>o.BeginTransaction()</c>/<c>Commit()</c>/<c>Rollback()</c>. Explicit Id values are preserved
        /// verbatim on every re-inserted row (matching the reference's own literal column-for-column
        /// <c>INSERT INTO ... SELECT Id, ...</c>) - Pages.Page/Pages.PageFile/Pages.PageComment are real
        /// store-generated identity columns (unlike their DeletedPages counterparts, explicitly configured
        /// <c>ValueGeneratedNever()</c> - see <see cref="Configurations.DeletedPages.PageConfiguration"/>), so
        /// each of those three tables' inserts is saved in its own dedicated
        /// <see cref="SaveChangesWithIdentityInsertAsync"/> call, in FK-dependency order (Page, then PageFile,
        /// before anything that references either); PageRevision/PageFileRevision/PageRevisionAttachment carry no
        /// single-column identity (composite primary keys), so their inserts are saved together in one ordinary
        /// call once Page/PageFile both exist. Flushes this page's cache via <see cref="FlushPageCache"/>
        /// afterward, same as the SQLite reference.
        /// </summary>
        public async Task RestoreDeletedPageByPageId(int pageId)
        {
            using var context = _createContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var deletedPage = await context.DeletedPages_Pages.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == pageId);

                if (deletedPage != null)
                {
                    context.Pages_Pages.Add(new PagesEntities.Page
                    {
                        Id = deletedPage.Id,
                        Name = deletedPage.Name,
                        Namespace = deletedPage.Namespace,
                        Navigation = deletedPage.Navigation,
                        Description = deletedPage.Description,
                        Revision = deletedPage.Revision,
                        CreatedByUserId = deletedPage.CreatedByUserId,
                        CreatedDate = deletedPage.CreatedDate,
                        ModifiedByUserId = deletedPage.ModifiedByUserId,
                        ModifiedDate = deletedPage.ModifiedDate,
                    });
                }
                await SaveChangesWithIdentityInsertAsync(context, "[Pages].[Page]", deletedPage != null);

                var deletedFiles = await context.DeletedPages_PageFiles.AsNoTracking()
                    .Where(f => f.PageId == pageId).ToListAsync();
                context.Pages_PageFiles.AddRange(deletedFiles.Select(f => new PagesEntities.PageFile
                {
                    Id = f.Id,
                    PageId = f.PageId,
                    Name = f.Name,
                    Navigation = f.Navigation,
                    Revision = f.Revision,
                    CreatedDate = f.CreatedDate,
                }));
                await SaveChangesWithIdentityInsertAsync(context, "[Pages].[PageFile]", deletedFiles.Count > 0);

                var deletedRevisions = await context.DeletedPages_PageRevisions.AsNoTracking()
                    .Where(pr => pr.PageId == pageId).ToListAsync();
                context.Pages_PageRevisions.AddRange(deletedRevisions.Select(pr => new PagesEntities.PageRevision
                {
                    PageId = pr.PageId,
                    Name = pr.Name,
                    Namespace = pr.Namespace,
                    Description = pr.Description,
                    Body = pr.Body,
                    Revision = pr.Revision,
                    ChangeSummary = pr.ChangeSummary,
                    ModifiedByUserId = pr.ModifiedByUserId,
                    ModifiedDate = pr.ModifiedDate,
                    DataHash = pr.DataHash,
                }));

                var deletedFileIds = deletedFiles.Select(f => f.Id).ToList();
                var deletedFileRevisions = await context.DeletedPages_PageFileRevisions.AsNoTracking()
                    .Where(fr => deletedFileIds.Contains(fr.PageFileId)).ToListAsync();
                context.Pages_PageFileRevisions.AddRange(deletedFileRevisions.Select(fr => new PagesEntities.PageFileRevision
                {
                    PageFileId = fr.PageFileId,
                    ContentType = fr.ContentType,
                    Size = fr.Size,
                    CreatedByUserId = fr.CreatedByUserId,
                    CreatedDate = fr.CreatedDate,
                    Data = fr.Data,
                    Revision = fr.Revision,
                    DataHash = fr.DataHash,
                }));

                var deletedAttachments = await context.DeletedPages_PageRevisionAttachments.AsNoTracking()
                    .Where(a => a.PageId == pageId).ToListAsync();
                context.Pages_PageRevisionAttachments.AddRange(deletedAttachments.Select(a => new PagesEntities.PageRevisionAttachment
                {
                    PageId = a.PageId,
                    PageFileId = a.PageFileId,
                    FileRevision = a.FileRevision,
                    PageRevision = a.PageRevision,
                }));

                await context.SaveChangesAsync();

                var deletedComments = await context.DeletedPages_PageComments.AsNoTracking()
                    .Where(c => c.PageId == pageId).ToListAsync();
                context.Pages_PageComments.AddRange(deletedComments.Select(c => new PagesEntities.PageComment
                {
                    Id = c.Id,
                    PageId = c.PageId,
                    CreatedDate = c.CreatedDate,
                    UserId = c.UserId,
                    Body = c.Body,
                }));
                await SaveChangesWithIdentityInsertAsync(context, "[Pages].[PageComment]", deletedComments.Count > 0);

                //Cleanup - discard the deleted page's tags/tokens/processing instructions rather than restoring
                //them (matching the reference script, which never inserts these three tables back into Pages).
                await context.DeletedPages_DeletionMetas.Where(d => d.PageId == pageId).ExecuteDeleteAsync();
                await context.DeletedPages_PageTags.Where(t => t.PageId == pageId).ExecuteDeleteAsync();
                await context.DeletedPages_PageTokens.Where(t => t.PageId == pageId).ExecuteDeleteAsync();
                await context.DeletedPages_PageProcessingInstructions.Where(pi => pi.PageId == pageId).ExecuteDeleteAsync();
                await context.DeletedPages_PageComments.Where(c => c.PageId == pageId).ExecuteDeleteAsync();
                await context.DeletedPages_PageRevisions.Where(pr => pr.PageId == pageId).ExecuteDeleteAsync();
                await context.DeletedPages_PageRevisionAttachments.Where(a => a.PageId == pageId).ExecuteDeleteAsync();
                await context.DeletedPages_PageFileRevisions.Where(fr => deletedFileIds.Contains(fr.PageFileId)).ExecuteDeleteAsync();
                await context.DeletedPages_PageFiles.Where(f => f.PageId == pageId).ExecuteDeleteAsync();
                await context.DeletedPages_Pages.Where(p => p.Id == pageId).ExecuteDeleteAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            await FlushPageCache(pageId);
        }

        /// <summary>
        /// Mirrors MovePageRevisionToDeletedById.sql: copies the single Pages.PageRevision row (and its
        /// Pages.PageRevisionAttachment rows) matching (<paramref name="pageId"/>, <paramref name="revision"/>)
        /// into the corresponding DeletedPageRevisions schema tables, records a DeletedPageRevisions.DeletionMeta
        /// row (<paramref name="userId"/>/now, UTC), then deletes the originals from the Pages schema - one
        /// transaction, matching the SQLite reference's own <c>o.BeginTransaction()</c>/<c>Commit()</c>/
        /// <c>Rollback()</c>. Unlike <see cref="MovePageToDeletedById"/>, this never touches
        /// <see cref="PagesEntities.Page.Revision"/> itself - matching the reference script exactly (this method
        /// is only ever invoked from the UI against a page's non-current, already-superseded revisions, so the
        /// page's own current-revision pointer is left alone). Flushes this page's cache via
        /// <see cref="FlushPageCache"/> afterward, same as the SQLite reference.
        /// </summary>
        public async Task MovePageRevisionToDeletedById(int pageId, int revision, Guid userId)
        {
            var deletedDate = DateTime.UtcNow;

            using var context = _createContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var pageRevision = await context.Pages_PageRevisions.AsNoTracking()
                    .FirstOrDefaultAsync(pr => pr.PageId == pageId && pr.Revision == revision);

                if (pageRevision != null)
                {
                    context.DeletedPageRevisions_PageRevisions.Add(new DeletedPageRevisionsEntities.PageRevision
                    {
                        PageId = pageRevision.PageId,
                        Name = pageRevision.Name,
                        Namespace = pageRevision.Namespace,
                        Description = pageRevision.Description,
                        Body = pageRevision.Body,
                        Revision = pageRevision.Revision,
                        ChangeSummary = pageRevision.ChangeSummary,
                        ModifiedByUserId = pageRevision.ModifiedByUserId,
                        ModifiedDate = pageRevision.ModifiedDate,
                        DataHash = pageRevision.DataHash,
                    });
                }

                var attachments = await context.Pages_PageRevisionAttachments.AsNoTracking()
                    .Where(a => a.PageId == pageId && a.PageRevision == revision).ToListAsync();
                context.DeletedPageRevisions_PageRevisionAttachments.AddRange(attachments.Select(a => new DeletedPageRevisionsEntities.PageRevisionAttachment
                {
                    PageId = a.PageId,
                    PageFileId = a.PageFileId,
                    FileRevision = a.FileRevision,
                    PageRevision = a.PageRevision,
                }));

                context.DeletedPageRevisions_DeletionMetas.Add(new DeletedPageRevisionsEntities.DeletionMeta
                {
                    PageId = pageId,
                    Revision = revision,
                    DeletedByUserId = userId,
                    DeletedDate = deletedDate,
                });

                await context.SaveChangesAsync();

                await context.Pages_PageRevisionAttachments
                    .Where(a => a.PageId == pageId && a.PageRevision == revision)
                    .ExecuteDeleteAsync();

                await context.Pages_PageRevisions
                    .Where(pr => pr.PageId == pageId && pr.Revision == revision)
                    .ExecuteDeleteAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            await FlushPageCache(pageId);
        }

        /// <summary>
        /// Mirrors MovePageToDeletedById.sql: soft-deletes an entire page - orphans any Pages.FeatureTemplate
        /// pointing at it (<see cref="PagesEntities.FeatureTemplate.PageId"/> set to null, the reference script's
        /// own first statement), copies every Pages.PageComment/PageRevision/PageRevisionAttachment/PageFileRevision/
        /// PageFile/Page/PageTag/PageToken/PageProcessingInstruction row for <paramref name="pageId"/> into the
        /// corresponding DeletedPages schema tables (DeletedPages.PageTag carries no Navigation column - only
        /// Tag/PageId are copied, matching <see cref="Entities.DeletedPages.PageTag"/>), records a
        /// DeletedPages.DeletionMeta row (<paramref name="userId"/>/now, UTC), then deletes every one of those
        /// rows from the Pages schema (in the same order as the reference script) plus every Pages.PageReference
        /// row in either direction (as source or as target - discarded, not moved, matching the reference; an
        /// incoming reference simply becomes a broken/missing-page reference again). One transaction, matching
        /// the SQLite reference's own <c>o.BeginTransaction()</c>/<c>Commit()</c>/<c>Rollback()</c>. Explicit Id
        /// values are preserved verbatim on every copied row - unlike <see cref="RestoreDeletedPageByPageId"/>'s
        /// own restore direction, this needs no manual <c>IDENTITY_INSERT</c> handling, since every DeletedPages
        /// table this writes into (Page/PageFile/PageComment) is explicitly configured
        /// <c>ValueGeneratedNever()</c> on its Id column (see e.g. <see cref="Configurations.DeletedPages.PageConfiguration"/>) -
        /// only the corresponding Pages-schema tables are real identity columns. Flushes this page's cache via
        /// <see cref="FlushPageCache"/> afterward, passing the navigation captured before the delete below -
        /// same as the SQLite reference (see that method's doc comment for why a post-delete re-query would fail).
        /// </summary>
        /// <remarks>
        /// <b>Deliberate divergence from the SQLite reference, forced by a real FOREIGN KEY.</b> After committing,
        /// the SQLite reference (<c>PageRepository.MovePageToDeletedById</c>) separately calls
        /// <c>StatisticsRepository.DeletePageStatisticsByPageId</c> <i>outside</i> its own transaction - safe
        /// there only because Statistics.PageStatistics lives in a physically different SQLite database file with
        /// no cross-database FOREIGN KEY enforcement at all. In the consolidated schema,
        /// <see cref="Entities.Statistics.PageStatistic.Page"/> carries a real, enforced FOREIGN KEY against
        /// Pages.Page (see that navigation's own doc comment) - attempting to delete a Pages.Page row while an
        /// unrelated <see cref="TightWikiDbContext.PageStatistics"/> row still references it fails with a
        /// FOREIGN KEY violation (confirmed empirically against a live SQL Server/LocalDB database). So this
        /// cleanup instead runs <i>inside</i> the same transaction, immediately before the Pages.Page delete
        /// below - functionally identical to <see cref="EfStatisticsRepository.DeletePageStatisticsByPageId"/>,
        /// done directly against <see cref="TightWikiDbContext.PageStatistics"/> rather than through an injected
        /// <see cref="ITwStatisticsRepository"/> (this class isn't constructed with one - see this class's own
        /// constructor/type-level remarks).
        /// </remarks>
        public async Task MovePageToDeletedById(int pageId, Guid userId)
        {
            var deletedDate = DateTime.UtcNow;
            string? pageNavigation = null;

            using var context = _createContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                await context.FeatureTemplates
                    .Where(ft => ft.PageId == pageId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(ft => ft.PageId, (int?)null));

                var page = await context.Pages_Pages.AsNoTracking().FirstOrDefaultAsync(p => p.Id == pageId);
                pageNavigation = page?.Navigation;

                var comments = await context.Pages_PageComments.AsNoTracking().Where(c => c.PageId == pageId).ToListAsync();
                context.DeletedPages_PageComments.AddRange(comments.Select(c => new DeletedPagesEntities.PageComment
                {
                    Id = c.Id,
                    PageId = c.PageId,
                    CreatedDate = c.CreatedDate,
                    UserId = c.UserId,
                    Body = c.Body,
                }));

                var revisions = await context.Pages_PageRevisions.AsNoTracking().Where(pr => pr.PageId == pageId).ToListAsync();
                context.DeletedPages_PageRevisions.AddRange(revisions.Select(pr => new DeletedPagesEntities.PageRevision
                {
                    PageId = pr.PageId,
                    Name = pr.Name,
                    Namespace = pr.Namespace,
                    Description = pr.Description,
                    Body = pr.Body,
                    Revision = pr.Revision,
                    ChangeSummary = pr.ChangeSummary,
                    ModifiedByUserId = pr.ModifiedByUserId,
                    ModifiedDate = pr.ModifiedDate,
                    DataHash = pr.DataHash,
                }));

                var attachments = await context.Pages_PageRevisionAttachments.AsNoTracking().Where(a => a.PageId == pageId).ToListAsync();
                context.DeletedPages_PageRevisionAttachments.AddRange(attachments.Select(a => new DeletedPagesEntities.PageRevisionAttachment
                {
                    PageId = a.PageId,
                    PageFileId = a.PageFileId,
                    FileRevision = a.FileRevision,
                    PageRevision = a.PageRevision,
                }));

                var fileIds = await context.Pages_PageFiles.Where(f => f.PageId == pageId).Select(f => f.Id).ToListAsync();

                var fileRevisions = await context.Pages_PageFileRevisions.AsNoTracking()
                    .Where(fr => fileIds.Contains(fr.PageFileId)).ToListAsync();
                context.DeletedPages_PageFileRevisions.AddRange(fileRevisions.Select(fr => new DeletedPagesEntities.PageFileRevision
                {
                    PageFileId = fr.PageFileId,
                    ContentType = fr.ContentType,
                    Size = fr.Size,
                    CreatedByUserId = fr.CreatedByUserId,
                    CreatedDate = fr.CreatedDate,
                    Data = fr.Data,
                    Revision = fr.Revision,
                    DataHash = fr.DataHash,
                }));

                var files = await context.Pages_PageFiles.AsNoTracking().Where(f => f.PageId == pageId).ToListAsync();
                context.DeletedPages_PageFiles.AddRange(files.Select(f => new DeletedPagesEntities.PageFile
                {
                    Id = f.Id,
                    PageId = f.PageId,
                    Name = f.Name,
                    Navigation = f.Navigation,
                    Revision = f.Revision,
                    CreatedDate = f.CreatedDate,
                }));

                if (page != null)
                {
                    context.DeletedPages_Pages.Add(new DeletedPagesEntities.Page
                    {
                        Id = page.Id,
                        Name = page.Name,
                        Namespace = page.Namespace,
                        Navigation = page.Navigation,
                        Description = page.Description,
                        Revision = page.Revision,
                        CreatedByUserId = page.CreatedByUserId,
                        CreatedDate = page.CreatedDate,
                        ModifiedByUserId = page.ModifiedByUserId,
                        ModifiedDate = page.ModifiedDate,
                    });
                }

                var tags = await context.Pages_PageTags.AsNoTracking().Where(t => t.PageId == pageId).ToListAsync();
                context.DeletedPages_PageTags.AddRange(tags.Select(t => new DeletedPagesEntities.PageTag
                {
                    PageId = t.PageId,
                    Tag = t.Tag,
                }));

                var tokens = await context.Pages_PageTokens.AsNoTracking().Where(t => t.PageId == pageId).ToListAsync();
                context.DeletedPages_PageTokens.AddRange(tokens.Select(t => new DeletedPagesEntities.PageToken
                {
                    PageId = t.PageId,
                    Token = t.Token,
                    Weight = t.Weight,
                    DoubleMetaphone = t.DoubleMetaphone,
                }));

                var instructions = await context.Pages_PageProcessingInstructions.AsNoTracking().Where(pi => pi.PageId == pageId).ToListAsync();
                context.DeletedPages_PageProcessingInstructions.AddRange(instructions.Select(pi => new DeletedPagesEntities.PageProcessingInstruction
                {
                    PageId = pi.PageId,
                    Instruction = pi.Instruction,
                }));

                context.DeletedPages_DeletionMetas.Add(new DeletedPagesEntities.DeletionMeta
                {
                    PageId = pageId,
                    DeletedByUserId = userId,
                    DeletedDate = deletedDate,
                });

                await context.SaveChangesAsync();

                //Cleanup - delete everything that was just moved above, plus outgoing/incoming PageReference rows
                //(discarded, not moved - matching the reference script), in the same order as the reference.
                await context.Pages_PageComments.Where(c => c.PageId == pageId).ExecuteDeleteAsync();
                await context.Pages_PageFileRevisions.Where(fr => fileIds.Contains(fr.PageFileId)).ExecuteDeleteAsync();
                await context.Pages_PageRevisionAttachments.Where(a => a.PageId == pageId).ExecuteDeleteAsync();
                await context.Pages_PageFiles.Where(f => f.PageId == pageId).ExecuteDeleteAsync();
                await context.Pages_PageProcessingInstructions.Where(pi => pi.PageId == pageId).ExecuteDeleteAsync();
                await context.PageReferences.Where(pr => pr.PageId == pageId).ExecuteDeleteAsync();
                await context.PageReferences.Where(pr => pr.ReferencesPageId == pageId).ExecuteDeleteAsync();
                await context.Pages_PageRevisions.Where(pr => pr.PageId == pageId).ExecuteDeleteAsync();
                await context.Pages_PageTags.Where(t => t.PageId == pageId).ExecuteDeleteAsync();
                await context.Pages_PageTokens.Where(t => t.PageId == pageId).ExecuteDeleteAsync();

                //Statistics.PageStatistics carries a real FOREIGN KEY against Pages.Page in the consolidated
                //schema (Entities.Statistics.PageStatistic.Page's own doc comment - unlike every *UserId
                //navigation elsewhere in this model, this one is enforced) - see this method's own remarks for
                //why that forces this cleanup to happen here, inside the transaction and before the Pages.Page
                //delete below, rather than after committing like the SQLite reference.
                await context.PageStatistics.Where(ps => ps.PageId == pageId).ExecuteDeleteAsync();

                await context.Pages_Pages.Where(p => p.Id == pageId).ExecuteDeleteAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            await FlushPageCache(pageId, pageNavigation);
        }

        /// <summary>
        /// Mirrors PurgeDeletedPageByPageId.sql plus its own trailing call into
        /// <see cref="PurgeDeletedPageRevisionsByPageId"/>: permanently deletes every DeletedPages row
        /// (DeletionMeta/PageTag/PageToken/PageProcessingInstruction/PageComment/PageRevision/PageRevisionAttachment/
        /// PageFileRevision/PageFile/Page) for <paramref name="pageId"/> in one transaction, then - matching
        /// <c>PageRepository.PurgeDeletedPageByPageId</c>'s own sequencing exactly - separately purges every
        /// DeletedPageRevisions row for the same page via <see cref="PurgeDeletedPageRevisionsByPageId"/> (which
        /// flushes this page's cache on its own), then flushes this page's cache again here too, same as the
        /// SQLite reference (which calls <c>FlushPageCache</c> both inside <c>PurgeDeletedPageRevisionsByPageId</c>
        /// and again at the end of <c>PurgeDeletedPageByPageId</c> itself).
        /// </summary>
        public async Task PurgeDeletedPageByPageId(int pageId)
        {
            using var context = _createContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                await context.DeletedPages_DeletionMetas.Where(d => d.PageId == pageId).ExecuteDeleteAsync();
                await context.DeletedPages_PageTags.Where(t => t.PageId == pageId).ExecuteDeleteAsync();
                await context.DeletedPages_PageTokens.Where(t => t.PageId == pageId).ExecuteDeleteAsync();
                await context.DeletedPages_PageProcessingInstructions.Where(pi => pi.PageId == pageId).ExecuteDeleteAsync();
                await context.DeletedPages_PageComments.Where(c => c.PageId == pageId).ExecuteDeleteAsync();
                await context.DeletedPages_PageRevisions.Where(pr => pr.PageId == pageId).ExecuteDeleteAsync();
                await context.DeletedPages_PageRevisionAttachments.Where(a => a.PageId == pageId).ExecuteDeleteAsync();

                var deletedFileIds = await context.DeletedPages_PageFiles.Where(f => f.PageId == pageId).Select(f => f.Id).ToListAsync();
                await context.DeletedPages_PageFileRevisions.Where(fr => deletedFileIds.Contains(fr.PageFileId)).ExecuteDeleteAsync();

                await context.DeletedPages_PageFiles.Where(f => f.PageId == pageId).ExecuteDeleteAsync();
                await context.DeletedPages_Pages.Where(p => p.Id == pageId).ExecuteDeleteAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            await PurgeDeletedPageRevisionsByPageId(pageId);

            await FlushPageCache(pageId);
        }

        /// <summary>
        /// Mirrors PurgeDeletedPages.sql plus its own trailing call into <see cref="PurgeDeletedPageRevisions"/>:
        /// permanently deletes every row from DeletedPages.PageComment/PageRevision/PageRevisionAttachment/
        /// PageFileRevision/PageFile/Page/DeletionMeta (in that order, matching the reference script exactly),
        /// then separately purges every DeletedPageRevisions row across every page via
        /// <see cref="PurgeDeletedPageRevisions"/>, same as <c>PageRepository.PurgeDeletedPages</c>.
        /// </summary>
        /// <remarks>
        /// <b>Confirmed, deliberately-not-"fixed" gap in the SQLite reference.</b> Unlike
        /// <see cref="PurgeDeletedPageByPageId"/>'s own reference script, PurgeDeletedPages.sql never deletes
        /// DeletedPages.PageTag/PageToken/PageProcessingInstruction - so purging every deleted page still leaves
        /// those three tables' rows behind as orphans (with no owning DeletedPages.Page row left to join back
        /// to). This asymmetry between the single-page and purge-all reference scripts is preserved verbatim here
        /// rather than "fixed" by additionally clearing those three tables, per this class's established
        /// convention of faithfully reproducing the reference's real behavior (see e.g.
        /// <see cref="UpdatePageReferences"/>'s remarks for the same policy applied to an actual bug, as opposed
        /// to this merely-inconsistent-looking omission).
        /// </remarks>
        public async Task PurgeDeletedPages()
        {
            using var context = _createContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                await context.DeletedPages_PageComments.ExecuteDeleteAsync();
                await context.DeletedPages_PageRevisions.ExecuteDeleteAsync();
                await context.DeletedPages_PageRevisionAttachments.ExecuteDeleteAsync();
                await context.DeletedPages_PageFileRevisions.ExecuteDeleteAsync();
                await context.DeletedPages_PageFiles.ExecuteDeleteAsync();
                await context.DeletedPages_Pages.ExecuteDeleteAsync();
                await context.DeletedPages_DeletionMetas.ExecuteDeleteAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            await PurgeDeletedPageRevisions();
        }

        /// <summary>
        /// Mirrors GetCountOfPageAttachmentsById.sql: the count of Pages.PageFile rows for
        /// <paramref name="pageId"/>.
        /// </summary>
        public async Task<int> GetCountOfPageAttachmentsById(int pageId)
        {
            using var context = _createContext();

            return await context.Pages_PageFiles.CountAsync(f => f.PageId == pageId);
        }

        /// <summary>
        /// Mirrors GetDeletedPageById.sql: an inner join of DeletedPages.Page to the DeletedPages.PageRevision row
        /// matching its own <see cref="DeletedPagesEntities.Page.Revision"/>, further inner-joined to the page's
        /// DeletedPages.DeletionMeta row, for <paramref name="pageId"/>. <see cref="TwPage.DeletedByUserId"/> is
        /// deliberately left unset here - the same literal quirk as
        /// <see cref="GetAllDeletedPagesPaged"/>'s own remarks (the reference script selects
        /// <c>DeletedUser.AccountName as DeletedByUserName</c> but never the raw <c>DM.DeletedByUserID</c> column
        /// itself). No caching, matching the SQLite reference.
        /// </summary>
        public async Task<TwPage?> GetDeletedPageById(int pageId)
        {
            using var context = _createContext();

            return await (from p in context.DeletedPages_Pages
                           join pr in context.DeletedPages_PageRevisions on new { p.Id, p.Revision } equals new { Id = pr.PageId, pr.Revision }
                           join dm in context.DeletedPages_DeletionMetas on p.Id equals dm.PageId
                           where p.Id == pageId
                           select new TwPage
                           {
                               Id = p.Id,
                               Name = p.Name,
                               Description = pr.Description,
                               Body = pr.Body,
                               Revision = pr.Revision,
                               MostCurrentRevision = p.Revision,
                               Navigation = p.Navigation,
                               CreatedByUserId = p.CreatedByUserId,
                               CreatedDate = p.CreatedDate,
                               ModifiedByUserId = pr.ModifiedByUserId,
                               ModifiedDate = pr.ModifiedDate,
                               DeletedDate = dm.DeletedDate ?? default,
                               CreatedByUserName = p.CreatedByUser != null ? (p.CreatedByUser.AccountName ?? string.Empty) : string.Empty,
                               ModifiedByUserName = p.ModifiedByUser != null ? (p.ModifiedByUser.AccountName ?? string.Empty) : string.Empty,
                               DeletedByUserName = dm.DeletedByUser != null ? (dm.DeletedByUser.AccountName ?? string.Empty) : string.Empty,
                           }).SingleOrDefaultAsync();
        }

        /// <summary>
        /// Mirrors GetLatestPageRevisionById.sql: an inner join of Pages.Page to the Pages.PageRevision row
        /// matching its own current <see cref="PagesEntities.Page.Revision"/>, for <paramref name="pageId"/>.
        /// <see cref="TwPage.ModifiedByUserId"/>/<see cref="TwPage.ModifiedDate"/> are populated from the joined
        /// <see cref="PagesEntities.PageRevision"/> row, but <see cref="TwPage.ModifiedByUserName"/> is resolved
        /// via <see cref="PagesEntities.Page.ModifiedByUser"/> - i.e. keyed off <c>P.ModifiedByUserId</c>, not
        /// <c>PR.ModifiedByUserId</c> - matching the script's own <c>LEFT OUTER JOIN users_db.Profile as MBU ON
        /// MBU.UserId = P.ModifiedByUserId</c> literally (the two are normally in sync for a page's current
        /// revision, but this preserves the exact join predicate rather than "fixing" it).
        /// </summary>
        public async Task<TwPage?> GetLatestPageRevisionById(int pageId)
        {
            using var context = _createContext();

            return await (from p in context.Pages_Pages
                           join pr in context.Pages_PageRevisions on new { p.Id, p.Revision } equals new { Id = pr.PageId, pr.Revision }
                           where p.Id == pageId
                           select new TwPage
                           {
                               Id = p.Id,
                               Name = p.Name,
                               Description = pr.Description,
                               Body = pr.Body,
                               Revision = pr.Revision,
                               MostCurrentRevision = p.Revision,
                               Navigation = p.Navigation,
                               CreatedByUserId = p.CreatedByUserId,
                               CreatedDate = p.CreatedDate,
                               ModifiedByUserId = pr.ModifiedByUserId,
                               ModifiedDate = pr.ModifiedDate,
                               ModifiedByUserName = p.ModifiedByUser != null ? (p.ModifiedByUser.AccountName ?? string.Empty) : string.Empty,
                           }).SingleOrDefaultAsync();
        }

        /// <summary>
        /// Mirrors GetPageNextRevision.sql: the lowest Pages.PageRevision <see cref="PagesEntities.PageRevision.Revision"/>
        /// for <paramref name="pageId"/> that is greater than <paramref name="revision"/>, or 0 if none exists
        /// (the script's <c>ExecuteScalarAsync&lt;int&gt;</c> over a query with no matching row).
        /// </summary>
        public async Task<int> GetPageNextRevision(int pageId, int revision)
        {
            using var context = _createContext();

            return await context.Pages_PageRevisions
                .Where(pr => pr.PageId == pageId && pr.Revision > revision)
                .OrderBy(pr => pr.Revision)
                .Select(pr => pr.Revision)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Mirrors GetPagePreviousRevision.sql: the highest Pages.PageRevision <see cref="PagesEntities.PageRevision.Revision"/>
        /// for <paramref name="pageId"/> that is less than <paramref name="revision"/>, or 0 if none exists (the
        /// script's <c>ExecuteScalarAsync&lt;int&gt;</c> over a query with no matching row).
        /// </summary>
        public async Task<int> GetPagePreviousRevision(int pageId, int revision)
        {
            using var context = _createContext();

            return await context.Pages_PageRevisions
                .Where(pr => pr.PageId == pageId && pr.Revision < revision)
                .OrderByDescending(pr => pr.Revision)
                .Select(pr => pr.Revision)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Mirrors GetDeletedPageRevisionsByIdPaged.sql: every DeletedPageRevisions.PageRevision row for
        /// <paramref name="pageId"/>, inner-joined to its own DeletedPageRevisions.DeletionMeta row, LEFT OUTER
        /// JOINed to Users.Profile for the deleting user - via the existing
        /// <see cref="DeletedPageRevisionsEntities.DeletionMeta.DeletedByUser"/> navigation rather than the
        /// script's own cross-database <c>o.Attach("users.db", "users_db")</c>. <see cref="TwPage.Id"/> (on the
        /// returned <see cref="TwDeletedPageRevision"/>) is populated from <c>PR.PageId</c>, matching the
        /// reference's own <c>PR.PageId as Id</c>. <paramref name="pageNumber"/> is paginated by the "Pagination
        /// Size" customization setting, same as the reference; <see cref="TwDeletedPageRevision.PaginationPageSize"/>/
        /// <see cref="TwPage.PaginationPageCount"/> are computed via the reference's own ceiling-division formula
        /// against the total (unpaginated) count of DeletedPageRevisions.PageRevision rows for the page - note
        /// this denominator is <i>not</i> further restricted to rows with a matching DeletionMeta row (matching
        /// the reference script's own subquery, which counts <c>[PageRevision]</c> alone with no DeletionMeta
        /// join). Ordering mirrors <c>RepositoryHelpers.TransposeOrderby</c> against the script's <c>--CONFIG::</c>
        /// mapping ("Revision"/"DeletedDate"/"DeletedBy"): no <paramref name="orderBy"/> falls back to the
        /// script's own un-transposed "ORDER BY PR.Revision" (always ascending - no <c>DESC</c> keyword in the
        /// reference), matching <see cref="GetPageRevisionsInfoByNavigationPaged"/>'s convention for an
        /// unrecognized <paramref name="orderBy"/> throwing.
        /// </summary>
        public async Task<List<TwDeletedPageRevision>> GetDeletedPageRevisionsByIdPaged(int pageId, int pageNumber, string? orderBy = null, string? orderByDirection = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            using var context = _createContext();

            var totalCount = await context.DeletedPageRevisions_PageRevisions.CountAsync(pr => pr.PageId == pageId);
            var paginationPageCount = (totalCount + (paginationSize - 1)) / paginationSize;

            var joined = from pr in context.DeletedPageRevisions_PageRevisions
                         join dm in context.DeletedPageRevisions_DeletionMetas on new { pr.PageId, pr.Revision } equals new { dm.PageId, dm.Revision }
                         where pr.PageId == pageId
                         select new { pr, dm };

            bool ascending = string.Equals(orderByDirection, "asc", StringComparison.InvariantCultureIgnoreCase);

            var ordered = string.IsNullOrEmpty(orderBy)
                ? joined.OrderBy(x => x.pr.Revision)
                : orderBy.ToUpperInvariant() switch
                {
                    "REVISION" => ascending ? joined.OrderBy(x => x.pr.Revision) : joined.OrderByDescending(x => x.pr.Revision),
                    "DELETEDDATE" => ascending ? joined.OrderBy(x => x.dm.DeletedDate) : joined.OrderByDescending(x => x.dm.DeletedDate),
                    "DELETEDBY" => ascending
                        ? joined.OrderBy(x => x.dm.DeletedByUser != null ? x.dm.DeletedByUser.AccountName : null)
                        : joined.OrderByDescending(x => x.dm.DeletedByUser != null ? x.dm.DeletedByUser.AccountName : null),
                    _ => throw new InvalidOperationException(
                        $"No order by mapping was found in 'GetDeletedPageRevisionsByIdPaged.sql' for the field '{orderBy}'."),
                };

            return await ordered
                .Skip((pageNumber - 1) * paginationSize)
                .Take(paginationSize)
                .Select(x => new TwDeletedPageRevision
                {
                    Id = x.pr.PageId,
                    Name = x.pr.Name,
                    Description = x.pr.Description,
                    Revision = x.pr.Revision,
                    DeletedDate = x.dm.DeletedDate ?? default,
                    DeletedByUserName = x.dm.DeletedByUser != null ? (x.dm.DeletedByUser.AccountName ?? string.Empty) : string.Empty,
                    PaginationPageSize = paginationSize,
                    PaginationPageCount = paginationPageCount,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors PurgeDeletedPageRevisions.sql: permanently deletes every row from
        /// DeletedPageRevisions.PageRevision/PageRevisionAttachment/DeletionMeta (in that order, matching the
        /// reference script exactly), across every page. One transaction, matching the same pattern as
        /// <see cref="PurgeDeletedPages"/>.
        /// </summary>
        public async Task PurgeDeletedPageRevisions()
        {
            using var context = _createContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                await context.DeletedPageRevisions_PageRevisions.ExecuteDeleteAsync();
                await context.DeletedPageRevisions_PageRevisionAttachments.ExecuteDeleteAsync();
                await context.DeletedPageRevisions_DeletionMetas.ExecuteDeleteAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Mirrors PurgeDeletedPageRevisionsByPageId.sql: permanently deletes every DeletedPageRevisions.PageRevision/
        /// PageRevisionAttachment/DeletionMeta row for <paramref name="pageId"/> (in that order, matching the
        /// reference script exactly). One transaction, same pattern as <see cref="PurgeDeletedPageRevisions"/>.
        /// Flushes this page's cache via <see cref="FlushPageCache"/> afterward, same as the SQLite reference.
        /// </summary>
        public async Task PurgeDeletedPageRevisionsByPageId(int pageId)
        {
            using var context = _createContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                await context.DeletedPageRevisions_PageRevisions.Where(pr => pr.PageId == pageId).ExecuteDeleteAsync();
                await context.DeletedPageRevisions_PageRevisionAttachments.Where(a => a.PageId == pageId).ExecuteDeleteAsync();
                await context.DeletedPageRevisions_DeletionMetas.Where(dm => dm.PageId == pageId).ExecuteDeleteAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            await FlushPageCache(pageId);
        }

        /// <summary>
        /// Mirrors PurgeDeletedPageRevisionByPageIdAndRevision.sql: permanently deletes the single
        /// DeletedPageRevisions.PageRevision/PageRevisionAttachment/DeletionMeta row(s) matching
        /// (<paramref name="pageId"/>, <paramref name="revision"/>). One transaction, same pattern as
        /// <see cref="PurgeDeletedPageRevisions"/>. Flushes this page's cache via <see cref="FlushPageCache"/>
        /// afterward, same as the SQLite reference.
        /// </summary>
        public async Task PurgeDeletedPageRevisionByPageIdAndRevision(int pageId, int revision)
        {
            using var context = _createContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                await context.DeletedPageRevisions_PageRevisions.Where(pr => pr.PageId == pageId && pr.Revision == revision).ExecuteDeleteAsync();
                await context.DeletedPageRevisions_PageRevisionAttachments.Where(a => a.PageId == pageId && a.PageRevision == revision).ExecuteDeleteAsync();
                await context.DeletedPageRevisions_DeletionMetas.Where(dm => dm.PageId == pageId && dm.Revision == revision).ExecuteDeleteAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            await FlushPageCache(pageId);
        }

        /// <summary>
        /// Mirrors RestoreDeletedPageRevisionByPageIdAndRevision.sql: the inverse of
        /// <see cref="MovePageRevisionToDeletedById"/> - copies the DeletedPageRevisions.PageRevision/
        /// PageRevisionAttachment row(s) matching (<paramref name="pageId"/>, <paramref name="revision"/>) back
        /// into the Pages schema, then deletes the DeletedPageRevisions.DeletionMeta/PageRevisionAttachment/
        /// PageRevision originals (in that order, matching the reference script exactly). Despite this method
        /// living on the "DeletedPageRevisions side" of the interface, the reference script's own restore
        /// direction is into the Pages schema (its own <c>o.Attach("pages.db", "pages_db")</c>, run from
        /// <c>DeletedPageRevisionsFactory</c>) - i.e. the opposite direction from every other DeletedPageRevisions
        /// method here, which all operate purely within that one schema. One transaction, matching the SQLite
        /// reference's own <c>o.BeginTransaction()</c>/<c>Commit()</c>/<c>Rollback()</c>... except the reference
        /// itself has no explicit transaction here (a bare <c>DeletedPageRevisionsFactory.EphemeralAsync</c> call
        /// with no <c>BeginTransaction</c>/<c>Commit</c>) - a transaction is still used here anyway, matching the
        /// pattern established by every sibling move/restore method in this class
        /// (<see cref="RestoreDeletedPageByPageId"/>/<see cref="MovePageRevisionToDeletedById"/>/
        /// <see cref="MovePageToDeletedById"/>), since a multi-table copy-then-delete should not be allowed to
        /// commit only partially. Flushes this page's cache via <see cref="FlushPageCache"/> afterward, same as
        /// the SQLite reference.
        /// </summary>
        public async Task RestoreDeletedPageRevisionByPageIdAndRevision(int pageId, int revision)
        {
            using var context = _createContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var deletedRevision = await context.DeletedPageRevisions_PageRevisions.AsNoTracking()
                    .FirstOrDefaultAsync(pr => pr.PageId == pageId && pr.Revision == revision);

                if (deletedRevision != null)
                {
                    context.Pages_PageRevisions.Add(new PagesEntities.PageRevision
                    {
                        PageId = deletedRevision.PageId,
                        Name = deletedRevision.Name,
                        Namespace = deletedRevision.Namespace,
                        Description = deletedRevision.Description,
                        Body = deletedRevision.Body,
                        Revision = deletedRevision.Revision,
                        ChangeSummary = deletedRevision.ChangeSummary,
                        ModifiedByUserId = deletedRevision.ModifiedByUserId,
                        ModifiedDate = deletedRevision.ModifiedDate,
                        DataHash = deletedRevision.DataHash,
                    });
                }

                var deletedAttachments = await context.DeletedPageRevisions_PageRevisionAttachments.AsNoTracking()
                    .Where(a => a.PageId == pageId && a.PageRevision == revision).ToListAsync();
                context.Pages_PageRevisionAttachments.AddRange(deletedAttachments.Select(a => new PagesEntities.PageRevisionAttachment
                {
                    PageId = a.PageId,
                    PageFileId = a.PageFileId,
                    FileRevision = a.FileRevision,
                    PageRevision = a.PageRevision,
                }));

                await context.SaveChangesAsync();

                await context.DeletedPageRevisions_DeletionMetas.Where(dm => dm.PageId == pageId && dm.Revision == revision).ExecuteDeleteAsync();
                await context.DeletedPageRevisions_PageRevisionAttachments.Where(a => a.PageId == pageId && a.PageRevision == revision).ExecuteDeleteAsync();
                await context.DeletedPageRevisions_PageRevisions.Where(pr => pr.PageId == pageId && pr.Revision == revision).ExecuteDeleteAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            await FlushPageCache(pageId);
        }

        /// <summary>
        /// Mirrors GetDeletedPageRevisionById.sql: the single DeletedPageRevisions.PageRevision row matching
        /// (<paramref name="pageId"/>, <paramref name="revision"/>), inner-joined to its own
        /// DeletedPageRevisions.DeletionMeta row, LEFT OUTER JOINed to Users.Profile for the deleting user via the
        /// existing <see cref="DeletedPageRevisionsEntities.DeletionMeta.DeletedByUser"/> navigation. Uses
        /// <c>FirstOrDefaultAsync</c> rather than <c>SingleOrDefaultAsync</c>, matching the reference's own
        /// <c>QueryFirstOrDefaultAsync</c> (both keys are already unique per their composite primary keys, so this
        /// is a difference in defensive posture only, not in observable behavior).
        /// </summary>
        public async Task<TwDeletedPageRevision?> GetDeletedPageRevisionById(int pageId, int revision)
        {
            using var context = _createContext();

            return await (from pr in context.DeletedPageRevisions_PageRevisions
                           join dm in context.DeletedPageRevisions_DeletionMetas on new { pr.PageId, pr.Revision } equals new { dm.PageId, dm.Revision }
                           where pr.PageId == pageId && pr.Revision == revision
                           select new TwDeletedPageRevision
                           {
                               Id = pr.PageId,
                               Name = pr.Name,
                               Description = pr.Description,
                               Revision = pr.Revision,
                               Body = pr.Body,
                               DeletedDate = dm.DeletedDate ?? default,
                               DeletedByUserName = dm.DeletedByUser != null ? (dm.DeletedByUser.AccountName ?? string.Empty) : string.Empty,
                           }).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Shared query behind both <see cref="GetPageRevisionByNavigation(TwNamespaceNavigation, int?)"/> and
        /// <see cref="GetPageRevisionByNavigation(string, int?, bool)"/> - mirrors GetPageRevisionByNavigation.sql:
        /// an inner join of Pages.Page to Pages.PageRevision for <paramref name="canonicalNavigation"/>, at
        /// <paramref name="revision"/> (falling back to the page's current <see cref="PagesEntities.Page.Revision"/>
        /// when null). <see cref="TwPage.ModifiedByUserName"/> is resolved via
        /// <see cref="PagesEntities.Page.ModifiedByUser"/> (keyed off <c>P.ModifiedByUserId</c>, not
        /// <c>PR.ModifiedByUserId</c> - matching the script's own <c>LEFT OUTER JOIN users_db.Profile as MBU ON
        /// MBU.UserId = P.ModifiedByUserId</c> literally, same quirk as <see cref="GetLatestPageRevisionById"/>),
        /// while <see cref="TwPage.CreatedByUserName"/> is resolved via <see cref="PagesEntities.Page.CreatedByUser"/>.
        /// <see cref="TwPage.HigherRevisionCount"/> is computed the same way as the script's own correlated
        /// subquery (count of sibling revisions with a higher <see cref="PagesEntities.PageRevision.Revision"/>).
        /// </summary>
        private static async Task<TwPage?> QueryPageRevisionByNavigation(TightWikiDbContext context, string canonicalNavigation, int? revision)
        {
            return await (from p in context.Pages_Pages
                           join pr in context.Pages_PageRevisions on p.Id equals pr.PageId
                           where p.Navigation == canonicalNavigation && pr.Revision == (revision ?? p.Revision)
                           select new TwPage
                           {
                               Id = p.Id,
                               Name = p.Name,
                               Description = pr.Description,
                               Body = pr.Body,
                               Revision = pr.Revision,
                               ChangeSummary = pr.ChangeSummary ?? string.Empty,
                               MostCurrentRevision = p.Revision,
                               Navigation = p.Navigation,
                               CreatedByUserId = p.CreatedByUserId,
                               CreatedDate = p.CreatedDate,
                               ModifiedByUserId = pr.ModifiedByUserId,
                               ModifiedDate = pr.ModifiedDate,
                               ModifiedByUserName = p.ModifiedByUser != null ? (p.ModifiedByUser.AccountName ?? string.Empty) : string.Empty,
                               CreatedByUserName = p.CreatedByUser != null ? (p.CreatedByUser.AccountName ?? string.Empty) : string.Empty,
                               HigherRevisionCount = context.Pages_PageRevisions.Count(ipr => ipr.PageId == p.Id && ipr.Revision > pr.Revision),
                           }).SingleOrDefaultAsync();
        }

        /// <summary>
        /// Mirrors <c>PageRepository.GetPageRevisionByNavigation(TwNamespaceNavigation, int?)</c>: looks up via
        /// <see cref="QueryPageRevisionByNavigation"/> using <paramref name="navigation"/>'s
        /// <see cref="TwNamespaceNavigation.Canonical"/> form. No caching, matching the SQLite reference (unlike
        /// the string overload below).
        /// </summary>
        public async Task<TwPage?> GetPageRevisionByNavigation(TwNamespaceNavigation navigation, int? revision = null)
        {
            using var context = _createContext();

            return await QueryPageRevisionByNavigation(context, navigation.Canonical, revision);
        }

        /// <summary>
        /// Mirrors <c>PageRepository.GetPageRevisionByNavigation(string, int?, bool)</c>: normalizes
        /// <paramref name="givenNavigation"/> via <see cref="TwNamespaceNavigation"/> and looks up via
        /// <see cref="QueryPageRevisionByNavigation"/>. Cached under <see cref="MemCache.Category.Page"/>, same
        /// cache key shape (canonical navigation + revision) as the SQLite reference; if
        /// <paramref name="refreshCache"/> is true, the cache entry is evicted first via <see cref="MemCache.Remove"/>,
        /// same as the reference.
        /// </summary>
        public async Task<TwPage?> GetPageRevisionByNavigation(string givenNavigation, int? revision = null, bool refreshCache = false)
        {
            var navigation = new TwNamespaceNavigation(givenNavigation);

            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.Page, [navigation.Canonical, revision]);

            if (refreshCache)
            {
                MemCache.Remove(cacheKey);
            }

            return await MemCache.AddOrGetAsync(cacheKey, async () =>
            {
                using var context = _createContext();

                return await QueryPageRevisionByNavigation(context, navigation.Canonical, revision);
            });
        }

        /// <summary>
        /// Mirrors GetAssociatedTags.sql: every distinct tag applied to any page that itself carries the tag
        /// matching <paramref name="tag"/>'s navigation (a "tags that co-occur with this tag" query, used for
        /// tag-cloud/related-tag browsing) - resolved in two steps (the pages carrying <paramref name="tag"/>,
        /// then every Pages.PageTag row belonging to those pages) rather than the reference script's own
        /// self-join, since the self-join's <c>Interm</c> alias is provably redundant (it only re-selects
        /// <paramref name="tag"/>'s own Pages.PageTag row, already implied by <c>Root</c>). <see cref="TwTagAssociation.Tag"/>
        /// is the alphabetically-last literal spelling among all pages sharing that tag's navigation (the
        /// reference's own <c>MAX(Extent.Tag)</c>); <see cref="TwTagAssociation.PageCount"/> is the count of
        /// distinct pages carrying it (the reference's own <c>COUNT(DISTINCT Extent.PageId)</c> - defensive
        /// against a page somehow carrying the same tag Navigation twice, which the table's own composite primary
        /// key (PageId, Tag) already prevents in practice for any single literal spelling, but not necessarily
        /// across two different-cased spellings that both clean to the same Navigation). No ordering, capped at
        /// 100 rows, matching the reference's own un-ordered <c>LIMIT 100</c>. Grouping/aggregation is done
        /// client-side over the (page-count-bounded) candidate rows rather than via a database
        /// <c>GROUP BY</c>/<c>COUNT(DISTINCT ...)</c>, to sidestep any provider-translation risk for a nested
        /// distinct-count inside a grouped projection.
        /// </summary>
        public async Task<List<TwTagAssociation>> GetAssociatedTags(string tag)
        {
            using var context = _createContext();

            var matchingPageIds = await context.Pages_PageTags
                .Where(t => t.Navigation == tag)
                .Select(t => t.PageId)
                .Distinct()
                .ToListAsync();

            if (matchingPageIds.Count == 0)
            {
                return new List<TwTagAssociation>();
            }

            var candidateTags = await context.Pages_PageTags
                .Where(t => matchingPageIds.Contains(t.PageId))
                .Select(t => new { t.PageId, t.Tag, t.Navigation })
                .ToListAsync();

            return candidateTags
                .GroupBy(t => t.Navigation)
                .Select(g => new TwTagAssociation
                {
                    Tag = g.Max(x => x.Tag)!,
                    PageCount = g.Select(x => x.PageId).Distinct().Count(),
                })
                .Take(100)
                .ToList();
        }

        /// <summary>
        /// Mirrors GetPageInfoByNamespaces.sql: page metadata (excluding content) for every page whose
        /// <see cref="PagesEntities.Page.Namespace"/> is one of <paramref name="namespaces"/>, resolved via
        /// <c>Contains(...)</c> (the <c>TempNamespaces</c> replacement, same pattern as
        /// <see cref="GetAllPagesPaged"/>'s remarks). The reference script's own <c>SELECT DISTINCT</c> is not
        /// reproduced as an explicit <c>Distinct()</c> call - <see cref="PagesEntities.Page.Id"/> is already
        /// unique, so a <c>Contains(...)</c> filter (unlike the reference's own join against a temp table that
        /// could contain duplicate namespace values) can never itself produce duplicate page rows.
        /// </summary>
        public async Task<List<TwPage>> GetPageInfoByNamespaces(List<string> namespaces)
        {
            using var context = _createContext();

            return await context.Pages_Pages
                .Where(p => namespaces.Contains(p.Namespace))
                .Select(p => new TwPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Navigation = p.Navigation,
                    CreatedByUserId = p.CreatedByUserId,
                    CreatedDate = p.CreatedDate,
                    ModifiedByUserId = p.ModifiedByUserId,
                    ModifiedDate = p.ModifiedDate,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors GetPageInfoByTags.sql: page metadata (excluding content) for every page carrying at least one
        /// tag whose <see cref="TwNavigation.Clean"/>-cleaned navigation matches one of <paramref name="tags"/>
        /// (each cleaned the same way, matching the reference's own <c>cleanedTags = tags.Select(TwNavigation.Clean)</c>).
        /// Resolved in two steps - matching page IDs via <c>Contains(...)</c> against
        /// <see cref="PagesEntities.PageTag.Navigation"/> (the <c>TempTags</c> replacement, same pattern as
        /// <see cref="GetAllPagesPaged"/>'s remarks), then the page rows themselves - rather than a single joined
        /// query, so the reference script's own <c>SELECT DISTINCT</c> (needed there because a page with multiple
        /// matching tags would otherwise join multiple times) has a natural equivalent: an intermediate
        /// <c>Distinct()</c> on page IDs.
        /// </summary>
        public async Task<List<TwPage>> GetPageInfoByTags(IEnumerable<string> tags)
        {
            var cleanedTags = tags.Select(t => TwNavigation.Clean(t)).ToList();

            using var context = _createContext();

            var pageIds = await context.Pages_PageTags
                .Where(t => cleanedTags.Contains(t.Navigation))
                .Select(t => t.PageId)
                .Distinct()
                .ToListAsync();

            return await context.Pages_Pages
                .Where(p => pageIds.Contains(p.Id))
                .Select(p => new TwPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Navigation = p.Navigation,
                    CreatedByUserId = p.CreatedByUserId,
                    CreatedDate = p.CreatedDate,
                    ModifiedByUserId = p.ModifiedByUserId,
                    ModifiedDate = p.ModifiedDate,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors <c>PageRepository.GetPageInfoByTag</c>: delegates to <see cref="GetPageInfoByTags"/> with a
        /// single-element list - both the reference implementation and this one clean <paramref name="tag"/> the
        /// same way (<see cref="TwNavigation.Clean"/>) and run the exact same underlying query
        /// (GetPageInfoByTags.sql, despite the reference building its own single-entry <c>TempTags</c> table
        /// separately rather than calling its own <c>GetPageInfoByTags</c> method), so delegating here reduces
        /// duplication without changing behavior.
        /// </summary>
        public async Task<List<TwPage>> GetPageInfoByTag(string tag)
            => await GetPageInfoByTags([tag]);

        /// <summary>
        /// Mirrors UpdatePageTags.sql: replaces all Pages.PageTag rows for <paramref name="pageId"/> with
        /// <paramref name="tags"/> (deduplicated by cleaned <see cref="TwNavigation.Clean"/> navigation, keeping
        /// the first literal spelling per navigation - the reference's own <c>DistinctBy(o => o.Navigation)</c> -
        /// then dropping any entry whose literal tag text is null/empty, the reference script's own <c>WHERE
        /// Coalesce(T.[Tag], '') &lt;&gt; ''</c> insert-time filter), wrapped in a single transaction (the
        /// reference's own <c>BEGIN TRANSACTION</c>/<c>COMMIT TRANSACTION</c>) - delete-then-insert, not a
        /// diff/merge. An empty (post-filtering) <paramref name="tags"/> list still deletes the page's existing
        /// tags (matching the reference's unconditional <c>DELETE FROM PageTag WHERE PageId = @PageId</c>) and
        /// simply inserts nothing.
        /// </summary>
        public async Task UpdatePageTags(int pageId, List<string> tags)
        {
            var paramTags = tags
                .Select(t => new { Tag = t, Navigation = TwNavigation.Clean(t) })
                .DistinctBy(t => t.Navigation)
                .Where(t => !string.IsNullOrEmpty(t.Tag))
                .ToList();

            using var context = _createContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            await context.Pages_PageTags
                .Where(t => t.PageId == pageId)
                .ExecuteDeleteAsync();

            if (paramTags.Count > 0)
            {
                context.Pages_PageTags.AddRange(paramTags.Select(t => new PagesEntities.PageTag
                {
                    PageId = pageId,
                    Tag = t.Tag,
                    Navigation = t.Navigation,
                }));

                await context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
        }

        /// <summary>
        /// Mirrors <c>PageRepository.UpsertPage</c>: saves <paramref name="page"/> (and its revision history) via
        /// <see cref="SavePage"/>, refreshes its tags/processing-instructions/search-tokens/outgoing-references
        /// via <see cref="RefreshPageMetadata"/>, and - only when <paramref name="page"/>.Id was 0 on entry (a
        /// brand-new page) - resolves any pre-existing orphaned Pages.PageReference rows that pointed at this
        /// page's navigation before it existed, via <see cref="UpdateSinglePageReference"/>. Same three-step
        /// orchestration, same order, as the SQLite reference.
        /// </summary>
        public async Task<int> UpsertPage(ITwEngine wikifier, ITwSharedLocalizationText localizer, TwPage page, ITwSessionState? sessionState = null)
        {
            bool isNewlyCreated = page.Id == 0;

            page.Id = await SavePage(page);

            await RefreshPageMetadata(wikifier, localizer, page, sessionState);

            if (isNewlyCreated)
            {
                //This will update the PageId of references that have been saved to the navigation link.
                await UpdateSinglePageReference(page.Navigation, page.Id);
            }

            return page.Id;
        }

        /// <summary>
        /// Mirrors <c>PageRepository.RefreshPageMetadata</c>: re-transforms <paramref name="page"/> through
        /// <paramref name="wikifier"/> (omitting <see cref="TwMatchType.StandardFunction"/> matches from
        /// tokenization, same as the reference - function calls are too dynamic for static searching), then
        /// rewrites this page's tags (<see cref="UpdatePageTags"/>), processing instructions
        /// (<see cref="UpdatePageProcessingInstructions"/>), search tokens (<see cref="ParsePageTokens"/> +
        /// <see cref="SavePageSearchTokens"/>), and outgoing references (<see cref="UpdatePageReferences"/>) from
        /// the resulting <see cref="ITwEngineState"/>, then clears this page's cache under both its id and its
        /// navigation - the same two <see cref="MemCache.ClearCategory(MemCacheKey)"/> calls as the SQLite
        /// reference (in addition to whatever caches the individual Update*/Save* calls above already flush on
        /// their own).
        /// </summary>
        public async Task RefreshPageMetadata(ITwEngine wikifier, ITwSharedLocalizationText localizer, TwPage page, ITwSessionState? sessionState = null)
        {
            //We omit function calls from the tokenization process because they are too dynamic for static searching.
            var state = await wikifier.Transform(localizer, sessionState, page, null, [TwMatchType.StandardFunction]);

            await UpdatePageTags(page.Id, state.Tags);
            await UpdatePageProcessingInstructions(page.Id, state.ProcessingInstructions);

            var pageTokens = (await ParsePageTokens(state)).Select(o =>
                      new TwPageToken
                      {
                          PageId = page.Id,
                          Token = o.Token,
                          DoubleMetaphone = o.DoubleMetaphone,
                          Weight = o.Weight
                      }).ToList();

            await SavePageSearchTokens(pageTokens);
            await UpdatePageReferences(page.Id, state.OutgoingLinks);

            MemCache.ClearCategory(MemCacheKey.Build(MemCache.Category.Page, [page.Id]));
            MemCache.ClearCategory(MemCacheKey.Build(MemCache.Category.Page, [page.Navigation]));
        }

        /// <summary>
        /// Mirrors <c>PageRepository.ParsePageTokens</c>: tokenizes <paramref name="state"/>'s rendered HTML,
        /// page description, tags, and page name (each with its own weight multiplier - 1/1.2/1.4/1.6
        /// respectively, same as the reference), then aggregates by token text into one row per distinct token
        /// with its <see cref="TwAggregatedSearchToken.DoubleMetaphone"/> code and summed weight, via
        /// <see cref="ComputeParsedPageTokens"/>.
        /// </summary>
        /// <remarks>
        /// This method - and the private <see cref="ComputeParsedPageTokens"/> helper it calls - has no
        /// dependency on <see cref="RefreshPageMetadata"/>/<see cref="UpsertPage"/> (only the other way around:
        /// <see cref="RefreshPageMetadata"/> is this method's own caller), so it was already fully functional in
        /// phase 2b.4 (when it landed) even though <see cref="RefreshPageMetadata"/> itself was still a
        /// <see cref="NotImplementedException"/> stub at the time (implemented for real in phase 2b.6).
        /// </remarks>
        public async Task<List<TwAggregatedSearchToken>> ParsePageTokens(ITwEngineState state)
        {
            var parsedTokens = new List<WeightedSearchToken>();

            parsedTokens.AddRange(await ComputeParsedPageTokens(state.HtmlResult, 1));
            parsedTokens.AddRange(await ComputeParsedPageTokens(state.Page.Description, 1.2));
            parsedTokens.AddRange(await ComputeParsedPageTokens(string.Join(" ", state.Tags), 1.4));
            parsedTokens.AddRange(await ComputeParsedPageTokens(state.Page.Name, 1.6));

            return parsedTokens
                .GroupBy(o => o.Token)
                .Select(o => new TwAggregatedSearchToken
                {
                    Token = o.Key,
                    DoubleMetaphone = o.Key.ToDoubleMetaphone(),
                    Weight = o.Sum(g => g.Weight),
                }).ToList();
        }

        /// <summary>
        /// Mirrors <c>PageRepository.ComputeParsedPageTokens</c>: strips HTML from <paramref name="content"/>
        /// (<c>NTDLS.Helpers.Html.StripHtml</c>), splits on whitespace/hyphen/underscore, optionally also splits
        /// camel-cased tokens into their component words (<c>NTDLS.Helpers.Text.SplitCamelCase</c>, gated by the
        /// "Split Camel Case" search setting - split words are added alongside the original tokens, not in place
        /// of them, same as the reference), lower-invariants everything, drops any token listed in the
        /// "Word Exclusions" search setting (comma/semicolon-separated), then groups by token text into a
        /// per-token count-based weight (occurrence count * <paramref name="weightMultiplier"/>), dropping
        /// whitespace-only tokens.
        /// </summary>
        private async Task<List<WeightedSearchToken>> ComputeParsedPageTokens(string content, double weightMultiplier)
        {
            var searchConfig = await _configurationRepository.GetConfigurationEntryValuesByGroupName(TwConfigGroup.Search);

            var exclusionWords = searchConfig?.Value<string>("Word Exclusions")?
                .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries).Distinct() ?? new List<string>();
            var strippedContent = Html.StripHtml(content);

            var tokens = strippedContent.Split([' ', '\n', '\t', '-', '_']).ToList();

            if (searchConfig?.Value<bool>("Split Camel Case") == true)
            {
                var allSplitTokens = new List<string>();

                foreach (var token in tokens)
                {
                    var splitTokens = Text.SplitCamelCase(token);
                    if (splitTokens.Count > 1)
                    {
                        splitTokens.ForEach(t => allSplitTokens.Add(t));
                    }
                }

                tokens.AddRange(allSplitTokens);
            }

            tokens = tokens.ConvertAll(d => d.ToLowerInvariant());

            tokens.RemoveAll(o => exclusionWords.Contains(o));

            var searchTokens = (from w in tokens
                                 group w by w into g
                                 select new WeightedSearchToken
                                 {
                                     Token = g.Key,
                                     Weight = g.Count() * weightMultiplier
                                 }).ToList();

            return searchTokens.Where(o => string.IsNullOrWhiteSpace(o.Token) == false).ToList();
        }

        /// <summary>
        /// Creates a new page or updates an existing page and its revision history in the data store.
        ///
        /// DO NOT USE DIRECTLY: Use <see cref="UpsertPage"/> instead.
        /// </summary>
        /// <remarks>
        /// Mirrors <c>PageRepository.SavePage</c>'s hash-based change detection and revision-bumping, one
        /// transaction (the reference's own <c>o.BeginTransaction()</c>/<c>Commit()</c>/<c>Rollback()</c>) per
        /// call:
        /// <list type="number">
        /// <item>A brand-new page (<paramref name="page"/>.Id == 0) is simply inserted (Revision hardcoded to 1,
        /// matching CreatePage.sql), and is therefore always treated as "changed".</item>
        /// <item>An existing page has its current, cached <see cref="TwPage.Revision"/>/<see cref="TwPage.DataHash"/>
        /// read via <see cref="GetLimitedPageInfoByIdAndRevision"/> (throwing if the page can no longer be found,
        /// same as the reference) <i>before</i> Pages.Page's mutable columns (Description/Name/Namespace/
        /// Navigation/ModifiedByUserId/ModifiedDate - matching UpdatePage.sql's own column list exactly, and
        /// deliberately excluding Revision, which is only ever bumped in step 3 below) are overwritten - "changed"
        /// is then whichever of Name/Namespace/Description/ChangeSummary/DataHash (a CRC32 of
        /// <paramref name="page"/>.Body, <see cref="TightWiki.Library.Security.SecurityUtility.Crc32(string)"/>,
        /// same algorithm as the reference) differs from what was just read, all compared the same
        /// ordinal/case-sensitive way in C# both here and in the reference (Dapper materializes the "current"
        /// row into a <see cref="TwPage"/> first on that side too, so there is no SQL-collation involved in this
        /// comparison on either side).</item>
        /// <item>Only when "changed": the page's <see cref="PagesEntities.Page.Revision"/> is bumped
        /// (UpdatePageRevisionNumber.sql), a new Pages.PageRevision snapshot row is inserted at that revision
        /// number (InsertPageRevision.sql - its own reference script's <c>FROM [Page] WHERE Id = @PageId</c>
        /// guard is not reproduced as a separate check, since by this point in the method the page is always
        /// known to exist, having either just been inserted or already been read back above), and every
        /// Pages.PageRevisionAttachment row still attached at the <i>previous</i> revision - only those whose
        /// <see cref="PagesEntities.PageRevisionAttachment.FileRevision"/> still matches the file's own current
        /// <see cref="PagesEntities.PageFile.Revision"/>, i.e. the file has not itself been separately replaced
        /// since - is carried forward to the new revision (ReassociateAllPageAttachments.sql, via the existing
        /// <see cref="PagesEntities.PageRevisionAttachment.PageFile"/> navigation rather than a manual join; a
        /// no-op for a brand-new page, which has no attachment rows yet at "revision 0").</item>
        /// </list>
        /// Unlike <paramref name="page"/>.Navigation - which callers are expected to have already resolved via
        /// <see cref="TwNamespaceNavigation.CleanAndValidate"/> before calling <see cref="UpsertPage"/>, same as
        /// the reference (<paramref name="page"/>.Navigation is read here but never written back onto
        /// <paramref name="page"/> itself) - the value actually written to
        /// <see cref="PagesEntities.Page.Navigation"/>/<see cref="PagesEntities.PageRevision.Namespace"/> is
        /// (re)computed from <paramref name="page"/>.Name via <see cref="TwNamespaceNavigation.CleanAndValidate"/>
        /// on every save, matching the reference's own <c>pageUpsertParam.Navigation</c>.
        /// </remarks>
        private async Task<int> SavePage(TwPage page)
        {
            var navigation = TwNamespaceNavigation.CleanAndValidate(page.Name);
            var newDataHash = SecurityUtility.Crc32(page.Body ?? string.Empty);
            var now = DateTime.UtcNow;

            using var context = _createContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                int currentPageRevision = 0;
                bool hasPageChanged;

                if (page.Id == 0)
                {
                    //This is a new page, just insert it.
                    var newPage = new PagesEntities.Page
                    {
                        Name = page.Name,
                        Namespace = page.Namespace,
                        Description = page.Description,
                        Navigation = navigation,
                        Revision = 1,
                        CreatedByUserId = page.CreatedByUserId,
                        CreatedDate = page.CreatedDate,
                        ModifiedByUserId = page.ModifiedByUserId,
                        ModifiedDate = now,
                    };

                    context.Pages_Pages.Add(newPage);
                    await context.SaveChangesAsync();

                    page.Id = newPage.Id;
                    hasPageChanged = true;
                }
                else
                {
                    //Get current page so we can determine if anything has changed.
                    var currentRevisionInfo = await GetLimitedPageInfoByIdAndRevision(page.Id)
                        ?? throw new Exception("The page could not be found.");

                    currentPageRevision = currentRevisionInfo.Revision;

                    //Update the existing page.
                    await context.Pages_Pages
                        .Where(p => p.Id == page.Id)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(p => p.Description, page.Description)
                            .SetProperty(p => p.Name, page.Name)
                            .SetProperty(p => p.Namespace, page.Namespace)
                            .SetProperty(p => p.Navigation, navigation)
                            .SetProperty(p => p.ModifiedByUserId, page.ModifiedByUserId)
                            .SetProperty(p => p.ModifiedDate, now));

                    //Determine if anything has actually changed.
                    hasPageChanged = currentRevisionInfo.Name != page.Name
                        || currentRevisionInfo.Namespace != page.Namespace
                        || currentRevisionInfo.Description != page.Description
                        || currentRevisionInfo.ChangeSummary != page.ChangeSummary
                        || currentRevisionInfo.DataHash != newDataHash;
                }

                if (hasPageChanged)
                {
                    var previousPageRevision = currentPageRevision;
                    currentPageRevision++;

                    //The page content has actually changed (according to the checksum), so we will bump the page revision.
                    await context.Pages_Pages
                        .Where(p => p.Id == page.Id)
                        .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Revision, currentPageRevision));

                    //Insert the new actual page revision entry (this is the data).
                    context.Pages_PageRevisions.Add(new PagesEntities.PageRevision
                    {
                        PageId = page.Id,
                        Name = page.Name,
                        Namespace = page.Namespace,
                        Description = page.Description,
                        Body = page.Body ?? string.Empty,
                        DataHash = newDataHash,
                        Revision = currentPageRevision,
                        ChangeSummary = page.ChangeSummary ?? string.Empty,
                        ModifiedByUserId = page.ModifiedByUserId,
                        ModifiedDate = now,
                    });

                    await context.SaveChangesAsync();

                    //Associate all page attachments that are still current with the latest revision.
                    var carriedAttachments = await context.Pages_PageRevisionAttachments
                        .Where(pra => pra.PageId == page.Id
                            && pra.PageRevision == previousPageRevision
                            && pra.PageFile.Revision == pra.FileRevision)
                        .Select(pra => new { pra.PageFileId, pra.FileRevision })
                        .ToListAsync();

                    if (carriedAttachments.Count > 0)
                    {
                        context.Pages_PageRevisionAttachments.AddRange(carriedAttachments.Select(a => new PagesEntities.PageRevisionAttachment
                        {
                            PageId = page.Id,
                            PageFileId = a.PageFileId,
                            FileRevision = a.FileRevision,
                            PageRevision = currentPageRevision,
                        }));

                        await context.SaveChangesAsync();
                    }
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return page.Id;
        }

        #region Page File.

        /// <summary>
        /// Mirrors DetachPageRevisionAttachment.sql: deletes the Pages.PageRevisionAttachment row (if any) for
        /// the file matching <paramref name="fileNavigation"/> on the page matching <paramref name="pageNavigation"/>,
        /// at <paramref name="pageRevision"/>. The reference script wraps this in a correlated <c>EXISTS</c>
        /// subquery that additionally re-joins Pages.PageRevision (confirming a snapshot row exists for
        /// <paramref name="pageRevision"/>) and Pages.PageFileRevision (confirming the currently-attached
        /// <see cref="PagesEntities.PageRevisionAttachment.FileRevision"/> has a matching revision row) - both are
        /// integrity guarantees that always hold for any row this application ever writes (a
        /// Pages.PageRevisionAttachment row is never inserted without both existing - see <see cref="SavePage"/>/
        /// <see cref="UpsertPageFile"/>), so - like the redundant existence guard <see cref="SavePage"/>'s own
        /// remarks already call out for InsertPageRevision.sql - they are not reproduced as separate checks here.
        /// </summary>
        public async Task DetachPageRevisionAttachment(string pageNavigation, string fileNavigation, int pageRevision)
        {
            using var context = _createContext();

            await context.Pages_PageRevisionAttachments
                .Where(pra => pra.Page.Navigation == pageNavigation
                    && pra.PageFile.Navigation == fileNavigation
                    && pra.PageRevision == pageRevision)
                .ExecuteDeleteAsync();
        }

        /// <summary>
        /// Mirrors GetOrphanedPageAttachments.sql: every Pages.PageFileRevision row with no matching
        /// Pages.PageRevisionAttachment (i.e. a file revision that either was never attached to any page revision,
        /// or has since been superseded/detached), joined back to its owning Pages.PageFile/Pages.Page via the
        /// existing navigations rather than a manual join. Paginated by the "Pagination Size" customization
        /// setting; <see cref="TwOrphanedPageAttachment.PaginationPageCount"/> via the reference's own
        /// ceiling-division formula against the total (unpaginated) orphan count. Ordering mirrors
        /// <c>RepositoryHelpers.TransposeOrderby</c> against the script's <c>--CONFIG::</c> mapping ("Page"/
        /// "File"/"Size"/"Revision"): no <paramref name="orderBy"/> falls back to the script's own un-transposed
        /// "ORDER BY P.[Name]" (always ascending, ignoring <paramref name="orderByDirection"/> - a literal quirk
        /// of the reference script, same as <see cref="GetMissingPagesPaged"/>); an unrecognized
        /// <paramref name="orderBy"/> throws, same pattern as <see cref="GetMissingPagesPaged"/>.
        /// </summary>
        public async Task<List<TwOrphanedPageAttachment>> GetOrphanedPageAttachmentsPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            using var context = _createContext();

            var query = context.Pages_PageFileRevisions
                .Where(pfr => !context.Pages_PageRevisionAttachments
                    .Any(pra => pra.PageFileId == pfr.PageFileId && pra.FileRevision == pfr.Revision));

            var totalCount = await query.CountAsync();
            var paginationPageCount = (totalCount + (paginationSize - 1)) / paginationSize;

            bool ascending = string.Equals(orderByDirection, "asc", StringComparison.InvariantCultureIgnoreCase);

            var ordered = string.IsNullOrEmpty(orderBy)
                ? query.OrderBy(pfr => pfr.PageFile.Page.Name)
                : orderBy.ToUpperInvariant() switch
                {
                    "PAGE" => ascending ? query.OrderBy(pfr => pfr.PageFile.Page.Name) : query.OrderByDescending(pfr => pfr.PageFile.Page.Name),
                    "FILE" => ascending ? query.OrderBy(pfr => pfr.PageFile.Name) : query.OrderByDescending(pfr => pfr.PageFile.Name),
                    "SIZE" => ascending ? query.OrderBy(pfr => pfr.Size) : query.OrderByDescending(pfr => pfr.Size),
                    "REVISION" => ascending ? query.OrderBy(pfr => pfr.Revision) : query.OrderByDescending(pfr => pfr.Revision),
                    _ => throw new InvalidOperationException(
                        $"No order by mapping was found in 'GetOrphanedPageAttachments.sql' for the field '{orderBy}'."),
                };

            return await ordered
                .Skip((pageNumber - 1) * paginationSize)
                .Take(paginationSize)
                .Select(pfr => new TwOrphanedPageAttachment
                {
                    PageFileId = pfr.PageFileId,
                    PageName = pfr.PageFile.Page.Name,
                    Namespace = pfr.PageFile.Page.Namespace,
                    PageNavigation = pfr.PageFile.Page.Navigation,
                    FileName = pfr.PageFile.Name,
                    FileNavigation = pfr.PageFile.Navigation,
                    Size = pfr.Size,
                    FileRevision = pfr.Revision,
                    PaginationPageCount = paginationPageCount,
                })
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors PurgeOrphanedPageAttachments.sql's two-statement transaction: bulk-deletes every orphaned
        /// Pages.PageFileRevision (same "no matching Pages.PageRevisionAttachment" predicate as
        /// <see cref="GetOrphanedPageAttachmentsPaged"/>), then bulk-deletes every Pages.PageFile row left with
        /// zero remaining revisions.
        /// </summary>
        public async Task PurgeOrphanedPageAttachments()
        {
            using var context = _createContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                await context.Pages_PageFileRevisions
                    .Where(pfr => !context.Pages_PageRevisionAttachments
                        .Any(pra => pra.PageFileId == pfr.PageFileId && pra.FileRevision == pfr.Revision))
                    .ExecuteDeleteAsync();

                await context.Pages_PageFiles
                    .Where(pf => !context.Pages_PageFileRevisions.Any(pfr => pfr.PageFileId == pf.Id))
                    .ExecuteDeleteAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Mirrors PurgeOrphanedPageAttachment.sql's two-statement transaction: deletes the single
        /// Pages.PageFileRevision matching (<paramref name="pageFileId"/>, <paramref name="revision"/>), then
        /// deletes the owning Pages.PageFile row too if that was its last remaining revision.
        /// </summary>
        public async Task PurgeOrphanedPageAttachment(int pageFileId, int revision)
        {
            using var context = _createContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                await context.Pages_PageFileRevisions
                    .Where(pfr => pfr.PageFileId == pageFileId && pfr.Revision == revision)
                    .ExecuteDeleteAsync();

                await context.Pages_PageFiles
                    .Where(pf => pf.Id == pageFileId && !context.Pages_PageFileRevisions.Any(pfr => pfr.PageFileId == pageFileId))
                    .ExecuteDeleteAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Mirrors GetPageFilesInfoByPageNavigationAndPageRevisionPaged.sql: every Pages.PageRevisionAttachment
        /// row for the page matching <paramref name="pageNavigation"/> at <paramref name="pageRevision"/>
        /// (falling back to the page's current <see cref="PagesEntities.Page.Revision"/> when null), restricted to
        /// attachments whose <see cref="PagesEntities.PageRevisionAttachment.FileRevision"/> equals the file's own
        /// current <see cref="PagesEntities.PageFile.Revision"/> ("--Latest file revision." in the reference),
        /// joined to Pages.PageFileRevision for ContentType/Size. Paginated by <paramref name="pageSize"/>
        /// (falling back to the "Pagination Size" customization setting when null), ordered by
        /// <see cref="PagesEntities.PageFile.Name"/> then <see cref="PagesEntities.PageRevisionAttachment.PageFileId"/>,
        /// matching the reference's own "ORDER BY PF.[Name], PF.Id".
        /// </summary>
        public async Task<List<TwPageFileAttachmentInfo>> GetPageFilesInfoByPageNavigationAndPageRevisionPaged(string pageNavigation, int pageNumber, int? pageSize = null, int? pageRevision = null)
        {
            pageSize ??= await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            using var context = _createContext();

            var query =
                from pra in context.Pages_PageRevisionAttachments
                join pfr in context.Pages_PageFileRevisions on new { pra.PageFileId, Revision = pra.FileRevision } equals new { pfr.PageFileId, pfr.Revision }
                where pra.Page.Navigation == pageNavigation
                    && pra.FileRevision == pra.PageFile.Revision
                    && pra.PageRevision == (pageRevision ?? pra.Page.Revision)
                select new { pra, pfr };

            var totalCount = await query.CountAsync();
            var paginationPageCount = (totalCount + (pageSize.Value - 1)) / pageSize.Value;

            return await query
                .OrderBy(x => x.pra.PageFile.Name).ThenBy(x => x.pra.PageFileId)
                .Skip((pageNumber - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .Select(x => new TwPageFileAttachmentInfo
                {
                    Id = x.pra.PageFileId,
                    PageId = x.pra.PageId,
                    Name = x.pra.PageFile.Name,
                    ContentType = x.pfr.ContentType,
                    Size = x.pfr.Size,
                    CreatedDate = x.pra.PageFile.CreatedDate,
                    FileRevision = x.pfr.Revision,
                    FileNavigation = x.pra.PageFile.Navigation,
                    PageNavigation = x.pra.Page.Navigation,
                    PaginationPageSize = pageSize.Value,
                    PaginationPageCount = paginationPageCount,
                })
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation.sql: the single
        /// Pages.PageRevisionAttachment row for the file matching <paramref name="fileNavigation"/> on the page
        /// matching <paramref name="pageNavigation"/>, at <paramref name="pageRevision"/> (falling back to the
        /// page's current <see cref="PagesEntities.Page.Revision"/> when null). The reference script computes
        /// this via a <c>GROUP BY ... MAX(FileRevision)</c> subquery, defending against more than one attachment
        /// existing for the same (page, file, page-revision) triple - but
        /// <see cref="Configurations.Pages.PageRevisionAttachmentConfiguration"/>'s own unique index on exactly
        /// that triple already makes that structurally impossible, so this picks the (necessarily unique) match
        /// directly (<c>OrderByDescending(FileRevision).FirstOrDefault()</c> kept only as the same defensive
        /// tie-break, never actually exercised), then joins to Pages.PageFileRevision for ContentType/Size as a
        /// second query. <see cref="TwPageFileAttachmentInfo.CreatedByUserId"/>/<c>CreatedByUserName</c>/
        /// <c>CreatedByNavigation</c> are left unset, matching the reference script's own column list (it
        /// selects only Id/PageId/Name/ContentType/Size/CreatedDate).
        /// </summary>
        public async Task<TwPageFileAttachmentInfo?> GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? pageRevision = null)
        {
            using var context = _createContext();

            var attachment = await context.Pages_PageRevisionAttachments
                .Where(pra => pra.Page.Navigation == pageNavigation
                    && pra.PageFile.Navigation == fileNavigation
                    && pra.PageRevision == (pageRevision ?? pra.Page.Revision))
                .OrderByDescending(pra => pra.FileRevision)
                .Select(pra => new
                {
                    pra.PageId,
                    pra.PageFileId,
                    pra.FileRevision,
                    Name = pra.PageFile.Name,
                    CreatedDate = pra.PageFile.CreatedDate,
                    FileNavigation = pra.PageFile.Navigation,
                    PageNavigation = pra.Page.Navigation,
                })
                .FirstOrDefaultAsync();

            if (attachment == null)
            {
                return null;
            }

            var revision = await context.Pages_PageFileRevisions
                .Where(pfr => pfr.PageFileId == attachment.PageFileId && pfr.Revision == attachment.FileRevision)
                .Select(pfr => new { pfr.ContentType, pfr.Size, pfr.Revision })
                .FirstOrDefaultAsync();

            if (revision == null)
            {
                return null;
            }

            return new TwPageFileAttachmentInfo
            {
                Id = attachment.PageFileId,
                PageId = attachment.PageId,
                Name = attachment.Name,
                ContentType = revision.ContentType,
                Size = revision.Size,
                CreatedDate = attachment.CreatedDate,
                FileRevision = revision.Revision,
                FileNavigation = attachment.FileNavigation,
                PageNavigation = attachment.PageNavigation,
            };
        }

        /// <summary>
        /// Mirrors GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation.sql: the Pages.PageFile
        /// matching <paramref name="fileNavigation"/> on the page matching <paramref name="pageNavigation"/>,
        /// joined to the Pages.PageFileRevision matching <paramref name="fileRevision"/> (falling back to the
        /// file's own current <see cref="PagesEntities.PageFile.Revision"/> when null), including
        /// <see cref="PagesEntities.PageFileRevision.Data"/>. No caching, matching the SQLite reference. The
        /// reference script's own column list excludes FileNavigation/PageNavigation (unlike the sibling
        /// <see cref="GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation"/>), so those are left
        /// unset here too, same as <see cref="GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation"/>'s
        /// CreatedBy* fields.
        /// </summary>
        public async Task<TwPageFileAttachment?> GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? fileRevision = null)
        {
            using var context = _createContext();

            return await (
                from pf in context.Pages_PageFiles
                join pfr in context.Pages_PageFileRevisions on pf.Id equals pfr.PageFileId
                where pf.Page.Navigation == pageNavigation
                    && pf.Navigation == fileNavigation
                    && pfr.Revision == (fileRevision ?? pf.Revision)
                select new TwPageFileAttachment
                {
                    Id = pf.Id,
                    PageId = pf.PageId,
                    Name = pf.Name,
                    ContentType = pfr.ContentType,
                    Size = pfr.Size,
                    CreatedDate = pf.CreatedDate,
                    Data = pfr.Data,
                }
            ).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Mirrors GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation.sql: the same "current
        /// attachment at a given page revision" lookup as
        /// <see cref="GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation"/> (see its remarks
        /// for why the reference's <c>GROUP BY ... MAX(FileRevision)</c> is picked directly instead), but
        /// returning the full <see cref="PagesEntities.PageFileRevision.Data"/> rather than just metadata, and
        /// cached under <see cref="MemCache.Category.Page"/> (same cache key shape - page navigation + file
        /// navigation + page revision - as the SQLite reference). The reference script's own column list excludes
        /// FileNavigation/PageNavigation, so those are left unset here too.
        /// </summary>
        public async Task<TwPageFileAttachment?> GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? pageRevision = null)
        {
            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.Page, [pageNavigation, fileNavigation, pageRevision]);

            return await MemCache.AddOrGetAsync(cacheKey, async () =>
            {
                using var context = _createContext();

                var attachment = await context.Pages_PageRevisionAttachments
                    .Where(pra => pra.Page.Navigation == pageNavigation
                        && pra.PageFile.Navigation == fileNavigation
                        && pra.PageRevision == (pageRevision ?? pra.Page.Revision))
                    .OrderByDescending(pra => pra.FileRevision)
                    .Select(pra => new { pra.PageId, pra.PageFileId, pra.FileRevision, Name = pra.PageFile.Name, CreatedDate = pra.PageFile.CreatedDate })
                    .FirstOrDefaultAsync();

                if (attachment == null)
                {
                    return null;
                }

                var revision = await context.Pages_PageFileRevisions
                    .Where(pfr => pfr.PageFileId == attachment.PageFileId && pfr.Revision == attachment.FileRevision)
                    .FirstOrDefaultAsync();

                if (revision == null)
                {
                    return null;
                }

                return new TwPageFileAttachment
                {
                    Id = attachment.PageFileId,
                    PageId = attachment.PageId,
                    Name = attachment.Name,
                    ContentType = revision.ContentType,
                    Size = revision.Size,
                    CreatedDate = attachment.CreatedDate,
                    Data = revision.Data,
                };
            });
        }

        /// <summary>
        /// Mirrors GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged.sql: every Pages.PageFileRevision
        /// for the file matching <paramref name="fileNavigation"/> on the page matching
        /// <paramref name="pageNavigation"/>, LEFT OUTER JOINed to Users.Profile for the uploader - via the
        /// existing <see cref="PagesEntities.PageFileRevision.CreatedByUser"/> navigation rather than a raw
        /// cross-database <c>ATTACH</c>, same pattern as <see cref="GetPageCommentsPaged"/>. Paginated by the
        /// "Pagination Size" customization setting. The reference script has no <c>ORDER BY</c> at all (relying on
        /// SQLite's incidental physical/rowid order, which happens to be ascending-by-Revision for a fixed
        /// PageFileId given how rows are inserted); an explicit <c>OrderBy(Revision)</c> is added here since
        /// Skip/Take pagination is not guaranteed deterministic without one on every EF Core provider (in
        /// particular SQL Server) - a deliberate, minor deviation from the literal reference for cross-provider
        /// correctness, not a behavioral difference for any data this application ever produces.
        /// </summary>
        public async Task<List<TwPageFileAttachmentInfo>> GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged(string pageNavigation, string fileNavigation, int pageNumber)
        {
            var paginationSize = await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            using var context = _createContext();

            var query = context.Pages_PageFileRevisions
                .Where(pfr => pfr.PageFile.Page.Navigation == pageNavigation && pfr.PageFile.Navigation == fileNavigation);

            var totalCount = await query.CountAsync();
            var paginationPageCount = (totalCount + (paginationSize - 1)) / paginationSize;

            return await query
                .OrderBy(pfr => pfr.Revision)
                .Skip((pageNumber - 1) * paginationSize)
                .Take(paginationSize)
                .Select(pfr => new TwPageFileAttachmentInfo
                {
                    Id = pfr.PageFile.Id,
                    PageId = pfr.PageFile.PageId,
                    Name = pfr.PageFile.Name,
                    ContentType = pfr.ContentType,
                    Size = pfr.Size,
                    CreatedDate = pfr.CreatedDate,
                    FileRevision = pfr.Revision,
                    CreatedByUserId = pfr.CreatedByUserId,
                    CreatedByUserName = pfr.CreatedByUser != null ? (pfr.CreatedByUser.AccountName ?? string.Empty) : string.Empty,
                    CreatedByNavigation = pfr.CreatedByUser != null ? (pfr.CreatedByUser.Navigation ?? string.Empty) : string.Empty,
                    PaginationPageSize = paginationSize,
                    PaginationPageCount = paginationPageCount,
                })
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors GetPageFilesInfoByPageId.sql: the same "latest file revision, attached to the page's current
        /// revision" shape as <see cref="GetPageFilesInfoByPageNavigationAndPageRevisionPaged"/>, but for every
        /// attachment on <paramref name="pageId"/>, unpaginated and unordered (matching the reference, which has
        /// no <c>ORDER BY</c>/<c>LIMIT</c>).
        /// </summary>
        public async Task<List<TwPageFileAttachmentInfo>> GetPageFilesInfoByPageId(int pageId)
        {
            using var context = _createContext();

            return await (
                from pra in context.Pages_PageRevisionAttachments
                join pfr in context.Pages_PageFileRevisions on new { pra.PageFileId, Revision = pra.FileRevision } equals new { pfr.PageFileId, pfr.Revision }
                where pra.PageId == pageId
                    && pra.FileRevision == pra.PageFile.Revision
                    && pra.PageRevision == pra.Page.Revision
                select new TwPageFileAttachmentInfo
                {
                    Id = pra.PageFileId,
                    PageId = pra.PageId,
                    Name = pra.PageFile.Name,
                    ContentType = pfr.ContentType,
                    Size = pfr.Size,
                    CreatedDate = pra.PageFile.CreatedDate,
                    FileRevision = pfr.Revision,
                    FileNavigation = pra.PageFile.Navigation,
                    PageNavigation = pra.Page.Navigation,
                }
            ).ToListAsync();
        }

        /// <summary>
        /// Shared helper behind <see cref="UpsertPageFile"/> - mirrors <c>PageRepository.GetPageFileInfoByFileNavigation</c>
        /// (a SQLite-only helper taking a raw connection, not part of <see cref="ITwPageRepository"/> as of the
        /// commit that trimmed the interface down to just what's actually needed cross-provider - see
        /// Database-Providers-Plan.md chapter 4.1): the Pages.PageFile row for <paramref name="fileNavigation"/>
        /// on <paramref name="pageId"/>, or null if the file has never been uploaded to this page before.
        /// </summary>
        private static async Task<TwPageFileRevisionAttachmentInfo?> GetPageFileInfoByFileNavigation(TightWikiDbContext context, int pageId, string fileNavigation)
        {
            return await context.Pages_PageFiles
                .Where(pf => pf.PageId == pageId && pf.Navigation == fileNavigation)
                .Select(pf => new TwPageFileRevisionAttachmentInfo
                {
                    PageFileId = pf.Id,
                    PageId = pf.PageId,
                    Revision = pf.Revision,
                })
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Shared helper behind <see cref="UpsertPageFile"/> - mirrors
        /// <c>PageRepository.GetPageCurrentRevisionAttachmentByFileNavigation</c> (a SQLite-only helper taking a
        /// raw connection, not part of <see cref="ITwPageRepository"/> - see
        /// <see cref="GetPageFileInfoByFileNavigation"/>'s remarks): the Pages.PageFileRevision currently attached
        /// to <paramref name="pageId"/>'s own current <see cref="PagesEntities.Page.Revision"/>, for the file
        /// matching <paramref name="fileNavigation"/>, or null if that file is not currently attached to the
        /// page's latest revision (either never attached, or detached since). Picked directly via
        /// <c>OrderByDescending(FileRevision).FirstOrDefault()</c> rather than the reference's defensive
        /// <c>MAX(...)</c>/<c>GROUP BY</c>, for the same reason given in
        /// <see cref="GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation"/>'s remarks.
        /// </summary>
        private static async Task<TwPageFileRevisionAttachmentInfo?> GetPageCurrentRevisionAttachmentByFileNavigation(TightWikiDbContext context, int pageId, string fileNavigation)
        {
            var attachment = await context.Pages_PageRevisionAttachments
                .Where(pra => pra.PageFile.PageId == pageId
                    && pra.PageFile.Navigation == fileNavigation
                    && pra.PageRevision == pra.Page.Revision)
                .OrderByDescending(pra => pra.FileRevision)
                .Select(pra => new { pra.PageFileId, pra.FileRevision })
                .FirstOrDefaultAsync();

            if (attachment == null)
            {
                return null;
            }

            return await context.Pages_PageFileRevisions
                .Where(pfr => pfr.PageFileId == attachment.PageFileId && pfr.Revision == attachment.FileRevision)
                .Select(pfr => new TwPageFileRevisionAttachmentInfo
                {
                    PageId = pageId,
                    PageFileId = pfr.PageFileId,
                    Revision = pfr.Revision,
                    ContentType = pfr.ContentType,
                    Size = (int)pfr.Size,
                    DataHash = pfr.DataHash,
                })
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Mirrors <c>PageRepository.UpsertPageFile</c>'s hash-based change detection and revision-bumping, one
        /// transaction per call - the same overall shape as <see cref="SavePage"/> (see its remarks) but for file
        /// attachments rather than page bodies:
        /// <list type="number">
        /// <item>If no Pages.PageFile row exists yet for <paramref name="item"/>.FileNavigation on
        /// <paramref name="item"/>.PageId (via <see cref="GetPageFileInfoByFileNavigation"/>), one is inserted
        /// (Revision hardcoded to 0, matching InsertPageFile.sql) - EF Core populates the new row's identity
        /// <c>Id</c> directly from <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync()"/>, so
        /// unlike the SQLite reference (which has to re-run <c>GetPageFileInfoByFileNavigation</c> a second time
        /// after the insert to recover the newly-generated id) no second round-trip is needed here.</item>
        /// <item>The file revision currently attached to the page's own current revision is read via
        /// <see cref="GetPageCurrentRevisionAttachmentByFileNavigation"/>. If one exists, "changed" is a CRC32
        /// mismatch (<see cref="TightWiki.Library.Security.SecurityUtility.Crc32(byte[])"/>, same algorithm as the
        /// reference) between it and <paramref name="item"/>.Data; if none exists (brand-new file, or a file that
        /// exists but is not attached to this page's current revision), it is unconditionally "changed". The
        /// reference script also sets an initial <c>hasFileChanged = true</c> right after inserting a brand-new
        /// Pages.PageFile row, but that assignment is immediately overwritten by this same second check before
        /// it's ever read (a brand-new file is, by construction, also never currently attached) - so it is
        /// provably dead code, not reproduced here.</item>
        /// <item>Only when "changed": the file's <see cref="PagesEntities.PageFile.Revision"/> counter is bumped
        /// (UpdatePageFileRevision.sql), a new Pages.PageFileRevision row is inserted at that revision
        /// (InsertPageFileRevision.sql) under <paramref name="userId"/>, the page's own current
        /// <see cref="PagesEntities.Page.Revision"/> is read via the same <paramref name="item"/>'s
        /// transaction/context. The reference's equivalent call, <c>GetCurrentPageRevision(o, pageId)</c>, is
        /// actually cached (it's wrapped in <c>MemCache.AddOrGetAsync</c> under the same cache key as the
        /// parameterless public overload, since <c>MemCacheKeyFunction.Build</c> keys on <c>[CallerMemberName]</c>
        /// - the connection parameter plays no part in the key). This EF implementation deliberately bypasses that
        /// cache and always issues a fresh query instead, so it can never observe a value staler than the
        /// reference's; that's safe either way because <see cref="RefreshPageMetadata"/> clears this exact cache
        /// entry (<c>MemCache.Category.Page, [pageId]</c>) on every <see cref="UpsertPage"/>, so no stale-cache
        /// scenario arises for either implementation in practice. The previous attachment for that page revision
        /// (if any) is removed, and the new file revision is associated with the page's current revision
        /// (AssociatePageFileAttachmentWithPageRevision.sql's delete-then-insert, both folded into this one
        /// transaction).</item>
        /// </list>
        /// Does not call <see cref="FlushPageCache"/> or invalidate the
        /// <see cref="GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation"/> cache entry, matching
        /// the SQLite reference (neither does either).
        /// </summary>
        public async Task UpsertPageFile(TwPageFileAttachment item, Guid userId)
        {
            using var context = _createContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var pageFileInfo = await GetPageFileInfoByFileNavigation(context, item.PageId, item.FileNavigation);
                if (pageFileInfo == null)
                {
                    //If the page file does not exist, then insert it.
                    var newPageFile = new PagesEntities.PageFile
                    {
                        PageId = item.PageId,
                        Name = item.Name,
                        Navigation = item.FileNavigation,
                        CreatedDate = item.CreatedDate,
                        Revision = 0,
                    };

                    context.Pages_PageFiles.Add(newPageFile);
                    await context.SaveChangesAsync();

                    //EF Core already gave us the new identity value - no need for a second round-trip to fetch it.
                    pageFileInfo = new TwPageFileRevisionAttachmentInfo
                    {
                        PageFileId = newPageFile.Id,
                        PageId = newPageFile.PageId,
                        Revision = newPageFile.Revision,
                    };
                }

                var newDataHash = SecurityUtility.Crc32(item.Data);

                int currentFileRevision;
                bool hasFileChanged;

                var currentlyAttachedFile = await GetPageCurrentRevisionAttachmentByFileNavigation(context, item.PageId, item.FileNavigation);
                if (currentlyAttachedFile != null)
                {
                    //The PageFile exists and a revision of it is attached to this page revision.
                    //Keep track of the file revision, and determine if the file has changed (via the file hash).
                    currentFileRevision = currentlyAttachedFile.Revision;
                    hasFileChanged = currentlyAttachedFile.DataHash != newDataHash;
                }
                else
                {
                    //The file either does not exist or is not attached to the current page revision.
                    hasFileChanged = true;

                    //We determined earlier that the PageFile does exist, so keep track of the file revision.
                    currentFileRevision = pageFileInfo.Revision;
                }

                if (hasFileChanged)
                {
                    currentFileRevision++;

                    //Get the current page revision so that we can associate the page file attachment with the current page revision.
                    var currentPageRevision = await context.Pages_Pages
                        .Where(p => p.Id == item.PageId)
                        .Select(p => p.Revision)
                        .FirstOrDefaultAsync();

                    //The file has changed (or is newly inserted), bump the file revision.
                    await context.Pages_PageFiles
                        .Where(pf => pf.Id == pageFileInfo.PageFileId)
                        .ExecuteUpdateAsync(setters => setters.SetProperty(pf => pf.Revision, currentFileRevision));

                    //Insert the actual file data.
                    context.Pages_PageFileRevisions.Add(new PagesEntities.PageFileRevision
                    {
                        PageFileId = pageFileInfo.PageFileId,
                        ContentType = item.ContentType,
                        Size = item.Size,
                        CreatedDate = item.CreatedDate,
                        CreatedByUserId = userId,
                        Data = item.Data,
                        Revision = currentFileRevision,
                        DataHash = newDataHash,
                    });

                    await context.SaveChangesAsync();

                    //Remove the previous page file revision attachment, if any.
                    if (currentlyAttachedFile != null)
                    {
                        await context.Pages_PageRevisionAttachments
                            .Where(pra => pra.PageId == item.PageId
                                && pra.PageFileId == pageFileInfo.PageFileId
                                && pra.FileRevision == currentlyAttachedFile.Revision
                                && pra.PageRevision == currentPageRevision)
                            .ExecuteDeleteAsync();
                    }

                    //Associate the latest version of the file with the latest version of the page.
                    context.Pages_PageRevisionAttachments.Add(new PagesEntities.PageRevisionAttachment
                    {
                        PageId = item.PageId,
                        PageFileId = pageFileInfo.PageFileId,
                        FileRevision = currentFileRevision,
                        PageRevision = currentPageRevision,
                    });

                    await context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region Current page editors.

        /// <summary>
        /// Mirrors UpsertCurrentPageEditor.sql's two statements: an "INSERT ... ON CONFLICT(PageId, UserId) DO
        /// UPDATE" upsert of the Pages.CurrentPageEditors row for (<paramref name="pageId"/>,
        /// <paramref name="userId"/>) - refreshing <see cref="PagesEntities.CurrentPageEditor.AccountName"/>/
        /// <see cref="PagesEntities.CurrentPageEditor.UtcDate"/> to now if the row already exists - followed by an
        /// unconditional delete of every row older than one day. EF Core's LINQ surface has no portable equivalent
        /// of that single atomic "ON CONFLICT" statement, so the upsert is implemented as "try an
        /// <see cref="Microsoft.EntityFrameworkCore.RelationalQueryableExtensions.ExecuteUpdateAsync{TSource}"/>
        /// first, insert only if it affected zero rows" - the same non-atomic pattern (and narrow race-condition
        /// caveat) as <see cref="EfStatisticsRepository.IncrementPageViewCount"/>/
        /// <see cref="EfStatisticsRepository.MergePageCompilationStatistics"/>. Does not call
        /// <see cref="FlushPageCache"/> - matching the SQLite reference, which touches no cache at all here.
        /// </summary>
        public async Task UpsertCurrentPageEditor(int pageId, Guid userId, string accountName)
        {
            using var context = _createContext();

            var utcNow = DateTime.UtcNow;

            var updated = await context.CurrentPageEditors
                .Where(e => e.PageId == pageId && e.UserId == userId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(e => e.AccountName, accountName)
                    .SetProperty(e => e.UtcDate, utcNow));

            if (updated == 0)
            {
                context.CurrentPageEditors.Add(new PagesEntities.CurrentPageEditor
                {
                    PageId = pageId,
                    UserId = userId,
                    AccountName = accountName,
                    UtcDate = utcNow,
                });

                await context.SaveChangesAsync();
            }

            var deleteThresholdDate = utcNow.AddDays(-1);

            await context.CurrentPageEditors
                .Where(e => e.UtcDate < deleteThresholdDate)
                .ExecuteDeleteAsync();
        }

        /// <summary>
        /// Mirrors DeleteCurrentPageEditor.sql via EF Core's LINQ bulk <c>ExecuteDeleteAsync</c> - fully
        /// provider-portable, no raw SQL needed.
        /// </summary>
        public async Task DeleteCurrentPageEditor(int pageId, Guid userId)
        {
            using var context = _createContext();

            await context.CurrentPageEditors
                .Where(e => e.PageId == pageId && e.UserId == userId)
                .ExecuteDeleteAsync();
        }

        /// <summary>
        /// Mirrors GetCurrentPageEditors.sql: the account names of every Pages.CurrentPageEditors row for
        /// <paramref name="pageId"/> whose <see cref="PagesEntities.CurrentPageEditor.UtcDate"/> is within the
        /// last <paramref name="windowMinutes"/> minutes.
        /// </summary>
        public async Task<List<string>> GetCurrentPageEditors(int pageId, int windowMinutes = 5)
        {
            using var context = _createContext();

            var thresholdDate = DateTime.UtcNow.AddMinutes(-windowMinutes);

            return await context.CurrentPageEditors
                .Where(e => e.PageId == pageId && e.UtcDate >= thresholdDate)
                .Select(e => e.AccountName)
                .ToListAsync();
        }

        #endregion
    }
}
