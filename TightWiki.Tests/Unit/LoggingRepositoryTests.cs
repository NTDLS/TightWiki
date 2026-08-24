using Microsoft.Extensions.Logging;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;

namespace TightWiki.Tests.Unit
{
    /// <summary>
    /// Scenario-level integration tests for <see cref="ITwLoggingRepository"/> (8 methods - write/read of the
    /// plugin's local exception/event log), obtained through <see cref="TwEngineFixture"/> exactly like
    /// <see cref="ConfigurationRepositoryTests"/> gets <c>ITwConfigurationRepository</c>. Written entirely against
    /// the provider-agnostic interface - no <c>#if SQLITE_PROVIDER</c>/<c>#elif SQLSERVER_PROVIDER</c>/
    /// <c>#elif POSTGRES_PROVIDER</c> branching here. The same compiled test code runs three times:
    /// <c>dotnet test</c> (SQLite, default), <c>dotnet test -p:DataProvider=SqlServer</c>,
    /// <c>dotnet test -p:DataProvider=Postgres</c> (Database-Providers-Testing-Plan.md chapter 5.5).
    /// </summary>
    /// <remarks>
    /// These run against the shared, persistent test database (chapter 5.3 - not a fresh-empty database, and not
    /// per-test transaction rollback), concurrently with every other xunit collection in this assembly (default
    /// xunit parallelization - see <see cref="ConfigurationRepositoryTests"/>'s own remarks for the same
    /// reasoning). The Log table is never empty even on a freshly-seeded database - <c>DatabaseLogger</c>
    /// (<c>TightWiki.Library</c>) actively writes real Information-and-above severity entries to it as the fixture
    /// and engine run - so every test here identifies its own row(s) by a GUID embedded in the log text rather than
    /// by any absolute row count, and every assertion about counts is a before/after delta rather than a fixed
    /// expected number. Every test here is therefore either:
    /// <list type="bullet">
    /// <item><description>read-only against already-seeded data (<see cref="GetSeverities_ReturnsAllSevenLogLevels_OrderedByName"/>,
    /// safe to run concurrently, any number of times), or</description></item>
    /// <item><description>a pure insert of a new, GUID-suffixed "TestLog_"-prefixed row that this test locates
    /// afterwards by scanning <see cref="ITwLoggingRepository.GetLogEntriesPaged"/> for its own text - the interface
    /// has no way to insert-and-get-the-new-id directly, mirroring <c>LoggingRepository.WriteLog</c>/<c>WriteException</c>'s
    /// own <c>void</c>-shaped (no returned identity) signatures), or</description></item>
    /// <item><description><see cref="PurgeLogs_DeletesTheWrittenEntry_ButLeavesSeverityRowsUntouched"/>, which
    /// deletes every row in Log (mirrors <c>PurgeLogs.sql</c>'s unconditional <c>DELETE FROM Log;</c>) - safe to
    /// re-run against the shared database because nothing else in this assembly asserts on Log's row count or
    /// contents (the only other reference, <c>DatabaseTests.EmptyEventLog</c>, is commented-out dead code), and
    /// because tests within one xunit collection run sequentially, so this never races the other tests in this same
    /// class.</description></item>
    /// </list>
    /// </remarks>
    [Collection("Logging Repository Tests")]
    public class LoggingRepositoryTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        /// <summary>
        /// Scans <see cref="ITwLoggingRepository.GetLogEntriesPaged"/>, filtered to <paramref name="severity"/> and
        /// ordered newest-first by CreatedDate, page by page (bounded by the first page's own
        /// <see cref="TwLogEntry.PaginationPageCount"/>) until a row with an exact <paramref name="text"/> match is
        /// found. A single page isn't assumed to be enough - the shared test database's pagination size may be
        /// small, and other xunit collections running in parallel may be concurrently writing their own
        /// Information-and-above log entries via <c>DatabaseLogger</c>, which could otherwise push a just-written
        /// row past page 1.
        /// </summary>
        private static async Task<TwLogEntry> FindLogEntryByTextAsync(ITwLoggingRepository repo, string severity, string text)
        {
            var firstPage = await repo.GetLogEntriesPaged(1, orderBy: "CreatedDate", orderByDirection: "desc", severity: severity);
            var totalPages = firstPage.Count > 0 ? firstPage[0].PaginationPageCount : 1;

            for (var pageNumber = 1; pageNumber <= totalPages; pageNumber++)
            {
                var page = pageNumber == 1
                    ? firstPage
                    : await repo.GetLogEntriesPaged(pageNumber, orderBy: "CreatedDate", orderByDirection: "desc", severity: severity);

                var match = page.FirstOrDefault(e => e.Text == text);
                if (match != null)
                {
                    return match;
                }
            }

            throw new Exception(
                $"Could not locate a log entry with severity '{severity}' and text '{text}' across {totalPages} page(s).");
        }

