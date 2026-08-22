using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TightWiki.Library;
using TightWiki.Library.Dummy;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
#if SQLITE_PROVIDER
using Microsoft.Data.Sqlite;
using TightWiki.Repository.Helpers;
#elif SQLSERVER_PROVIDER
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TightWiki.Data.EfCore.SqlServer;
#elif POSTGRES_PROVIDER
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TightWiki.Data.EfCore.Postgres;
#endif

namespace TightWiki.Tests.Unit
{
    /// <summary>
    /// T5 (Database-Providers-Testing-Plan.md chapters 4/7) - the one thing no other test in this project
    /// exercises: bootstrapping <see cref="ITwDatabaseManager"/> from a genuinely fresh/empty database, and
    /// explicitly proving that re-running <see cref="ITwDatabaseManager.InitializeSchema"/>/the seed sequence
    /// against an already-initialized database is a safe no-op rather than an assumed side effect. Every sibling
    /// <c>*RepositoryTests</c>/<c>BootstrapSeedRegressionTests</c>/<c>SpannedRepositoryAdminOperationsTests</c>
    /// class deliberately runs against the shared, persistent <c>TightWikiTest</c>/<c>tightwiki_test</c> database
    /// (Database-Providers-Testing-Plan.md chapter 5.3), which by the time any of those tests run has already been
    /// bootstrapped once - this class is the one place that exercises the bootstrap itself, end to end, against its
    /// own dedicated, disposable database (<c>TightWikiFreshTest</c> / <c>tightwiki_fresh_test</c>, same Docker
    /// containers as every other SqlServer/Postgres test - see <c>Start-TestDatabases.ps1</c>) that is dropped
    /// before and after every run - never the shared database every other SqlServer/Postgres test file relies on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deliberately builds its own minimal <see cref="ITwDatabaseManager"/>/<see cref="UserManager{TUser}"/>
    /// directly from an <see cref="IConfiguration"/> pointed at the dedicated database - not through
    /// <see cref="TightWiki.Test.Library.MockWikiEngineArtifacts"/>/<see cref="TwEngineFixture"/>, both of which
    /// are hard-wired to the shared database and cached in a process-wide static field
    /// (<c>TwEngineFixture</c>'s own <c>_artifacts</c>) the very first time any test in the assembly touches them.
    /// No environment-variable override is used to redirect that shared machinery at the dedicated database either
    /// - xunit runs different <c>[Collection]</c>s in parallel by default (same reasoning as every sibling test
    /// class's own remarks), so mutating the process-wide <c>ConnectionStrings__TightWikiEfCore</c> environment
    /// variable here could race a concurrently-running collection's first (cached-forever) construction of that
    /// shared fixture and permanently misdirect it at this test's disposable database instead. Building a fresh
    /// <see cref="IConfiguration"/> via <see cref="ConfigurationBuilder.AddInMemoryCollection"/> avoids that
    /// entirely - see <see cref="BuildFreshConfiguration"/>. The extra wiring needed beyond that (a minimal
    /// <see cref="UserManager{TUser}"/> service provider) is thin, self-contained glue - not a duplication of
    /// <see cref="ITwDatabaseManager"/>'s own bootstrap/seed logic, which is exercised for real via the actual
    /// <c>SqlServerDatabaseManager</c>/<c>PostgresDatabaseManager</c> classes exactly like every other test file.
    /// </para>
    /// <para>
    /// No <see cref="IClassFixture{TFixture}"/>/shared fixture is used at all (unlike every sibling class) -
    /// deliberately, since the whole point is a database this class alone creates and destroys.
    /// </para>
    /// </remarks>
    [Collection("Fresh Database And Idempotence Tests")]
    public class FreshDatabaseAndIdempotenceTests
    {
        /// <summary>
        /// The 8 schema/database names every provider's <see cref="ITwDatabaseManager.GetDatabaseVersions"/>/
        /// <see cref="ITwDatabaseManager.GetDatabasePageCounts"/> reports one row per - same list
        /// <see cref="SpannedRepositoryAdminOperationsTests"/> uses against the shared database, reused here to
        /// confirm the freshly bootstrapped/freshly copied database has the same structure.
        /// </summary>
        private static readonly string[] ExpectedSchemaNames =
        [
            "DeletedPageRevisions", "DeletedPages", "Pages", "Statistics", "Emoji", "Logging", "Users", "Config"
        ];

#if SQLSERVER_PROVIDER || POSTGRES_PROVIDER

