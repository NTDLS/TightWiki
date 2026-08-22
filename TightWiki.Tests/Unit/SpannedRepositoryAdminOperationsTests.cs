using TightWiki.Plugin.Interfaces;

namespace TightWiki.Tests.Unit
{
    /// <summary>
    /// Scenario-level integration tests for the 7 <see cref="ISpannedRepository"/> admin/DBA-maintenance members -
    /// exposed directly on <see cref="ITwDatabaseManager"/> itself (<see cref="ITwDatabaseManager.VacuumDatabase"/>,
    /// <see cref="ITwDatabaseManager.OptimizeDatabase"/>, <see cref="ITwDatabaseManager.IntegrityCheckDatabase"/>,
    /// <see cref="ITwDatabaseManager.ForeignKeyCheck"/>, <see cref="ITwDatabaseManager.GetDatabaseVersions"/>,
    /// <see cref="ITwDatabaseManager.GetDatabasePageCounts"/>, <see cref="ITwDatabaseManager.GetDatabasePageSizes"/>
    /// - the operations behind <c>AdminController</c>'s "Database" admin screen). Obtained through
    /// <see cref="TwEngineFixture"/> exactly like every sibling <c>*RepositoryTests</c> class gets its own
    /// repository. Written entirely against the provider-agnostic <see cref="ITwDatabaseManager"/> interface - no
    /// <c>#if SQLITE_PROVIDER</c>/<c>#elif SQLSERVER_PROVIDER</c>/<c>#elif POSTGRES_PROVIDER</c> branching here,
    /// though several assertions do branch at runtime on <see cref="ITwDatabaseManager.GetType"/>'s
    /// <see cref="Type.Name"/> where the three implementations' documented, by-design status-message wording
    /// genuinely differs (mirrors <c>BootstrapSeedRegressionTests</c>'s own Bug 4 test, per this task's own
    /// instruction to prefer a runtime check over a compile-time <c>#if</c>). The same compiled test code runs
    /// three times: <c>dotnet test</c> (SQLite, default), <c>dotnet test -p:DataProvider=SqlServer</c>,
    /// <c>dotnet test -p:DataProvider=Postgres</c> (Database-Providers-Testing-Plan.md chapter 5.5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// All 7 members are DBA-maintenance operations (VACUUM/ANALYZE/CHECKDB-equivalents), not data-mutating - safe
    /// to run against the shared, persistent test database (chapter 5.3) any number of times, and safe to run
    /// concurrently with every other xunit collection in this assembly (default xunit parallelization: collections
    /// run in parallel with each other, tests within one collection run sequentially - same reasoning as every
    /// sibling repository test class). Every <c>databaseName</c> argument below is the literal string "Pages" - the
    /// only value that matters on every provider: the SQLite reference implementation
    /// (<c>TightWiki.Repository.Helpers.DatabaseManager</c>) looks it up by exact match against its own 8-entry
    /// <c>Databases</c> array and throws if no match is found, while both EF Core drivers
    /// (<c>SqlServerDatabaseManager</c>/<c>PostgresDatabaseManager</c>) ignore the argument entirely post-schema-
    /// consolidation (confirmed by reading both - every one of their 4 status-returning methods' own doc comment
    /// says so explicitly: "accepted only for interface compatibility") and always operate against the whole
    /// (single, consolidated) database regardless of what is passed - so "Pages" is simply a valid, real SQLite
    /// database name that is also harmlessly accepted (and ignored) by the other two.
    /// </para>
    /// <para>
    /// None of the operations below intentionally break foreign key/data integrity to exercise the "found a
    /// violation" branch of <see cref="ITwDatabaseManager.ForeignKeyCheck"/>/<see cref="ITwDatabaseManager.IntegrityCheckDatabase"/>
    /// - deliberately out of scope per this task's own instruction (too risky against a database shared with every
    /// other concurrently-running test class/collection). Every assertion below only checks the healthy-database
    /// "no problems found" path.
    /// </para>
    /// </remarks>
    [Collection("Spanned Repository Admin Operations Tests")]
    public class SpannedRepositoryAdminOperationsTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        /// <summary>
        /// The 8 schema/database names every provider reports one row per, confirmed by reading the SQLite
        /// reference's own <c>Databases</c> array (<c>TightWiki.Repository.Helpers.DatabaseManager</c>) and both EF
        /// drivers' own <c>TargetSchemas</c> array (<c>SqlServerDatabaseManager</c>/<c>PostgresDatabaseManager</c>) -
        /// all three declare the very same 8 names. Compared as a set (order-independent) below, since ordering
        /// isn't part of any documented contract even though all three implementations happen to agree on it today.
        /// </summary>
        private static readonly string[] ExpectedSchemaNames =
        [
            "DeletedPageRevisions", "DeletedPages", "Pages", "Statistics", "Emoji", "Logging", "Users", "Config"
        ];