        [Fact]
        public async Task WriteLog_PersistsRoundTrip_ViaGetLogEntriesPagedAndGetLogEntryById()
        {
            var repo = fixture.Artifacts.DatabaseManager.LoggingRepository;

            //Exercises CreateTablesIfNotExist explicitly - a no-op on the EF Core providers (Log/Severity already
            //exist via migrations by the time any repository is used) and idempotent on SQLite (DoesTableExist
            //guard) even though the fixture's own repository construction already called it once. Calling it again
            //here must not throw on any provider.
            await repo.CreateTablesIfNotExist();

            var marker = Guid.NewGuid().ToString("N");
            var text = $"TestLog_Text_{marker}";
            var exceptionText = $"TestLog_Exception_{marker}";
            var stackTrace = $"TestLog_StackTrace_{marker}";

            await repo.WriteLog(LogLevel.Warning, text, exceptionText, stackTrace);

            var foundEntry = await FindLogEntryByTextAsync(repo, LogLevel.Warning.ToString(), text);
            Assert.Equal(LogLevel.Warning.ToString(), foundEntry.Severity);
            Assert.Equal(text, foundEntry.Text);
            Assert.Equal(exceptionText, foundEntry.ExceptionText);
            Assert.Equal(stackTrace, foundEntry.StackTrace);

            //GetLogEntryById is a second, independent read path (by primary key rather than by paged scan) of the
            //exact same row - every field must agree with what the paged scan already found, including CreatedDate
            //(compared row-to-row here rather than against wall-clock "now", to stay independent of any
            //provider-specific DateTime storage/precision/timezone differences).
            var byId = await repo.GetLogEntryById(foundEntry.Id);
            Assert.Equal(foundEntry.Id, byId.Id);
            Assert.Equal(foundEntry.Severity, byId.Severity);
            Assert.Equal(foundEntry.Text, byId.Text);
            Assert.Equal(foundEntry.ExceptionText, byId.ExceptionText);
            Assert.Equal(foundEntry.StackTrace, byId.StackTrace);
            Assert.Equal(foundEntry.CreatedDate, byId.CreatedDate);
        }

        [Fact]
        public async Task WriteException_WritesErrorSeverityEntry_AndIncreasesGetExceptionCount()
        {
            var repo = fixture.Artifacts.DatabaseManager.LoggingRepository;

            var countBefore = await repo.GetExceptionCount();

            var marker = Guid.NewGuid().ToString("N");
            var text = $"TestLog_ExcText_{marker}";
            var exceptionText = $"TestLog_ExcExceptionText_{marker}";
            var stackTrace = $"TestLog_ExcStackTrace_{marker}";

            await repo.WriteException(text, exceptionText, stackTrace);

            var countAfter = await repo.GetExceptionCount();

            //A before/after delta rather than an exact "+1" - GetExceptionCount reads the shared, persistent, Log
            //table that other xunit collections running in parallel may also be writing Error-severity entries
            //into between the two reads (never removing any: nothing else in this assembly purges Log
            //concurrently with this test - see this class's own remarks). The count can therefore only have gone
            //up by at least our own write.
            Assert.True(countAfter > countBefore,
                $"Expected GetExceptionCount() to increase after WriteException (before={countBefore}, after={countAfter}).");

            //WriteException always logs at LogLevel.Error (mirrors LoggingRepository.WriteException) - confirm our
            //specific entry really landed with that severity and its full text/exception/stack-trace payload.
            var foundEntry = await FindLogEntryByTextAsync(repo, LogLevel.Error.ToString(), text);
            Assert.Equal(LogLevel.Error.ToString(), foundEntry.Severity);
            Assert.Equal(text, foundEntry.Text);
            Assert.Equal(exceptionText, foundEntry.ExceptionText);
            Assert.Equal(stackTrace, foundEntry.StackTrace);
        }