        /// <summary>
        /// This test's own dedicated, disposable database name - deliberately distinct from
        /// <c>TightWikiTest</c>/<c>tightwiki_test</c> (the shared database every other SqlServer/Postgres test
        /// file relies on, see this class's own remarks) even though it lives in the very same Docker container
        /// (<c>tightwiki-test-mssql</c>/<c>tightwiki-test-postgres</c>, <c>Start-TestDatabases.ps1</c>).
        /// </summary>
#if SQLSERVER_PROVIDER
        private const string FreshDatabaseName = "TightWikiFreshTest";
#elif POSTGRES_PROVIDER
        private const string FreshDatabaseName = "tightwiki_fresh_test";
#endif

        /// <summary>
        /// The exact <see cref="TwDefaultDataType"/> flag set <c>Program.cs</c>'s post-<c>Build()</c>
        /// <c>ApplyAllSeedData</c> call passes on the SqlServer/Postgres builds (the one that actually seeds wiki
        /// pages - see <c>SqlServerDatabaseManager.SeedContentDataAsync</c>'s own doc comment for why), including
        /// <see cref="TwDefaultDataType.BuiltinPages"/> (SQLite needs no equivalent flag - those pages arrive for
        /// free via the shipped, pre-populated database files; MSSQL/Postgres have no such shortcut).
        /// </summary>
        private static readonly TwDefaultDataType[] AllDefaultDataTypes =
        [
            TwDefaultDataType.Themes,
            TwDefaultDataType.Configurations,
            TwDefaultDataType.FeatureTemplates,
            TwDefaultDataType.HelpPages,
            TwDefaultDataType.BuiltinPages,
            TwDefaultDataType.IncludePages,
            TwDefaultDataType.RootPages,
            TwDefaultDataType.SandboxPages,
        ];