        /// <summary>
        /// The database/schema name passed to every <c>databaseName</c> parameter below - see this class's own
        /// remarks for why "Pages" specifically works, unmodified, on every provider.
        /// </summary>
        private const string TargetDatabaseName = "Pages";

        /// <summary>
        /// Exercises <see cref="ITwDatabaseManager.VacuumDatabase"/>/<see cref="ITwDatabaseManager.OptimizeDatabase"/>
        /// - must not throw, and must return a non-null result. Only the two EF drivers are additionally asserted
        /// to return a non-empty, descriptive status string: the SQLite reference's own <c>VacuumDatabase.sql</c>
        /// ("VACUUM;") and <c>OptimizeDatabase.sql</c> ("PRAGMA optimize;") are both statements that produce zero
        /// result rows, and <c>DatabaseManager.VacuumDatabase</c>/<c>OptimizeDatabase</c> build their return value
        /// via <c>string.Join("\r\n", results)</c> over whatever rows came back - zero rows in, empty string out.
        /// That is expected, correct SQLite behavior (confirmed empirically - not a bug to route around), so this
        /// only requires a non-null result there, not a non-empty one.
        /// </summary>
        [Fact]
        public async Task VacuumDatabase_And_OptimizeDatabase_CompleteWithoutException_AndReturnAResult()
        {
            var databaseManager = fixture.Artifacts.DatabaseManager;
            //SQLite's VacuumDatabase/OptimizeDatabase legitimately return an empty string - see this test's own
            //doc comment - so the "non-empty, descriptive status" assertion below is skipped for that provider.
            var expectsDescriptiveStatusText = databaseManager.GetType().Name != "DatabaseManager";

            var vacuumResult = await databaseManager.VacuumDatabase(TargetDatabaseName);
            Assert.NotNull(vacuumResult);
            if (expectsDescriptiveStatusText)
            {
                Assert.False(string.IsNullOrWhiteSpace(vacuumResult),
                    "VacuumDatabase should return a non-empty status message describing what it did.");
            }

            var optimizeResult = await databaseManager.OptimizeDatabase(TargetDatabaseName);
            Assert.NotNull(optimizeResult);
            if (expectsDescriptiveStatusText)
            {
                Assert.False(string.IsNullOrWhiteSpace(optimizeResult),
                    "OptimizeDatabase should return a non-empty status message describing what it did.");
            }
        }

