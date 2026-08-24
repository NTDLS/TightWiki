using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;
using LoggingEntities = TightWiki.Data.EfCore.Entities.Logging;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Data.EfCore.Repositories
{
    /// <summary>
    /// Provider-agnostic (SQL Server/Postgres, per Database-Providers-Plan.md chapter 3) LINQ-over-EF-Core
    /// implementation of <see cref="ITwLoggingRepository"/>. Lives in the shared <c>TightWiki.Data.EfCore</c>
    /// project rather than a per-provider driver project, for the same reason as <see cref="EfConfigurationRepository"/>
    /// (see that class's doc comment): plain LINQ against <see cref="TightWikiDbContext"/> needs no provider-specific
    /// code here at all. Originally landed as a <c>SqlServerLoggingRepository</c> stub under
    /// <c>TightWiki.Data.EfCore.SqlServer/Repositories/</c> in phase 2a.1; moved and implemented for real here in
    /// phase 2a.7.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reference semantics throughout are the SQLite implementation, <c>TightWiki.Repository.LoggingRepository</c>,
    /// and its backing <c>Scripts\CreateLogTable.sql</c>/<c>CreateSeverityTable.sql</c>/<c>PurgeLogs.sql</c>/
    /// <c>InsertLog.sql</c>/<c>GetExceptionCount.sql</c>/<c>GetSeverities.sql</c>/<c>GetLogEntriesPaged.sql</c>/
    /// <c>GetLogEntryById.sql</c> - see each method's doc comment below for the specific script it mirrors,
    /// including two behavioral quirks that are deliberately preserved rather than "fixed":
    /// <list type="bullet">
    /// <item><description>Every read (<see cref="GetExceptionCount"/>, <see cref="GetLogEntriesPaged"/>,
    /// <see cref="GetLogEntryById"/>) mirrors its script's <c>INNER JOIN Severity S ON L.SeverityId = S.Id</c>: a
    /// Log row whose <c>SeverityId</c> does not resolve to any Severity row (the column is nullable in the real
    /// schema, see <see cref="LoggingEntities.Log.SeverityId"/>) is silently excluded, not just filtered out when a
    /// specific severity is requested.</description></item>
    /// <item><description><see cref="WriteLog"/> mirrors InsertLog.sql's "<c>SELECT S.Id, ... FROM Severity S
    /// WHERE S.Name = @SeverityName</c>": if <paramref name="severity"/>'s <see cref="LogLevel.ToString"/> does not
    /// resolve to any seeded Severity row, the insert silently affects zero rows rather than throwing - in
    /// practice this never happens, since <see cref="LogLevel"/>'s 7 values map 1:1 onto the 7 rows
    /// <c>SeverityConfiguration.HasData</c> seeds, but the SQL semantics are preserved anyway.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Takes a <see cref="Func{TightWikiDbContext}"/> rather than an injected context instance, mirroring
    /// <see cref="EfConfigurationRepository"/> - see that class's doc comment for why. Also takes an
    /// <see cref="ITwConfigurationRepository"/> instance directly (not another <see cref="Func{TResult}"/>),
    /// mirroring the SQLite reference constructor's own <c>ConfigurationRepository configurationRepository</c>
    /// parameter - <see cref="GetLogEntriesPaged"/> is the only member that needs it, to read the
    /// "Pagination Size" customization setting.
    /// </para>
    /// </remarks>
    public sealed class EfLoggingRepository : ITwLoggingRepository
    {
        private readonly Func<TightWikiDbContext> _createContext;
        private readonly ITwConfigurationRepository _configurationRepository;

        public EfLoggingRepository(Func<TightWikiDbContext> createContext, ITwConfigurationRepository configurationRepository)
        {
            _createContext = createContext;
            _configurationRepository = configurationRepository;
        }

        /// <summary>
        /// Mirrors PurgeLogs.sql ("DELETE FROM Log;") via EF Core's LINQ bulk <c>ExecuteDeleteAsync</c> - fully
        /// provider-portable, no raw SQL needed. Severity rows are untouched, same as the SQLite reference.
        /// </summary>
        public async Task PurgeLogs()
        {
            using var context = _createContext();
            await context.Logs.ExecuteDeleteAsync();
        }

        /// <summary>
        /// No-op for the EF Core providers. The SQLite reference (<c>LoggingRepository.CreateTablesIfNotExist</c>,
        /// called from its own constructor) exists because SQLite has no separate migration mechanism at startup -
        /// it has to create Logging.Log/Logging.Severity ad hoc, the first time a <c>LoggingRepository</c> is ever
        /// constructed against a fresh database file. Here, both tables are created by
        /// <c>SqlServerDatabaseManager.InitializeSchema()</c>'s EF Core Migrations (Database-Providers-Plan.md
        /// chapter 4.2) before this repository is ever used, so there is nothing left for this method to do.
        /// </summary>
        public Task CreateTablesIfNotExist() => Task.CompletedTask;

        /// <summary>
        /// Mirrors <c>LoggingRepository.WriteException</c> exactly (no SQL script of its own): writes an
        /// <see cref="LogLevel.Error"/>-severity entry via <see cref="WriteLog"/>.
        /// </summary>
        public async Task WriteException(string? text = null, string? exceptionText = null, string? stackTrace = null)
            => await WriteLog(LogLevel.Error, text, exceptionText, stackTrace);

        /// <summary>
        /// Mirrors InsertLog.sql - see this class's doc comment for the "unresolvable severity name" quirk this
        /// deliberately preserves. Also mirrors the SQLite reference's console echo of every
        /// <see cref="LogLevel.Warning"/>-or-higher entry (a pre-existing, seemingly debug-only side effect of the
        /// SQLite reference, kept here for behavioral parity rather than as a deliberate logging design).
        /// </summary>
        public async Task WriteLog(LogLevel severity, string? text = null, string? exceptionText = null, string? stackTrace = null)
        {
            if (severity >= LogLevel.Warning)
            {
                Console.WriteLine($"{text} {exceptionText} {stackTrace}");
            }

            using var context = _createContext();

            var severityId = await context.Severities
                .Where(s => s.Name == severity.ToString())
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync();

            if (severityId == null)
            {
                //Matches InsertLog.sql's silent zero-row insert when @SeverityName does not resolve - see this
                //class's doc comment.
                return;
            }

            context.Logs.Add(new LoggingEntities.Log
            {
                SeverityId = severityId,
                Text = text,
                ExceptionText = exceptionText,
                StackTrace = stackTrace,
                CreatedDate = DateTime.UtcNow,
            });

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Mirrors GetExceptionCount.sql - a count of Log rows whose (inner-joined) Severity name is exactly
        /// "Error", i.e. <see cref="LogLevel.Error"/>-severity entries only (not <see cref="LogLevel.Critical"/>).
        /// </summary>
        public async Task<int> GetExceptionCount()
        {
            using var context = _createContext();

            return await context.Logs.CountAsync(l => l.Severity != null && l.Severity.Name == "Error");
        }

        /// <summary>
        /// Mirrors GetSeverities.sql - every Severity row, ordered by Name.
        /// </summary>
        public async Task<List<TwEventLogSeverity>> GetSeverities()
        {
            using var context = _createContext();

            return await context.Severities
                .OrderBy(s => s.Name)
                .Select(s => new TwEventLogSeverity
                {
                    Id = s.Id,
                    Name = s.Name,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors GetLogEntriesPaged.sql: paginated, optionally severity-filtered Log rows (inner-joined to
        /// Severity - see this class's doc comment), with <see cref="TwLogEntry.PaginationPageCount"/> computed
        /// from the same filtered set's total row count via the script's own ceiling-division formula
        /// (<c>(Count(0) + (@PageSize - 1)) / @PageSize</c>). Ordering mirrors
        /// <c>RepositoryHelpers.TransposeOrderby</c> against the script's <c>--CONFIG::</c> mapping
        /// ("Id=Id", "CreatedDate=[CreatedDate]"): no <paramref name="orderBy"/> falls back to the script's own
        /// un-transposed "ORDER BY L.Id DESC"; an unrecognized <paramref name="orderBy"/> throws the same
        /// "No order by mapping..." message <c>RepositoryHelpers.TransposeOrderby</c> throws; direction is
        /// ascending only when <paramref name="orderByDirection"/> is exactly "asc" (case-insensitively),
        /// descending for anything else including null - same as the script/helper's own direction handling
        /// (see <see cref="EfConfigurationRepository.GetAllMenuItems"/> for the same pattern applied to a
        /// different script).
        /// </summary>
        public async Task<List<TwLogEntry>> GetLogEntriesPaged(int pageNumber,
            string? orderBy = null, string? orderByDirection = null, string? severity = null)
        {
            //Get<T> is declared as "Task<T?> Get<T>(...)" over an unconstrained T - without a "struct" constraint,
            //Nullable<T> is not available, so for a value type argument like int the "?" is a no-op annotation and
            //this genuinely returns a plain Task<int> (same as the SQLite reference's own
            //"var paginationSize = await _configurationRepository.Get<int>(...)").
            var paginationSize = await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            using var context = _createContext();

            //Mirrors GetLogEntriesPaged.sql's "INNER JOIN Severity S ON L.SeverityId = S.Id" - see this class's
            //doc comment.
            var filtered = context.Logs.Where(l => l.Severity != null);

            if (severity != null)
            {
                filtered = filtered.Where(l => l.Severity!.Name == severity);
            }

            var totalCount = await filtered.CountAsync();
            var paginationPageCount = (totalCount + (paginationSize - 1)) / paginationSize;

            IOrderedQueryable<LoggingEntities.Log> ordered;
            if (string.IsNullOrEmpty(orderBy))
            {
                ordered = filtered.OrderByDescending(l => l.Id);
            }
            else
            {
                bool ascending = string.Equals(orderByDirection, "asc", StringComparison.InvariantCultureIgnoreCase);

                ordered = orderBy.ToUpperInvariant() switch
                {
                    "ID" => ascending
                        ? filtered.OrderBy(l => l.Id)
                        : filtered.OrderByDescending(l => l.Id),
                    "CREATEDDATE" => ascending
                        ? filtered.OrderBy(l => l.CreatedDate)
                        : filtered.OrderByDescending(l => l.CreatedDate),
                    _ => throw new InvalidOperationException(
                        $"No order by mapping was found in 'GetLogEntriesPaged.sql' for the field '{orderBy}'."),
                };
            }

            return await ordered
                .Skip((pageNumber - 1) * paginationSize)
                .Take(paginationSize)
                .Select(l => new TwLogEntry
                {
                    Id = l.Id,
                    Severity = l.Severity!.Name,
                    Text = l.Text ?? string.Empty,
                    ExceptionText = l.ExceptionText ?? string.Empty,
                    StackTrace = l.StackTrace ?? string.Empty,
                    CreatedDate = l.CreatedDate ?? default,
                    PaginationPageCount = paginationPageCount,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors GetLogEntryById.sql, via <see cref="Queryable.SingleAsync{TSource}(IQueryable{TSource},System.Threading.CancellationToken)"/> -
        /// like the SQLite reference's <c>QuerySingleAsync</c>, this throws if no row matches (either because
        /// <paramref name="id"/> does not exist, or - see this class's doc comment - because it matches a Log row
        /// whose Severity does not resolve) rather than returning null, matching the interface's non-nullable
        /// <see cref="TwLogEntry"/> return type. Id is the primary key, so at most one row can ever match.
        /// </summary>
        public async Task<TwLogEntry> GetLogEntryById(int id)
        {
            using var context = _createContext();

            return await context.Logs
                .Where(l => l.Id == id && l.Severity != null)
                .Select(l => new TwLogEntry
                {
                    Id = l.Id,
                    Severity = l.Severity!.Name,
                    Text = l.Text ?? string.Empty,
                    ExceptionText = l.ExceptionText ?? string.Empty,
                    StackTrace = l.StackTrace ?? string.Empty,
                    CreatedDate = l.CreatedDate ?? default,
                }).SingleAsync();
        }
    }
}