        /// <summary>
        /// End-to-end fresh-empty-database smoke test plus idempotence check, combined into a single flow (rather
        /// than split across several <see cref="FactAttribute"/> methods) because every later step depends on the
        /// database state the previous step left behind - splitting these would mean either re-running the entire
        /// (expensive) migrate+seed sequence per test method, or sharing state across methods via instance fields,
        /// which xunit does not guarantee ordering for. Steps, mirroring this task's own numbered brief:
        /// <list type="number">
        /// <item><description>Ensure <see cref="FreshDatabaseName"/> does not exist (DROP DATABASE IF EXISTS,
        /// issued from this C# test code itself via <see cref="SqlConnection"/>/<see cref="NpgsqlConnection"/> -
        /// not a Bash shell command).</description></item>
        /// <item><description>Construct a real <c>SqlServerDatabaseManager</c>/<c>PostgresDatabaseManager</c>
        /// pointed at it and call <see cref="ITwDatabaseManager.InitializeSchema"/> - must report that it created
        /// the schema (the database did not exist a moment ago).</description></item>
        /// <item><description>Verify structure: <see cref="ITwDatabaseManager.GetDatabaseVersions"/>/
        /// <see cref="ITwDatabaseManager.GetDatabasePageCounts"/> report exactly the 8 expected schemas.</description></item>
        /// <item><description>Seed it (<see cref="ITwDatabaseManager.ApplyAllSeedData"/>, mirroring <c>Program.cs</c>'s
        /// own post-<c>Build()</c> call) and verify basic seed data: at least one configuration entry, at least one
        /// wiki page, and the admin account profile.</description></item>
        /// <item><description>Call <see cref="ITwDatabaseManager.InitializeSchema"/> a second time - must report no
        /// pending upgrade.</description></item>
        /// <item><description>Call <see cref="ITwDatabaseManager.ApplyAllSeedData"/> a second time, unconditionally
        /// (not gated on step 5's result, deliberately exercising the idempotent match-by-natural-key/MERGE
        /// behavior documented on that method itself) - must not throw and must not duplicate any row (config
        /// entry count and page count both unchanged from step 4).</description></item>
        /// <item><description><c>finally</c>: drop <see cref="FreshDatabaseName"/> again, so nothing is left
        /// behind for a subsequent run - unlike <c>TightWikiTest</c>/<c>tightwiki_test</c>, this database is never
        /// meant to persist between runs.</description></item>
        /// </list>
        /// </summary>
        [Fact]
        public async Task FreshDatabase_BootstrapsSchemaAndSeedData_AndIsIdempotentOnASecondRun()
        {
            var baseConfiguration = BuildBaseConfiguration();

            //Step 1.
            var freshConnectionString = await DropFreshDatabaseIfExistsAsync(baseConfiguration);

            try
            {
                var freshConfiguration = BuildFreshConfiguration(freshConnectionString);

#if SQLSERVER_PROVIDER
                ITwDatabaseManager databaseManager = new SqlServerDatabaseManager(freshConfiguration);
#elif POSTGRES_PROVIDER
                ITwDatabaseManager databaseManager = new PostgresDatabaseManager(freshConfiguration);
#endif

                //Step 2.
                var wasUpgradedFirstRun = await databaseManager.InitializeSchema();
                Assert.True(wasUpgradedFirstRun,
                    "Expected the very first InitializeSchema() call against a freshly dropped database to report that it created/upgraded the schema.");

                //Step 3.
                await AssertSchemaStructureIsPresent(databaseManager);

                //Step 4.
                await using var serviceProvider = BuildIdentityServiceProvider(freshConnectionString);
                var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
                var localizer = new TwVerbatimLocalizationText();

                //Mirrors Program.cs's post-Build() ApplyAllSeedData call. This test builds no
                //WikiConfigurationManager, so - unlike Program.cs/MockWikiEngineArtifacts - there is no pre-Build()
                //ordering constraint here forcing a split pre-/post- SeedContentDataAsync call: a single
                //ApplyAllSeedData call seeds everything, including wiki pages, in one shot (it internally creates
                //the admin user first, then delegates to SeedContentDataAsync - see SqlServerDatabaseManager's own
                //doc comment). tightEngine is deliberately null - it is only used to additionally refresh
                //search-token/tag metadata for seeded pages (SeedWikiPages's own doc comment), not needed to prove
                //the page rows themselves were created, and building a real ITwEngine here would mean duplicating
                //a large slice of MockWikiEngineArtifacts's own bootstrap that this class deliberately avoids (see
                //this class's own remarks).
                //
                //Deliberately does NOT also call ITwUsersRepository.ValidateEncryptionAndCreateAdminUser (unlike
                //MockWikiEngineArtifacts's own SQLSERVER_PROVIDER/POSTGRES_PROVIDER branch) - confirmed empirically
                //that it throws "Cache has not been initialized" here (EfUsersRepository.AdminPasswordStatus reads
                //TightWiki.Library.Caching.MemCache.Cache directly, which only WikiConfigurationManager's
                //constructor ever initializes - MemCache is static/process-wide, so calling MemCache.Initialize
                //from this test would race/disrupt every concurrently-running xunit collection's own already-
                //initialized cache, which this class's own remarks already rule out doing for the same reason via
                //environment variables). Not needed for this task's own definition of "structure + basic seed
                //data" (config entries, at least one page, the admin profile row ApplyAllSeedData's own
                //EnsureAdminUser already creates) - Administrator role membership (Bug 2,
                //BootstrapSeedRegressionTests) is out of scope here.
                await databaseManager.ApplyAllSeedData(localizer, userManager, null!, AllDefaultDataTypes);

                var configEntriesFirstRun = await databaseManager.ConfigurationRepository.GetFlatConfiguration();
                Assert.True(configEntriesFirstRun.Count > 0, "Expected at least one seeded configuration entry.");

                var pagesFirstRun = await databaseManager.PageRepository.GetAllPages();
                Assert.True(pagesFirstRun.Count > 0, "Expected at least one seeded wiki page.");

                var adminProfile = await databaseManager.UsersRepository.GetAccountProfileByNavigation(Constants.DEFAULTACCOUNT);
                Assert.NotNull(adminProfile);

                //Step 5.
                var wasUpgradedSecondRun = await databaseManager.InitializeSchema();
                Assert.False(wasUpgradedSecondRun,
                    "Expected a second InitializeSchema() call against an already-initialized database to report no pending upgrade.");

                //Step 6.
                await databaseManager.ApplyAllSeedData(localizer, userManager, null!, AllDefaultDataTypes);

                var configEntriesSecondRun = await databaseManager.ConfigurationRepository.GetFlatConfiguration();
                Assert.Equal(configEntriesFirstRun.Count, configEntriesSecondRun.Count);

                var pagesSecondRun = await databaseManager.PageRepository.GetAllPages();
                Assert.Equal(pagesFirstRun.Count, pagesSecondRun.Count);
            }
            finally
            {
                //Step 7.
                await DropFreshDatabaseIfExistsAsync(baseConfiguration);
            }
        }