        [Fact]
        public async Task GetSeverities_ReturnsAllSevenLogLevels_OrderedByName()
        {
            var repo = fixture.Artifacts.DatabaseManager.LoggingRepository;

            var severities = await repo.GetSeverities();

            //Mirrors CreateSeverityTable.sql's seed of exactly the 7 Microsoft.Extensions.Logging.LogLevel names
            //(Trace, Debug, Information, Warning, Error, Critical, None) - see EfLoggingRepository's own doc
            //comment for the same 1:1 mapping relied on by WriteLog's severity-name lookup.
            var expectedNames = Enum.GetNames(typeof(LogLevel));
            Assert.Equal(expectedNames.Length, severities.Count);
            Assert.Equal(
                expectedNames.OrderBy(n => n, StringComparer.Ordinal),
                severities.Select(s => s.Name).OrderBy(n => n, StringComparer.Ordinal));

            //Mirrors GetSeverities.sql's "ORDER BY Name" - assert the repository itself already returns them
            //sorted, not just that sorting the result would happen to match.
            var namesInReturnedOrder = severities.Select(s => s.Name).ToList();
            var sortedNames = namesInReturnedOrder.OrderBy(n => n, StringComparer.Ordinal).ToList();
            Assert.Equal(sortedNames, namesInReturnedOrder);

            Assert.All(severities, s => Assert.True(s.Id > 0));
        }

        [Fact]
        public async Task PurgeLogs_DeletesTheWrittenEntry_ButLeavesSeverityRowsUntouched()
        {
            var repo = fixture.Artifacts.DatabaseManager.LoggingRepository;

            var marker = Guid.NewGuid().ToString("N");
            var text = $"TestLog_PurgeText_{marker}";

            await repo.WriteLog(LogLevel.Critical, text, $"TestLog_PurgeExc_{marker}", $"TestLog_PurgeStack_{marker}");

            var foundEntry = await FindLogEntryByTextAsync(repo, LogLevel.Critical.ToString(), text);

            //Sanity check pre-purge: the row is really there and independently readable by primary key.
            var beforePurge = await repo.GetLogEntryById(foundEntry.Id);
            Assert.Equal(text, beforePurge.Text);

            await repo.PurgeLogs();

            //PurgeLogs deletes every row in Log unconditionally - our entry's Id can no longer resolve, the same
            //"zero rows -> throw" behavior GetLogEntryById/GetMenuItemById already exhibit for any nonexistent Id
            //(see ConfigurationRepositoryTests.MenuItem_InsertGetUpdateDelete_RoundTrips for the same pattern
            //applied to ConfigurationRepository.GetMenuItemById).
            await Assert.ThrowsAnyAsync<Exception>(() => repo.GetLogEntryById(foundEntry.Id));

            //Severity rows are untouched by PurgeLogs (mirrors PurgeLogs.sql, which only ever targets Log) - the
            //seeded 7 severities must still all be there afterwards.
            var severitiesAfterPurge = await repo.GetSeverities();
            Assert.Equal(7, severitiesAfterPurge.Count);
        }
    }
}