        /// <summary>
        /// Exercises <see cref="ITwDatabaseManager.IntegrityCheckDatabase"/> against the shared, healthy, seeded
        /// database - must not throw, and must report no corruption. Branches at runtime on the concrete
        /// implementation because the three drivers' "healthy" wording genuinely differs by design:
        /// <list type="bullet">
        /// <item><description>SQLite (<c>DatabaseManager.IntegrityCheckDatabase</c>): <c>PRAGMA integrity_check</c>
        /// returns the single row "ok" on a healthy database. The method then unconditionally appends
        /// <c>await ForeignKeyCheck(databaseName)</c>, which on a healthy database returns
        /// <see cref="string.Empty"/> (<c>PRAGMA foreign_key_check</c> reports zero rows - see
        /// <see cref="ForeignKeyCheck_OnHealthySeededDatabase_ReportsNoViolations"/>'s own SQLite branch), so the
        /// real return value is exactly "ok". Previously (before the fix tracked in
        /// Database-Providers-Testing-Findings.md finding #3) that call was missing its <c>await</c>, so string
        /// concatenation invoked <see cref="object.ToString"/> on the unawaited <see cref="Task{TResult}"/> itself
        /// instead of its result, appending a literal "System.Threading.Tasks.Task`1[System.String]" after "ok".
        /// This now asserts exact equality rather than merely a prefix, which fails against that pre-fix
        /// behavior.</description></item>
        /// <item><description>SQL Server (<c>SqlServerDatabaseManager</c>): runs <c>DBCC CHECKDB</c>, returning the
        /// literal "DBCC CHECKDB completed - no corruption or structural issues found." on success.</description></item>
        /// <item><description>PostgreSQL (<c>PostgresDatabaseManager</c>): runs <c>amcheck</c>'s
        /// <c>bt_index_check</c> over every B-tree index, returning a message containing "no corruption found" on
        /// success - or, if the <c>amcheck</c> extension cannot be created (not installed server-side, or the
        /// connection's role lacks <c>CREATE EXTENSION</c> privilege), a distinct "Integrity check skipped..."
        /// message documented as an equally valid, non-error outcome by the method's own doc comment. Both are
        /// accepted here since either is a legitimate "no corruption found" result, not a failure - the test
        /// environment's Postgres role (<c>tightwiki</c>, the container's own bootstrap superuser per
        /// <c>Start-TestDatabases.ps1</c>'s <c>POSTGRES_USER</c>) is expected to have the privilege, but this test
        /// does not hard-fail if a future container/image swap changes that.</description></item>
        /// </list>
        /// </summary>
        [Fact]
        public async Task IntegrityCheckDatabase_OnHealthySeededDatabase_ReportsNoCorruption()
        {
            var databaseManager = fixture.Artifacts.DatabaseManager;

            var result = await databaseManager.IntegrityCheckDatabase(TargetDatabaseName);
            Assert.False(string.IsNullOrWhiteSpace(result));

            switch (databaseManager.GetType().Name)
            {
                case "SqlServerDatabaseManager":
                    Assert.Contains("no corruption or structural issues found", result, StringComparison.OrdinalIgnoreCase);
                    break;
                case "PostgresDatabaseManager":
                    Assert.True(
                        result.Contains("no corruption found", StringComparison.OrdinalIgnoreCase)
                        || result.Contains("integrity check skipped", StringComparison.OrdinalIgnoreCase),
                        $"Expected either a clean amcheck result or the documented 'skipped' fallback message, but got: {result}");
                    break;
                default: //SQLite reference (TightWiki.Repository.Helpers.DatabaseManager) - see this test's own doc comment.
                    Assert.Equal("ok", result, StringComparer.OrdinalIgnoreCase);
                    break;
            }
        }

        /// <summary>
        /// Exercises <see cref="ITwDatabaseManager.ForeignKeyCheck"/> against the shared, healthy, seeded database -
        /// must not throw, and must report zero violations. Branches at runtime on the concrete implementation
        /// because the three drivers' "no violations" wording (and even the return value's shape - empty string vs.
        /// a descriptive sentence) genuinely differs by design (confirmed by reading all three <c>ForeignKeyCheck</c>
        /// implementations):
        /// <list type="bullet">
        /// <item><description>SQLite: <c>PRAGMA foreign_key_check</c> returns zero rows on a healthy database, and
        /// <c>string.Join("\r\n", results)</c> over an empty sequence is <see cref="string.Empty"/> - not a
        /// descriptive sentence the way the two EF drivers return.</description></item>
        /// <item><description>SQL Server: <c>DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS</c> reports zero rows, and
        /// the method returns the literal "DBCC CHECKCONSTRAINTS reported no foreign key or check constraint
        /// violations." instead.</description></item>
        /// <item><description>PostgreSQL: a <c>pg_constraint</c> query for un-<c>VALID</c>ated FK/CHECK constraints
        /// finds none (every constraint EF Core migrations create is <c>VALID</c> by default), and the method
        /// returns "No unvalidated foreign key or check constraints found...".</description></item>
        /// </list>
        /// </summary>
        [Fact]
        public async Task ForeignKeyCheck_OnHealthySeededDatabase_ReportsNoViolations()
        {
            var databaseManager = fixture.Artifacts.DatabaseManager;

            var result = await databaseManager.ForeignKeyCheck(TargetDatabaseName);

            switch (databaseManager.GetType().Name)
            {
                case "SqlServerDatabaseManager":
                    Assert.Contains("no foreign key or check constraint violations", result, StringComparison.OrdinalIgnoreCase);
                    break;
                case "PostgresDatabaseManager":
                    Assert.Contains("no unvalidated foreign key or check constraints found", result, StringComparison.OrdinalIgnoreCase);
                    break;
                default: //SQLite reference - see this test's own doc comment for why this is an empty string, not a sentence.
                    Assert.Equal(string.Empty, result);
                    break;
            }
        }