        /// <summary>
        /// Reads the same configuration sources <see cref="TightWiki.Test.Library.MockWikiEngineArtifacts"/> does
        /// (shared <c>appsettings.json</c> + provider-specific <c>appsettings.Development.SqlServer.json</c>/
        /// <c>.Postgres.json</c> + environment variables, highest precedence last) so this test picks up the same
        /// server/port/credentials CI or a local run would use (<c>ConnectionStrings__TightWikiEfCore</c>) -
        /// only the database name is ever overridden from that (see <see cref="DropFreshDatabaseIfExistsAsync"/>/
        /// <see cref="BuildFreshConfiguration"/>), never the server/credentials themselves.
        /// </summary>
        private static IConfiguration BuildBaseConfiguration()
        {
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
#if SQLSERVER_PROVIDER
                .AddJsonFile("appsettings.Development.SqlServer.json", optional: true, reloadOnChange: true);
#elif POSTGRES_PROVIDER
                .AddJsonFile("appsettings.Development.Postgres.json", optional: true, reloadOnChange: true);
#endif
            configurationBuilder.AddEnvironmentVariables();
            return configurationBuilder.Build();
        }

        /// <summary>
        /// Builds a minimal, in-memory-only <see cref="IConfiguration"/> carrying nothing but
        /// <c>ConnectionStrings:TightWikiEfCore</c> pointed at <paramref name="freshConnectionString"/> - the only
        /// key <c>SqlServerDatabaseManager</c>/<c>PostgresDatabaseManager</c>'s constructor actually requires
        /// (<c>EventLogLevel</c> has its own <c>GetValue(..., "Information")</c> fallback). Deliberately not built
        /// via environment variables - see this class's own remarks for why.
        /// </summary>
        private static IConfiguration BuildFreshConfiguration(string freshConnectionString)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:TightWikiEfCore"] = freshConnectionString
                })
                .Build();
        }

        /// <summary>
        /// Builds a minimal <see cref="UserManager{TUser}"/> against <paramref name="freshConnectionString"/> -
        /// just enough DI to satisfy <see cref="ITwDatabaseManager.ApplyAllSeedData"/>'s
        /// <see cref="UserManager{TUser}"/> parameter (needed to create/find the admin Identity user), mirroring
        /// the second half of <see cref="TightWiki.Test.Library.MockWikiEngineArtifacts"/>'s own constructor
        /// without the Autofac/<c>ITwEngine</c>/<c>SignInManager</c> machinery this test does not need. Caller owns
        /// disposal (the returned <see cref="ServiceProvider"/> is <see cref="IAsyncDisposable"/>).
        /// </summary>
        private static ServiceProvider BuildIdentityServiceProvider(string freshConnectionString)
        {
            var services = new ServiceCollection();
            services.AddLogging();

#if SQLSERVER_PROVIDER
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(freshConnectionString,
                b => b.MigrationsAssembly("TightWiki.Data.EfCore.SqlServer")
                      .MigrationsHistoryTable(SqlServerMigrationsHistory.ApplicationDbTableName, SqlServerMigrationsHistory.ApplicationDbSchema)));
#elif POSTGRES_PROVIDER
            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(freshConnectionString,
                b => b.MigrationsAssembly("TightWiki.Data.EfCore.Postgres")
                      .MigrationsHistoryTable(PostgresMigrationsHistory.ApplicationDbTableName, PostgresMigrationsHistory.ApplicationDbSchema)));
#endif

            services.AddIdentity<IdentityUser, IdentityRole>()
                    .AddEntityFrameworkStores<ApplicationDbContext>()
                    .AddDefaultTokenProviders();

            return services.BuildServiceProvider();
        }

