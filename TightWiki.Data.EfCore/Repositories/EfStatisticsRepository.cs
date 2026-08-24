using Microsoft.EntityFrameworkCore;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;
using StatisticsEntities = TightWiki.Data.EfCore.Entities.Statistics;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Data.EfCore.Repositories
{
    /// <summary>
    /// Provider-agnostic (SQL Server/Postgres, per Database-Providers-Plan.md chapter 3) LINQ-over-EF-Core
    /// implementation of <see cref="ITwStatisticsRepository"/>. Lives in the shared <c>TightWiki.Data.EfCore</c>
    /// project rather than a per-provider driver project, for the same reason as <see cref="EfConfigurationRepository"/>/
    /// <see cref="EfLoggingRepository"/>/<see cref="EfEmojiRepository"/> (see those classes' doc comments): plain
    /// LINQ against <see cref="TightWikiDbContext"/> needs no provider-specific code here at all. Originally landed
    /// as a <c>SqlServerStatisticsRepository</c> stub under <c>TightWiki.Data.EfCore.SqlServer/Repositories/</c> in
    /// phase 2a.1; moved and implemented for real here in phase 2a.9.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reference semantics throughout are the SQLite implementation, <c>TightWiki.Repository.StatisticsRepository</c>,
    /// and its backing <c>Scripts\IncrementPageViewCount.sql</c>/<c>MergePageCompilationStatistics.sql</c>/
    /// <c>GetPageTotalViewCount.sql</c>/<c>PurgePageStatistics.sql</c>/<c>GetPageStatisticsPaged.sql</c>/
    /// <c>DeletePageStatisticsByPageId.sql</c> - see each method's doc comment below for the specific script it
    /// mirrors.
    /// </para>
    /// <para>
    /// <b>⚠ Confirmed bug in the SQLite reference, not reproduced here</b> - see <see cref="DeletePageStatisticsByPageId"/>'s
    /// doc comment: that method's interface contract promises "the number of records deleted", but the SQLite
    /// reference always returns 0 regardless of how many rows were actually deleted (confirmed empirically against
    /// a live SQLite database, not just by reading the SQL). This implementation returns the real, accurate deleted
    /// row count instead.
    /// </para>
    /// <para>
    /// <see cref="IncrementPageViewCount"/> and <see cref="MergePageCompilationStatistics"/> both mirror a SQLite
    /// "<c>INSERT ... ON CONFLICT(PageId) DO UPDATE ...</c>" upsert. EF Core's LINQ surface has no portable
    /// (SQL Server/Postgres) equivalent of that single atomic statement, so both are implemented here as "try an
    /// <see cref="Microsoft.EntityFrameworkCore.RelationalQueryableExtensions.ExecuteUpdateAsync{TSource}"/> first,
    /// insert only if it affected zero rows" - functionally equivalent for the single-writer paths that call these
    /// (page view/compile), but - unlike the SQLite reference's single atomic statement - leaves a narrow window
    /// where two concurrent first-ever calls for the same brand new <see cref="StatisticsEntities.PageStatistic.PageId"/>
    /// could each observe zero rows updated and both attempt to insert, violating the unique index on
    /// <see cref="StatisticsEntities.PageStatistic.PageId"/> (<c>IX_CompilationStatistics_PageId</c>) for one of
    /// them. Accepted as out of scope to fully close here, same as <see cref="EfEmojiRepository.UpsertEmoji"/>'s own
    /// find-then-write-back pattern.
    /// </para>
    /// <para>
    /// Takes a <see cref="Func{TightWikiDbContext}"/> rather than an injected context instance, mirroring
    /// <see cref="EfConfigurationRepository"/>/<see cref="EfLoggingRepository"/>/<see cref="EfEmojiRepository"/> -
    /// see those classes' doc comments for why. Also takes an <see cref="ITwConfigurationRepository"/> instance
    /// directly (not another <see cref="Func{TResult}"/>), mirroring the SQLite reference constructor's own
    /// <c>ConfigurationRepository configurationRepository</c> parameter - <see cref="GetPageStatisticsPaged"/> is
    /// the only member that needs it, to read the "Pagination Size" customization setting.
    /// </para>
    /// </remarks>
    public sealed class EfStatisticsRepository : ITwStatisticsRepository
    {
        private readonly Func<TightWikiDbContext> _createContext;
        private readonly ITwConfigurationRepository _configurationRepository;

        public EfStatisticsRepository(Func<TightWikiDbContext> createContext, ITwConfigurationRepository configurationRepository)
        {
            _createContext = createContext;
            _configurationRepository = configurationRepository;
        }

        /// <summary>
        /// Mirrors IncrementPageViewCount.sql's upsert: increments <see cref="StatisticsEntities.PageStatistic.TotalViewCount"/>
        /// by one for the row matching <paramref name="pageId"/>, or - if no such row exists yet - inserts a new
        /// one with <c>TotalCompilationCount = 1</c>, <c>TotalViewCount = 1</c> and
        /// <see cref="StatisticsEntities.PageStatistic.LastCompileDateTime"/> set to now (every other column left
        /// at its default/null, same as the script's INSERT column list not mentioning them). Unlike the script,
        /// an existing row's <c>LastCompileDateTime</c> is deliberately left untouched here too - matching the
        /// script's own <c>ON CONFLICT ... DO UPDATE SET TotalViewCount = ...</c> clause, which likewise does not
        /// touch <c>LastCompileDateTime</c> on conflict. See this class's doc comment for the non-atomic
        /// upsert caveat.
        /// </summary>
        public async Task IncrementPageViewCount(int pageId)
        {
            using var context = _createContext();

            var updated = await context.PageStatistics
                .Where(ps => ps.PageId == pageId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(ps => ps.TotalViewCount, ps => ps.TotalViewCount + 1));

            if (updated == 0)
            {
                context.PageStatistics.Add(new StatisticsEntities.PageStatistic
                {
                    PageId = pageId,
                    LastCompileDateTime = DateTime.UtcNow, //Because the column is not nullable.
                    TotalCompilationCount = 1,
                    TotalViewCount = 1,
                });

                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Mirrors MergePageCompilationStatistics.sql's upsert: for the row matching <paramref name="pageId"/>,
        /// bumps <see cref="StatisticsEntities.PageStatistic.TotalCompilationCount"/> by one, adds
        /// <paramref name="wikifyTimeMs"/> onto <see cref="StatisticsEntities.PageStatistic.TotalWikifyTimeMs"/>,
        /// refreshes <see cref="StatisticsEntities.PageStatistic.LastCompileDateTime"/> to now and every "Last*"
        /// column to the given values - or, if no row exists yet, inserts one with
        /// <c>TotalCompilationCount = 1</c>, <c>TotalViewCount = 1</c>, <c>TotalWikifyTimeMs = wikifyTimeMs</c> and
        /// the same "Last*" values. <see cref="StatisticsEntities.PageStatistic.TotalViewCount"/> is left untouched
        /// on an existing row, same as the script's <c>ON CONFLICT</c> clause not mentioning it. See this class's
        /// doc comment for the non-atomic upsert caveat.
        /// </summary>
        /// <remarks>
        /// <b>Deliberate divergence from the SQLite reference: <paramref name="pageId"/> &lt;= 0 is a no-op here.</b>
        /// <see cref="Plugin.Models.TwPage.Id"/>'s own doc comment states "a value of 0 indicates the page has not
        /// been saved" - i.e. every real, persisted page has an <see cref="TightWiki.Data.EfCore.Entities.Pages.Page.Id"/>
        /// assigned by the identity/auto-increment primary key (starts at 1, see <c>PageConfiguration.Configure</c>'s
        /// <c>HasKey(e =&gt; e.Id)</c>), so <c>pageId &lt;= 0</c> can only ever be the synthetic, un-persisted "adhoc"
        /// <see cref="Plugin.Models.TwPage"/> that <c>WikiEngine.Transform(localizer, session, string markup)</c>/
        /// <c>TransformLite</c> construct on the fly (e.g. <c>_Layout.cshtml</c>'s footer blurb, profile biography,
        /// page comment previews) - never a real page. The SQLite reference script's <c>INSERT ... ON CONFLICT</c>
        /// has no such guard and silently inserts/updates an orphaned <c>PageStatistics</c> row for
        /// <c>PageId = 0</c> that no <c>Page</c> row will ever match; SQLite does not enforce
        /// <c>PageStatistics.PageId</c> as a real FOREIGN KEY reference to <c>Page.Id</c> (unlike this EF Core
        /// model - see <see cref="StatisticsEntities.PageStatistic.Page"/>/<c>PageStatisticConfiguration</c>'s real,
        /// required FK), so that orphan row is silently accepted there and never surfaces as an observable bug -
        /// it just wastes a row that has no value (nothing ever reads statistics for a page that doesn't exist).
        /// Under a provider that enforces the FK (SQL Server/Postgres), the same insert attempt instead violates
        /// the constraint and fails the entire request that triggered it (originally surfaced via every page
        /// rendering <c>_Layout.cshtml</c>'s footer, since <see cref="TightWiki.Plugin.TwConfiguration.FooterBlurb"/>
        /// is transformed through the pageless overload on every request). Left unfixed in the SQLite reference
        /// per this project's convention of not modifying <c>Scripts\*.sql</c> - see repo-root <c>CLAUDE.md</c> -
        /// since it has no observable behavioral impact there.
        /// </remarks>
        public async Task MergePageCompilationStatistics(int pageId,
            double wikifyTimeMs, int matchCount, int errorCount, int outgoingLinkCount,
            int tagCount, int processedBodySize, int bodySize)
        {
            if (pageId <= 0)
            {
                //See the <remarks> above: pageId <= 0 only ever means the synthetic, un-persisted "adhoc" TwPage
                //used for pageless markup transforms (footer blurb, biography, comment previews, etc.) - there is
                //no real Page row to attribute statistics to, and attempting the insert would violate the real
                //FK from PageStatistics.PageId to Page.Id that this EF Core model enforces (unlike SQLite).
                return;
            }

            using var context = _createContext();

            var lastCompileDateTime = DateTime.UtcNow;

            var updated = await context.PageStatistics
                .Where(ps => ps.PageId == pageId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(ps => ps.LastCompileDateTime, lastCompileDateTime)
                    .SetProperty(ps => ps.TotalCompilationCount, ps => ps.TotalCompilationCount + 1)
                    .SetProperty(ps => ps.LastWikifyTimeMs, wikifyTimeMs)
                    .SetProperty(ps => ps.TotalWikifyTimeMs, ps => (ps.TotalWikifyTimeMs ?? 0) + wikifyTimeMs)
                    .SetProperty(ps => ps.LastMatchCount, matchCount)
                    .SetProperty(ps => ps.LastErrorCount, errorCount)
                    .SetProperty(ps => ps.LastOutgoingLinkCount, outgoingLinkCount)
                    .SetProperty(ps => ps.LastTagCount, tagCount)
                    .SetProperty(ps => ps.LastProcessedBodySize, processedBodySize)
                    .SetProperty(ps => ps.LastBodySize, bodySize));

            if (updated == 0)
            {
                context.PageStatistics.Add(new StatisticsEntities.PageStatistic
                {
                    PageId = pageId,
                    LastCompileDateTime = lastCompileDateTime,
                    TotalCompilationCount = 1,
                    TotalViewCount = 1,
                    LastWikifyTimeMs = wikifyTimeMs,
                    TotalWikifyTimeMs = wikifyTimeMs,
                    LastMatchCount = matchCount,
                    LastErrorCount = errorCount,
                    LastOutgoingLinkCount = outgoingLinkCount,
                    LastTagCount = tagCount,
                    LastProcessedBodySize = processedBodySize,
                    LastBodySize = bodySize,
                });

                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Mirrors GetPageTotalViewCount.sql: the <see cref="StatisticsEntities.PageStatistic.TotalViewCount"/> of
        /// the row matching <paramref name="pageId"/>, or 0 if no such row exists (the script's
        /// <c>ExecuteScalarAsync&lt;int&gt;</c> over a query with no matching row - a genuinely sensible "no
        /// statistics recorded yet" fallback, unlike the bug documented on <see cref="DeletePageStatisticsByPageId"/>).
        /// </summary>
        public async Task<int> GetPageTotalViewCount(int pageId)
        {
            using var context = _createContext();

            return await context.PageStatistics
                .Where(ps => ps.PageId == pageId)
                .Select(ps => (int?)ps.TotalViewCount)
                .FirstOrDefaultAsync() ?? 0;
        }

        /// <summary>
        /// Mirrors PurgePageStatistics.sql ("DELETE FROM PageStatistics;") via EF Core's LINQ bulk
        /// <c>ExecuteDeleteAsync</c> - fully provider-portable, no raw SQL needed.
        /// </summary>
        public async Task PurgePageStatistics()
        {
            using var context = _createContext();
            await context.PageStatistics.ExecuteDeleteAsync();
        }

        /// <summary>
        /// Mirrors GetPageStatisticsPaged.sql: a page of every PageStatistics row inner-joined to its owning Page
        /// (real, required, one-to-one relationship - see <see cref="StatisticsEntities.PageStatistic.Page"/>'s doc
        /// comment - so no separate <c>GROUP BY</c>/<c>MAX(...)</c> trick is needed the way the script uses one to
        /// combine a join with non-aggregated columns), with <see cref="TwPageStatistics.PaginationPageCount"/>
        /// computed via the script's own ceiling-division formula (<c>(Count(DISTINCT P.Id) + (@PageSize - 1)) /
        /// @PageSize</c>) against the total row count. <see cref="TwPageStatistics.Namespace"/> is populated from
        /// <see cref="StatisticsEntities.PageStatistic.Page"/>'s own <c>Namespace</c> column, matching the script's
        /// <c>MAX(P.Namespace) as Namespace</c>. Nullable "Last*"/"Total*" columns are coalesced to 0 - in practice
        /// never null for a row this query can return, since
        /// <see cref="IncrementPageViewCount"/>/<see cref="MergePageCompilationStatistics"/> are the only writers
        /// and both always populate them. Ordering mirrors <c>RepositoryHelpers.TransposeOrderby</c> against the
        /// script's <c>--CONFIG::</c> mapping ("Name=MAX(P.Name)", "Navigation=MAX(P.Navigation)",
        /// "Revisions=MAX(P.Revision)", "PageId=Stats.PageId", plus every "Last*"/"Total*" statistics column): no
        /// <paramref name="orderBy"/> falls back to the script's own un-transposed "ORDER BY MAX(P.Name)"; an
        /// unrecognized <paramref name="orderBy"/> throws the same "No order by mapping..." message
        /// <c>RepositoryHelpers.TransposeOrderby</c> throws; direction is ascending only when
        /// <paramref name="orderByDirection"/> is exactly "asc" (case-insensitively), descending for anything else
        /// including null - same as the script/helper's own direction handling (see
        /// <see cref="EfConfigurationRepository.GetAllMenuItems"/> for the same pattern applied to a different
        /// script).
        /// </summary>
        public async Task<List<TwPageStatistics>> GetPageStatisticsPaged(
            int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            pageSize ??= await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            using var context = _createContext();

            var totalCount = await context.PageStatistics.CountAsync();
            var paginationPageCount = (totalCount + (pageSize.Value - 1)) / pageSize.Value;

            var ordered = ApplyOrder(context.PageStatistics, orderBy, orderByDirection);

            return await ordered
                .Skip((pageNumber - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .Select(ps => new TwPageStatistics
                {
                    PageName = ps.Page.Name,
                    Navigation = ps.Page.Navigation,
                    Namespace = ps.Page.Namespace,
                    Revisions = ps.Page.Revision,
                    PageId = ps.PageId,
                    TotalViewCount = ps.TotalViewCount,
                    LastCompileDateTime = ps.LastCompileDateTime,
                    TotalCompilationCount = ps.TotalCompilationCount,
                    LastWikifyTimeMs = (decimal)(ps.LastWikifyTimeMs ?? 0),
                    TotalWikifyTimeMs = (decimal)(ps.TotalWikifyTimeMs ?? 0),
                    LastMatchCount = ps.LastMatchCount ?? 0,
                    LastErrorCount = ps.LastErrorCount ?? 0,
                    LastOutgoingLinkCount = ps.LastOutgoingLinkCount ?? 0,
                    LastTagCount = ps.LastTagCount ?? 0,
                    LastProcessedBodySize = ps.LastProcessedBodySize ?? 0,
                    LastBodySize = ps.LastBodySize ?? 0,
                    PaginationPageSize = pageSize.Value,
                    PaginationPageCount = paginationPageCount,
                }).ToListAsync();
        }

        /// <summary>
        /// Shared ordering logic for <see cref="GetPageStatisticsPaged"/> - see that method's doc comment for the
        /// field-mapping/direction rules this implements.
        /// </summary>
        private static IOrderedQueryable<StatisticsEntities.PageStatistic> ApplyOrder(
            IQueryable<StatisticsEntities.PageStatistic> query, string? orderBy, string? orderByDirection)
        {
            if (string.IsNullOrEmpty(orderBy))
            {
                return query.OrderBy(ps => ps.Page.Name);
            }

            bool ascending = string.Equals(orderByDirection, "asc", StringComparison.InvariantCultureIgnoreCase);

            return orderBy.ToUpperInvariant() switch
            {
                "NAME" => ascending ? query.OrderBy(ps => ps.Page.Name) : query.OrderByDescending(ps => ps.Page.Name),
                "NAVIGATION" => ascending ? query.OrderBy(ps => ps.Page.Navigation) : query.OrderByDescending(ps => ps.Page.Navigation),
                "REVISIONS" => ascending ? query.OrderBy(ps => ps.Page.Revision) : query.OrderByDescending(ps => ps.Page.Revision),
                "PAGEID" => ascending ? query.OrderBy(ps => ps.PageId) : query.OrderByDescending(ps => ps.PageId),
                "LASTCOMPILEDATETIME" => ascending ? query.OrderBy(ps => ps.LastCompileDateTime) : query.OrderByDescending(ps => ps.LastCompileDateTime),
                "TOTALCOMPILATIONCOUNT" => ascending ? query.OrderBy(ps => ps.TotalCompilationCount) : query.OrderByDescending(ps => ps.TotalCompilationCount),
                "TOTALVIEWCOUNT" => ascending ? query.OrderBy(ps => ps.TotalViewCount) : query.OrderByDescending(ps => ps.TotalViewCount),
                "LASTWIKIFYTIMEMS" => ascending ? query.OrderBy(ps => ps.LastWikifyTimeMs) : query.OrderByDescending(ps => ps.LastWikifyTimeMs),
                "TOTALWIKIFYTIMEMS" => ascending ? query.OrderBy(ps => ps.TotalWikifyTimeMs) : query.OrderByDescending(ps => ps.TotalWikifyTimeMs),
                "LASTMATCHCOUNT" => ascending ? query.OrderBy(ps => ps.LastMatchCount) : query.OrderByDescending(ps => ps.LastMatchCount),
                "LASTERRORCOUNT" => ascending ? query.OrderBy(ps => ps.LastErrorCount) : query.OrderByDescending(ps => ps.LastErrorCount),
                "LASTOUTGOINGLINKCOUNT" => ascending ? query.OrderBy(ps => ps.LastOutgoingLinkCount) : query.OrderByDescending(ps => ps.LastOutgoingLinkCount),
                "LASTTAGCOUNT" => ascending ? query.OrderBy(ps => ps.LastTagCount) : query.OrderByDescending(ps => ps.LastTagCount),
                "LASTPROCESSEDBODYSIZE" => ascending ? query.OrderBy(ps => ps.LastProcessedBodySize) : query.OrderByDescending(ps => ps.LastProcessedBodySize),
                "LASTBODYSIZE" => ascending ? query.OrderBy(ps => ps.LastBodySize) : query.OrderByDescending(ps => ps.LastBodySize),
                _ => throw new InvalidOperationException(
                    $"No order by mapping was found in 'GetPageStatisticsPaged.sql' for the field '{orderBy}'."),
            };
        }

        /// <summary>
        /// Mirrors DeletePageStatisticsByPageId.sql's intent ("delete every PageStatistics row for
        /// <paramref name="pageId"/>, returning how many records were deleted") via EF Core's LINQ bulk
        /// <c>ExecuteDeleteAsync</c>, which returns the real affected-row count.
        /// </summary>
        /// <remarks>
        /// <b>⚠ Confirmed bug in the SQLite reference, deliberately not reproduced here.</b> The reference script
        /// is a bare <c>DELETE FROM [PageStatistics] WHERE PageId = @PageId;</c> with no trailing <c>SELECT</c>,
        /// but <c>StatisticsRepository.DeletePageStatisticsByPageId</c> runs it through
        /// <c>StatisticsFactory.ExecuteScalarAsync&lt;int&gt;</c> - Dapper's <c>ExecuteScalarAsync&lt;T&gt;</c>
        /// reads the first column of the first row of a result set, and a plain <c>DELETE</c> with no
        /// <c>RETURNING</c>/<c>SELECT</c> produces no result set at all, so the call always parses a null scalar
        /// into <c>default(int)</c> - i.e. 0 - regardless of how many rows were actually deleted. Confirmed
        /// empirically against a live SQLite database (via <c>NTDLS.SqliteDapperWrapper</c>, the same package the
        /// reference uses): deleting an existing row still returns 0 even though the row is genuinely gone
        /// afterward. The interface's own doc comment ("Returns the number of records deleted") describes the
        /// evidently-intended behavior, which this method actually implements; the SQLite reference's always-0
        /// return value is not consumed by its one caller (<c>PageRepository.MovePageToDeletedById</c> only awaits
        /// it), so this divergence has no observable behavioral impact on any existing caller.
        /// </remarks>
        public async Task<int> DeletePageStatisticsByPageId(int pageId)
        {
            using var context = _createContext();
            return await context.PageStatistics.Where(ps => ps.PageId == pageId).ExecuteDeleteAsync();
        }
    }
}