        /// <summary>
        /// Exercises <see cref="ITwDatabaseManager.GetDatabaseVersions"/> - one row per schema, matching
        /// <see cref="ExpectedSchemaNames"/> exactly (as a set), with a non-empty <c>Version</c> string for every
        /// row on every provider (the SQLite engine version, or an EF Core migrations-history <c>MigrationId</c> -
        /// see both EF drivers' own doc comments for the "TightWikiDb: ... / Identity: ..." formatting their shared
        /// "Users" schema row uses).
        /// </summary>
        [Fact]
        public async Task GetDatabaseVersions_ReturnsOneRowPerSchema_WithNonEmptyVersionStrings()
        {
            var databaseManager = fixture.Artifacts.DatabaseManager;

            var versions = await databaseManager.GetDatabaseVersions();

            Assert.Equal(ExpectedSchemaNames.Length, versions.Count);
            Assert.Equal(
                ExpectedSchemaNames.OrderBy(n => n, StringComparer.Ordinal),
                versions.Select(v => v.Name).OrderBy(n => n, StringComparer.Ordinal));

            foreach (var (name, version) in versions)
            {
                Assert.False(string.IsNullOrWhiteSpace(version), $"Expected a non-empty version string for schema '{name}'.");
            }
        }

        /// <summary>
        /// Exercises <see cref="ITwDatabaseManager.GetDatabasePageCounts"/> and
        /// <see cref="ITwDatabaseManager.GetDatabasePageSizes"/> together - both must return one row per schema,
        /// matching <see cref="ExpectedSchemaNames"/> exactly (as a set, same as
        /// <see cref="GetDatabaseVersions_ReturnsOneRowPerSchema_WithNonEmptyVersionStrings"/>), with the same name
        /// set reported by both calls - mirroring the invariant <c>AdminController.Database()</c> itself relies on
        /// (its own <c>pageCounts.FirstOrDefault(o => o.Name == version.Name)</c>/matching lookup for
        /// <c>PageSize</c> silently falls through to a zeroed-out row if the two collections' name sets ever
        /// diverge). Every <c>PageCount</c> is non-negative (a schema with no tables can legitimately report 0 - see
        /// both EF drivers' own <c>LEFT JOIN</c>/<c>COALESCE</c> handling of that case), and every <c>PageSize</c>
        /// is strictly positive on every provider (SQLite's <c>PRAGMA page_size</c>, SQL Server's fixed 8 KB
        /// constant, PostgreSQL's queried <c>block_size</c> - none of which can be 0 for a database that exists at
        /// all).
        /// </summary>
        [Fact]
        public async Task GetDatabasePageCounts_And_GetDatabasePageSizes_ReturnOneRowPerSchema_WithConsistentNonNegativeValues()
        {
            var databaseManager = fixture.Artifacts.DatabaseManager;

            var pageCounts = await databaseManager.GetDatabasePageCounts();
            var pageSizes = await databaseManager.GetDatabasePageSizes();

            Assert.Equal(ExpectedSchemaNames.Length, pageCounts.Count);
            Assert.Equal(
                ExpectedSchemaNames.OrderBy(n => n, StringComparer.Ordinal),
                pageCounts.Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));

            Assert.Equal(ExpectedSchemaNames.Length, pageSizes.Count);
            Assert.Equal(
                ExpectedSchemaNames.OrderBy(n => n, StringComparer.Ordinal),
                pageSizes.Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));

            foreach (var (name, pageCount) in pageCounts)
            {
                Assert.True(pageCount >= 0, $"Expected a non-negative page count for schema '{name}', but got {pageCount}.");
            }

            foreach (var (name, pageSize) in pageSizes)
            {
                Assert.True(pageSize > 0, $"Expected a strictly positive page size for schema '{name}', but got {pageSize}.");
            }
        }
    }
}