#if SQLSERVER_PROVIDER

        /// <summary>
        /// Ensures <see cref="FreshDatabaseName"/> does not exist on the <c>tightwiki-test-mssql</c> container,
        /// issued directly from this C# test code via <see cref="SqlConnection"/> against the <c>master</c>
        /// database (not a Bash shell command - see this task's own instruction on why that distinction matters).
        /// <c>SET SINGLE_USER WITH ROLLBACK IMMEDIATE</c> forcibly disconnects any lingering session (including
        /// this process's own pooled connections from an earlier phase of the same test run) before
        /// <c>DROP DATABASE</c>, which otherwise fails with "database is in use" if anything is still connected.
        /// Returns the connection string for <see cref="FreshDatabaseName"/> itself (derived from the same
        /// server/credentials as <paramref name="baseConfiguration"/>'s own <c>TightWikiEfCore</c> connection
        /// string), for the caller to use afterward.
        /// </summary>
        private static async Task<string> DropFreshDatabaseIfExistsAsync(IConfiguration baseConfiguration)
        {
            var baseConnectionString = baseConfiguration.GetConnectionString("TightWikiEfCore")
                ?? throw new InvalidOperationException(
                    "Missing connection string 'ConnectionStrings:TightWikiEfCore' - required to run against the tightwiki-test-mssql container (see Start-TestDatabases.ps1).");

            var adminConnectionString = new SqlConnectionStringBuilder(baseConnectionString) { InitialCatalog = "master" }.ConnectionString;
            var freshConnectionString = new SqlConnectionStringBuilder(baseConnectionString) { InitialCatalog = FreshDatabaseName }.ConnectionString;

            await using (var adminConnection = new SqlConnection(adminConnectionString))
            {
                await adminConnection.OpenAsync();
                await using var command = adminConnection.CreateCommand();
                command.CommandText = $"""
                    IF DB_ID(N'{FreshDatabaseName}') IS NOT NULL
                    BEGIN
                        ALTER DATABASE [{FreshDatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        DROP DATABASE [{FreshDatabaseName}];
                    END
                    """;
                await command.ExecuteNonQueryAsync();
            }

            //Scoped to just this test's own connection string - not SqlConnection.ClearAllPools() - so this never
            //disturbs pooled connections any concurrently-running xunit collection holds open against the shared
            //TightWikiTest database (xunit runs different [Collection]s in parallel by default, see this class's
            //own remarks).
            SqlConnection.ClearPool(new SqlConnection(freshConnectionString));

            return freshConnectionString;
        }

#elif POSTGRES_PROVIDER

        /// <summary>
        /// Ensures <see cref="FreshDatabaseName"/> does not exist on the <c>tightwiki-test-postgres</c> container,
        /// issued directly from this C# test code via <see cref="NpgsqlConnection"/> against the <c>postgres</c>
        /// database (not a Bash shell command - see this task's own instruction on why that distinction matters).
        /// <c>WITH (FORCE)</c> (PostgreSQL 13+ - the pinned <c>tightwiki-test-postgres</c> image is
        /// <c>postgres:17</c>, see <c>Start-TestDatabases.ps1</c>) disconnects any lingering session before
        /// dropping, same purpose as the SQL Server branch's <c>SET SINGLE_USER WITH ROLLBACK IMMEDIATE</c>.
        /// Returns the connection string for <see cref="FreshDatabaseName"/> itself (derived from the same
        /// server/credentials as <paramref name="baseConfiguration"/>'s own <c>TightWikiEfCore</c> connection
        /// string), for the caller to use afterward.
        /// </summary>
        private static async Task<string> DropFreshDatabaseIfExistsAsync(IConfiguration baseConfiguration)
        {
            var baseConnectionString = baseConfiguration.GetConnectionString("TightWikiEfCore")
                ?? throw new InvalidOperationException(
                    "Missing connection string 'ConnectionStrings:TightWikiEfCore' - required to run against the tightwiki-test-postgres container (see Start-TestDatabases.ps1).");

            var adminConnectionString = new NpgsqlConnectionStringBuilder(baseConnectionString) { Database = "postgres" }.ConnectionString;
            var freshConnectionString = new NpgsqlConnectionStringBuilder(baseConnectionString) { Database = FreshDatabaseName }.ConnectionString;

            await using (var adminConnection = new NpgsqlConnection(adminConnectionString))
            {
                await adminConnection.OpenAsync();
                await using var command = adminConnection.CreateCommand();
                command.CommandText = $"DROP DATABASE IF EXISTS {FreshDatabaseName} WITH (FORCE);";
                await command.ExecuteNonQueryAsync();
            }

            //Scoped cleanup, not NpgsqlConnection.ClearAllPools() - see the matching comment on the SQL Server branch.
            NpgsqlConnection.ClearPool(new NpgsqlConnection(freshConnectionString));

            return freshConnectionString;
        }

#endif

        /// <summary>
        /// Confirms all 8 expected schemas exist and are reachable (<see cref="ITwDatabaseManager.GetDatabaseVersions"/>/
        /// <see cref="ITwDatabaseManager.GetDatabasePageCounts"/>, same pair
        /// <see cref="SpannedRepositoryAdminOperationsTests"/> exercises against the shared database) - this is the
        /// "structure" half of this task's own brief, independent of whether any seed data has been written yet.
        /// </summary>
        private static async Task AssertSchemaStructureIsPresent(ITwDatabaseManager databaseManager)
        {
            var versions = await databaseManager.GetDatabaseVersions();
            Assert.Equal(
                ExpectedSchemaNames.OrderBy(n => n, StringComparer.Ordinal),
                versions.Select(v => v.Name).OrderBy(n => n, StringComparer.Ordinal));

            var pageCounts = await databaseManager.GetDatabasePageCounts();
            Assert.Equal(
                ExpectedSchemaNames.OrderBy(n => n, StringComparer.Ordinal),
                pageCounts.Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
        }

#elif SQLITE_PROVIDER

        /// <summary>
        /// SQLite's bootstrap model is fundamentally different from the two EF Core providers, so this is a
        /// deliberately adapted (not force-fit identical) equivalent - per this task's own explicit instruction.
        /// <see cref="TightWiki.Repository.Helpers.DatabaseManager.InitializeSchema"/>
        /// (<c>ApplyDatabaseUpgradeScripts</c>) only ever applies <i>incremental</i> upgrade scripts on top of a
        /// base schema that must already exist - the base schema and seed data for a brand new installation arrive
        /// as a byte-for-byte file copy of the shipped, pre-populated <c>Data\*.db</c> files (confirmed by reading
        /// both <c>DatabaseManager.CreateDefaultsDatabase</c>, which only ever extracts the embedded
        /// <c>defaults.db</c> seed-import template - not the 8 live databases themselves - and
        /// <c>TwEngineFixture</c>'s own constructor, which performs exactly that copy into the shared
        /// <c>bin\...\data\</c> directory every other SQLite test in this project relies on). So there is no "run a
        /// live migration against a database that does not exist yet" story to test here the way there is for SQL
        /// Server/Postgres (EF Core's <c>Migrate()</c> creating the database from nothing) - <c>InitializeSchema</c>
        /// on SQLite cannot create the 8 live databases' base schema by itself at all.
        /// <para>
        /// The closest still-meaningful equivalent, and what this test does: copy the same shipped files into a
        /// brand-new, never-before-used temp directory (deliberately not the shared <c>bin\...\data\</c> directory
        /// - this never touches what every other SQLite test in this project depends on) and confirm the resulting
        /// database is structurally complete, carries the expected seed data, and that
        /// <see cref="ITwDatabaseManager.InitializeSchema"/> is idempotent when run twice against it - the one part
        /// of the bootstrap sequence that <i>is</i> live/executable against a not-pre-baked target on SQLite too.
        /// </para>
        /// </summary>
        [Fact]
        public async Task FreshlyCopiedDatabase_InitializeSchemaIsIdempotent_AndStructureAndSeedDataArePresent()
        {
            var freshDirectory = Path.Combine(Path.GetTempPath(), $"TightWikiFreshDbTest_{Guid.NewGuid():N}");

            try
            {
                var originalDatabasePath = GetOriginalDatabasePath();
                Directory.CreateDirectory(freshDirectory);
                foreach (var file in Directory.GetFiles(originalDatabasePath))
                {
                    File.Copy(file, Path.Combine(freshDirectory, Path.GetFileName(file)), overwrite: true);
                }

                var configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DatabasePath"] = freshDirectory
                    })
                    .Build();

                var databaseManager = new DatabaseManager(configuration);

                //Idempotence: both calls must succeed, and the second must never (re-)report an upgrade, regardless
                //of what the first call found (the shipped Data\*.db files may or may not already be at the
                //current schema version - either way, only one of the two calls can legitimately apply an upgrade).
                var wasUpgradedFirstRun = await databaseManager.InitializeSchema();
                var wasUpgradedSecondRun = await databaseManager.InitializeSchema();
                Assert.False(wasUpgradedSecondRun,
                    $"Expected a second InitializeSchema() call against the same freshly-copied database to report no pending upgrade (first call reported wasUpgraded={wasUpgradedFirstRun}).");

                //Structure: all 8 schemas exist and are reachable in the freshly-copied directory.
                var versions = await databaseManager.GetDatabaseVersions();
                Assert.Equal(
                    ExpectedSchemaNames.OrderBy(n => n, StringComparer.Ordinal),
                    versions.Select(v => v.Name).OrderBy(n => n, StringComparer.Ordinal));

                var pageCounts = await databaseManager.GetDatabasePageCounts();
                Assert.Equal(
                    ExpectedSchemaNames.OrderBy(n => n, StringComparer.Ordinal),
                    pageCounts.Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));

                //Basic seed data: the shipped Data\*.db files already carry full seed data (unlike SQL
                //Server/Postgres, nothing further needs to run to populate them - see this test's own doc comment),
                //so this only confirms the freshly-copied files are actually readable/queryable end to end, not
                //corrupted by the copy.
                var configEntries = await databaseManager.ConfigurationRepository.GetFlatConfiguration();
                Assert.True(configEntries.Count > 0, "Expected at least one configuration entry in the freshly-copied database.");

                var pages = await databaseManager.PageRepository.GetAllPages();
                Assert.True(pages.Count > 0, "Expected at least one wiki page in the freshly-copied database.");
            }
            finally
            {
                //Microsoft.Data.Sqlite pools native SQLite connections by connection string even though every
                //SqliteManagedInstance query above closes/disposes its own SqliteConnection object (confirmed by
                //reading NTDLS.SqliteDapperWrapper's own doc comment on that class - one instance per query,
                //disposed after) - the underlying pooled file handle otherwise keeps config.db/pages.db/etc. open
                //past this method's own queries, which fails the recursive delete below with "process cannot
                //access the file" (confirmed empirically). ClearAllPools() (global, not scoped to just this
                //directory's 8 connection strings - Microsoft.Data.Sqlite's ClearPool(SqliteConnection) overload
                //would need one call per database file) only discards *idle* pooled connections, never ones
                //actively in use, so this cannot break a concurrently-running xunit collection's own SQLite
                //queries against the shared bin\...\data\ directory (xunit runs different [Collection]s in
                //parallel by default) - same reasoning as the SQL Server/Postgres branches' own pool-clearing
                //calls, just global here rather than scoped since Microsoft.Data.Sqlite's API only offers a
                //per-connection-string scoped overload, not a per-directory one.
                SqliteConnection.ClearAllPools();

                if (Directory.Exists(freshDirectory))
                {
                    Directory.Delete(freshDirectory, recursive: true);
                }
            }
        }

        /// <summary>
        /// Resolves <c>ConnectionStrings:OriginalDatabasePath</c> (<c>TightWiki.Tests\appsettings.json</c>) the
        /// same way <see cref="TwEngineFixture"/>'s own constructor does, independently - this test deliberately
        /// does not depend on <see cref="TwEngineFixture"/>/its cached, shared
        /// <see cref="TightWiki.Test.Library.MockWikiEngineArtifacts"/> singleton (see this class's own remarks).
        /// Left as a relative path exactly as <see cref="TwEngineFixture"/> itself uses it - resolved against
        /// <see cref="Directory.GetCurrentDirectory"/> by <see cref="Directory.GetFiles(string)"/>, not this
        /// method.
        /// </summary>
        private static string GetOriginalDatabasePath()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            return configuration.GetConnectionString("OriginalDatabasePath")
                ?? throw new InvalidOperationException("Missing connection string 'ConnectionStrings:OriginalDatabasePath'.");
        }

#endif
    }
}
