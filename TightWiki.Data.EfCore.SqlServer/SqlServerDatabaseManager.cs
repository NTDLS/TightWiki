using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;
using TightWiki.Data.EfCore.Repositories;
using TightWiki.Data.EfCore.Seeding;
using TightWiki.Library;
using TightWiki.Library.Security;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;
using TightWiki.Plugin.Models.Defaults;
using ConfigEntities = TightWiki.Data.EfCore.Entities.Config;
using EmojiEntities = TightWiki.Data.EfCore.Entities.Emoji;
using PagesEntities = TightWiki.Data.EfCore.Entities.Pages;
using StatisticsEntities = TightWiki.Data.EfCore.Entities.Statistics;
using UsersEntities = TightWiki.Data.EfCore.Entities.Users;

namespace TightWiki.Data.EfCore.SqlServer
{
    /// <summary>
    /// MSSQL/EF Core implementation of <see cref="ITwDatabaseManager"/> and <see cref="ISpannedRepository"/> - the
    /// second, build-time-selectable implementation alongside the SQLite <c>TightWiki.Repository.Helpers.DatabaseManager</c>
    /// (see Database-Providers-Plan.md chapter 4.2). Selected instead of the SQLite implementation by <c>Program.cs</c>
    /// when built with <c>-p:DataProvider=SqlServer</c> (<c>SQLSERVER_PROVIDER</c>).
    /// </summary>
    /// <remarks>
    /// <see cref="InitializeSchema"/>, its <c>ApplicationDbContext</c>/<c>TightWikiDbContext</c> migrations,
    /// <see cref="DefaultsRepository"/>, every <see cref="ISpannedRepository"/> member (phase 2a.4 - vendor-native
    /// MSSQL maintenance operations, Database-Providers-Plan.md chapter 4.4), <see cref="ConfigurationRepository"/>
    /// (phase 2a.6 - <see cref="EfConfigurationRepository"/>), <see cref="LoggingRepository"/> (phase 2a.7 -
    /// <see cref="EfLoggingRepository"/>), <see cref="EmojiRepository"/> (phase 2a.8 - <see cref="EfEmojiRepository"/>)
    /// and <see cref="StatisticsRepository"/> (phase 2a.9 - <see cref="EfStatisticsRepository"/>) - all
    /// provider-agnostic LINQ living in the shared <c>TightWiki.Data.EfCore</c> project rather than here, see each
    /// class's doc comment - are real. <see cref="PageRepository"/> (<see cref="EfPageRepository"/>) and
    /// <see cref="UsersRepository"/> (<see cref="EfUsersRepository"/>) followed the same move in phase 2b.1, still
    /// as <see cref="NotImplementedException"/> stubs (only the wiring moved in 2b.1); all 86 of
    /// <see cref="PageRepository"/>'s members were implemented for real across phases 2b.2-2b.8 (see
    /// <see cref="EfPageRepository"/>'s own doc comment for which). 12 of <see cref="UsersRepository"/>'s 51
    /// members (the role CRUD/membership category) were implemented for real in phase 2b.9 (see
    /// <see cref="EfUsersRepository"/>'s own doc comment for which) - the rest land across phases 2b.10-2b.13.
    /// </remarks>
    public class SqlServerDatabaseManager : ITwDatabaseManager, ISpannedRepository
    {
        /// <summary>
        /// See <see cref="ITwDatabaseManager.Logger"/>. A plain console logger until <see cref="LoggingRepository"/>
        /// (phase 2a.7 - <see cref="EfLoggingRepository"/>) is constructed, then promoted to
        /// <see cref="TightWiki.Library.DatabaseLogger"/> - same two-stage bootstrap as the SQLite
        /// <c>DatabaseManager</c> (<c>TightWiki.Repository/Helpers/DatabaseManager.cs</c>: "We expose this here
        /// because it is the earliest we can prop up a database logger").
        /// </summary>
        public ILogger Logger { get; private set; }

        public EfConfigurationRepository ConfigurationRepository { get; private set; }
        ITwConfigurationRepository ITwDatabaseManager.ConfigurationRepository => ConfigurationRepository;

        public EfDefaultsRepository DefaultsRepository { get; private set; }
        ITwDefaultsRepository ITwDatabaseManager.DefaultsRepository => DefaultsRepository;

        public EfEmojiRepository EmojiRepository { get; private set; }
        ITwEmojiRepository ITwDatabaseManager.EmojiRepository => EmojiRepository;

        public EfLoggingRepository LoggingRepository { get; private set; }
        ITwLoggingRepository ITwDatabaseManager.LoggingRepository => LoggingRepository;

        public EfPageRepository PageRepository { get; private set; }
        ITwPageRepository ITwDatabaseManager.PageRepository => PageRepository;

        public EfStatisticsRepository StatisticsRepository { get; private set; }
        ITwStatisticsRepository ITwDatabaseManager.StatisticsRepository => StatisticsRepository;

        public EfUsersRepository UsersRepository { get; private set; }
        ITwUsersRepository ITwDatabaseManager.UsersRepository => UsersRepository;

        /// <summary>
        /// The single new configuration key introduced for the EF Core providers (Database-Providers-Plan.md
        /// chapter 7, "Rozhodnuto" - "Connection string klíč: ConnectionStrings:TightWikiEfCore").
        /// </summary>
        private readonly string _connectionString;

        public SqlServerDatabaseManager(IConfiguration configuration)
        {
            Logger = new ConsoleLogger();

            _connectionString = configuration.GetConnectionString("TightWikiEfCore")
                ?? throw new InvalidOperationException(
                    "Missing connection string 'ConnectionStrings:TightWikiEfCore', which is required when built with -p:DataProvider=SqlServer.");

            ConfigurationRepository = new EfConfigurationRepository(CreateDbContext, CreateApplicationDbContext);
            DefaultsRepository = new EfDefaultsRepository();

            LoggingRepository = new EfLoggingRepository(CreateDbContext, ConfigurationRepository);

            //Same two-stage bootstrap as the SQLite DatabaseManager - see the doc comment on Logger.
            var minimumLogLevel = Enum.Parse<LogLevel>(configuration.GetValue("EventLogLevel", LogLevel.Information.ToString()));
            Logger = new TightWiki.Library.DatabaseLogger(LoggingRepository, minimumLogLevel);

            EmojiRepository = new EfEmojiRepository(CreateDbContext, ConfigurationRepository);
            PageRepository = new EfPageRepository(CreateDbContext, ConfigurationRepository);
            StatisticsRepository = new EfStatisticsRepository(CreateDbContext, ConfigurationRepository);
            UsersRepository = new EfUsersRepository(CreateDbContext, CreateApplicationDbContext, ConfigurationRepository);
        }

        /// <summary>
        /// Creates a new <see cref="TightWikiDbContext"/> configured against the MSSQL connection string. Callers
        /// are responsible for disposing the returned context.
        /// </summary>
        private TightWikiDbContext CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<TightWikiDbContext>();
            //See the matching comment in TightWikiDbContextFactory - migrations for this shared context live in
            //this driver project's assembly, not TightWiki.Data.EfCore's, and its MigrationsHistoryTable is
            //explicit and distinct from ApplicationDbContext's (SqlServerMigrationsHistory).
            optionsBuilder.UseSqlServer(_connectionString,
                b => b.MigrationsAssembly(typeof(SqlServerDatabaseManager).Assembly.GetName().Name)
                      .MigrationsHistoryTable(SqlServerMigrationsHistory.TightWikiDbTableName));
            return new TightWikiDbContext(optionsBuilder.Options);
        }

        /// <summary>
        /// Creates a new <see cref="ApplicationDbContext"/> (ASP.NET Core Identity, Database-Providers-Plan.md
        /// chapter 4.1.1) configured against the same MSSQL connection string as <see cref="CreateDbContext"/>.
        /// Callers are responsible for disposing the returned context.
        /// </summary>
        private ApplicationDbContext CreateApplicationDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            //See the matching comment in ApplicationDbContextFactory - migrations for this shared context live
            //in this driver project's assembly, not TightWiki.Library's, and its MigrationsHistoryTable is
            //explicit and distinct from TightWikiDbContext's (SqlServerMigrationsHistory).
            optionsBuilder.UseSqlServer(_connectionString,
                b => b.MigrationsAssembly(typeof(SqlServerDatabaseManager).Assembly.GetName().Name)
                      .MigrationsHistoryTable(SqlServerMigrationsHistory.ApplicationDbTableName, SqlServerMigrationsHistory.ApplicationDbSchema));
            return new ApplicationDbContext(optionsBuilder.Options);
        }

        /// <summary>
        /// EF Core implementation of <see cref="ITwDatabaseManager.InitializeSchema"/>: applies any pending EF Core
        /// migrations via <see cref="Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.MigrateAsync"/>
        /// (Database-Providers-Plan.md chapter 4.2).
        /// </summary>
        /// <remarks>
        /// <see cref="ApplicationDbContext"/> (Identity) migrations are applied first, then
        /// <see cref="TightWikiDbContext"/>'s - per chapter 4.1.1 ("Migrace ApplicationDbContext musí běžet před
        /// migracemi TightWiki modelu") and the open question in chapter 8. Note that in the EF model itself
        /// <c>Users.Profile.UserId</c> is <b>not</b> a declared foreign key against <c>AspNetUsers.Id</c> - see
        /// the remarks on <c>Entities.Users.Profile</c> - it is only a logical/matching-by-convention link,
        /// because the two tables live in separate <see cref="Microsoft.EntityFrameworkCore.DbContext"/>s and EF
        /// Core cannot declare a cross-context FK constraint. So this ordering is not enforced by any FK EF would
        /// otherwise fail to create; it is kept anyway both because the plan calls for it and because it is the
        /// only sane bootstrap order (an application with no Identity tables yet has no users to run as).
        /// </remarks>
        public async Task<bool> InitializeSchema()
        {
            using var identityContext = CreateApplicationDbContext();
            var identityPendingMigrations = await identityContext.Database.GetPendingMigrationsAsync();
            var wasIdentitySchemaUpgraded = identityPendingMigrations.Any();
            if (wasIdentitySchemaUpgraded)
            {
                await identityContext.Database.MigrateAsync();
            }

            using var context = CreateDbContext();
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            var wasWikiSchemaUpgraded = pendingMigrations.Any();
            if (wasWikiSchemaUpgraded)
            {
                await context.Database.MigrateAsync();
            }

            return wasIdentitySchemaUpgraded || wasWikiSchemaUpgraded;
        }

        /// <summary>
        /// EF Core implementation of <see cref="ITwDatabaseManager.ApplyAllSeedData"/> (Database-Providers-Plan.md
        /// chapter 4.6/phase 2a.5). Reads the provider-neutral content seed (<see cref="DefaultsRepository"/> over
        /// "Seed\tightwiki.seed.zip") and writes it directly into <see cref="TightWikiDbContext"/> via
        /// <c>Add</c>/<c>AddRange</c> + <c>SaveChangesAsync</c> - deliberately <b>not</b> through
        /// <see cref="ConfigurationRepository"/>/<see cref="PageRepository"/> (still <see cref="NotImplementedException"/>
        /// stubs until phases 2a.6-2a.9/2b), matching the task's explicit instruction to write straight to the
        /// shared EF model.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Phase 2a.10 split everything except <see cref="EnsureAdminUser"/> out into
        /// <see cref="SeedContentDataAsync"/> - this method is now a thin wrapper: ensure the admin user exists,
        /// then delegate. It still exists (rather than every caller using the split halves directly) because it is
        /// the <see cref="ITwDatabaseManager"/> interface member, whose signature could not change (see
        /// <see cref="SeedContentDataAsync"/>'s doc comment for why the split exists and how the two calls in
        /// <c>Program.cs</c> divide the work).
        /// </para>
        /// <para>
        /// Mirrors the SQLite reference (<c>TightWiki.Repository.Helpers.DatabaseManager.ApplyAllSeedData</c>) in
        /// structure and in which <paramref name="defaultDataTypes"/> flag gates which section - Configurations,
        /// Themes, {Help,Include,Builtin}Pages, FeatureTemplates. Config.MenuItem and the whole Emoji schema have
        /// no corresponding <see cref="TwDefaultDataType"/> flag and are seeded unconditionally instead: on
        /// SQLite these tables are never populated through this method at all - they arrive "for free" via a
        /// full copy of the shipped, pre-populated Data\config.db/emoji.db files (see
        /// <see cref="DefaultsRepository"/>'s and <c>DefaultsRepository.GetDefaultEmojis</c>'s doc comments) - but
        /// MSSQL has no such file-copy shortcut, so this is the only path that ever populates them.
        /// </para>
        /// <para>
        /// Idempotency mirrors the SQLite reference's "MERGE ... ON CONFLICT DO UPDATE" scripts
        /// (Scripts\Defaults\Merge\*.sql): each row is looked up by its natural key first, existing rows are
        /// updated in place (except <c>ConfigurationEntry.Value</c>, deliberately preserved on conflict so a
        /// re-run never clobbers an administrator's tuned setting - see <c>MergeConfigurationEntry.sql</c> and
        /// <see cref="SeedConfigurations"/>), and missing rows are inserted. In practice this only matters if
        /// this method (or <see cref="SeedContentDataAsync"/>) is ever invoked more than once against the same
        /// database - both callers (<c>Program.cs</c>) gate on <see cref="InitializeSchema"/> having just
        /// performed the very first migration.
        /// </para>
        /// <para>
        /// Bootstrapping the admin account needed for <see cref="Page.CreatedByUserId"/>/<c>ModifiedByUserId</c>
        /// (<see cref="EnsureAdminUser"/>) talks to <paramref name="userManager"/> (real ASP.NET Core Identity,
        /// phase 2a.1) and writes a matching <see cref="Users.Profile"/> row directly - not through
        /// <c>ITwUsersRepository.CreateProfile</c>/<c>UpsertUserClaims</c> like the SQLite reference does, since
        /// that repository remains a stub until phase 2b. Claims (first/last name) are therefore not seeded here;
        /// that is purely cosmetic and out of scope for seeding wiki page ownership.
        /// </para>
        /// </remarks>
        public async Task ApplyAllSeedData(ITwSharedLocalizationText localizer, UserManager<IdentityUser> userManager,
            ITwEngine tightEngine, TwDefaultDataType[] defaultDataTypes)
        {
            using (var context = CreateDbContext())
            {
                await EnsureAdminUser(context, userManager);
            }

            await SeedContentDataAsync(defaultDataTypes, tightEngine, localizer);
        }

        /// <summary>
        /// The DI-free half of <see cref="ApplyAllSeedData"/> (Database-Providers-Plan.md phase 2a.10): seeds
        /// everything <see cref="ApplyAllSeedData"/> does except <see cref="EnsureAdminUser"/> - i.e.
        /// <see cref="SeedConfigurations"/>, <see cref="SeedThemes"/>, <see cref="SeedWikiPages"/>,
        /// <see cref="SeedFeatureTemplates"/>, <see cref="SeedMenuItems"/>, <see cref="SeedEmojiAndCategories"/>.
        /// Takes only what it needs to open a <see cref="TightWikiDbContext"/> (nothing - it reuses
        /// <see cref="CreateDbContext"/> like every other method here) and <paramref name="defaultDataTypes"/>;
        /// deliberately does <b>not</b> take a <see cref="UserManager{TUser}"/>, so it is safe to call before
        /// ASP.NET Core's DI container exists.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Exists because <c>Program.cs</c> constructs <c>WikiConfigurationManager</c> before
        /// <c>WebApplicationBuilder.Build()</c>, and that constructor eagerly reads Config.Theme
        /// (<c>WikiConfigurationManager.ReloadAll</c>: <c>.Single(o => o.Name == themeName)</c>) - which is empty
        /// on a freshly migrated-but-unseeded MSSQL database (unlike SQLite, where Config.Theme ships pre-seeded
        /// inside the shipped <c>config.db</c> file), crashing the app with "Sequence contains no matching
        /// element" before Kestrel ever starts listening. <c>Program.cs</c> calls this method directly (via an
        /// explicit cast to <see cref="SqlServerDatabaseManager"/> - it is intentionally not part of
        /// <see cref="ITwDatabaseManager"/>) right after <see cref="InitializeSchema"/>, gated on the same
        /// "was the schema just upgraded" condition as the later <see cref="ApplyAllSeedData"/> call, specifically
        /// so Config.Theme (and the rest of this method's sections) exist by the time
        /// <c>WikiConfigurationManager</c> is constructed.
        /// </para>
        /// <para>
        /// Because no <see cref="UserManager{TUser}"/> is available yet at that point in <c>Program.cs</c>, this
        /// method cannot create the admin user itself. Instead of skipping <see cref="SeedWikiPages"/> outright,
        /// it looks up whatever admin <see cref="Users.Profile"/> row already exists (there is none on the very
        /// first, pre-<c>Build()</c> call) and only seeds wiki pages if one is found. The later, post-<c>Build()</c>
        /// call to <see cref="ApplyAllSeedData"/> (<c>Program.cs</c>, inside <c>app.Services.CreateScope()</c>)
        /// runs <see cref="EnsureAdminUser"/> first and then calls this method again - by then the admin profile
        /// exists, so that second call is what actually seeds wiki pages; every other section it repeats
        /// (Configurations, Themes, FeatureTemplates, MenuItems, Emoji) is a no-op the second time, per this
        /// method's idempotency (see <see cref="ApplyAllSeedData"/>'s remarks).
        /// </para>
        /// </remarks>
        public async Task SeedContentDataAsync(TwDefaultDataType[] defaultDataTypes, ITwEngine? tightEngine = null, ITwSharedLocalizationText? localizer = null)
        {
            using var context = CreateDbContext();

            if (defaultDataTypes.Contains(TwDefaultDataType.Configurations))
            {
                await SeedConfigurations(context);
            }

            if (defaultDataTypes.Contains(TwDefaultDataType.Themes))
            {
                await SeedThemes(context);
            }

            var adminProfile = await context.Profiles.FirstOrDefaultAsync(p => p.AccountName == "admin");
            if (adminProfile != null)
            {
                var namespaces = new List<string>();
                if (defaultDataTypes.Contains(TwDefaultDataType.HelpPages)) namespaces.Add("Wiki Help");
                if (defaultDataTypes.Contains(TwDefaultDataType.IncludePages)) namespaces.Add("Include");
                if (defaultDataTypes.Contains(TwDefaultDataType.BuiltinPages)) namespaces.Add("Builtin");
                if (defaultDataTypes.Contains(TwDefaultDataType.RootPages)) namespaces.Add("");
                if (defaultDataTypes.Contains(TwDefaultDataType.SandboxPages)) namespaces.Add("Sandbox");

                if (namespaces.Count > 0)
                {
                    await SeedWikiPages(context, namespaces, adminProfile.UserId, tightEngine, localizer);
                }
            }

            if (defaultDataTypes.Contains(TwDefaultDataType.FeatureTemplates))
            {
                await SeedFeatureTemplates(context);
            }

            //No TwDefaultDataType flag exists for these three - see the remarks on ApplyAllSeedData above on why
            //they are always seeded rather than gated.
            await SeedMenuItems(context);
            await SeedEmojiAndCategories(context);
        }

        /// <summary>
        /// Finds or creates the built-in admin <see cref="IdentityUser"/> (looked up/created by <see
        /// cref="Constants.DEFAULTUSERNAME"/>, not the literal string <c>"admin"</c> - see remarks) together with
        /// its matching <see cref="Users.Profile"/> row (still keyed on the literal <c>"admin"</c>
        /// <see cref="Users.Profile.AccountName"/>/<see cref="Users.Profile.Navigation"/>, which is an independent,
        /// TightWiki-owned value unrelated to the Identity username), so that <see cref="SeedWikiPages"/> has a
        /// valid <see cref="Page.CreatedByUserId"/>/<c>ModifiedByUserId</c>. Returns null (logging the failure) if
        /// no admin user could be found or created, in which case the caller skips wiki page seeding entirely -
        /// same fallback behavior as the SQLite reference.
        /// </summary>
        /// <remarks>
        /// The SQLite reference's inline bootstrap (<c>DatabaseManager.ApplyAllSeedData</c>) finds/creates the
        /// Identity user by the literal username <c>"admin"</c> instead. This intentionally diverges from that:
        /// this method runs (via <see cref="ApplyAllSeedData"/>) immediately before
        /// <c>EfUsersRepository.ValidateEncryptionAndCreateAdminUserAsync</c> - called moments later from the same
        /// <c>Program.cs</c> DI scope - which independently finds/creates the Identity user by
        /// <see cref="Constants.DEFAULTUSERNAME"/> and then re-points the existing <c>Profile</c> row (keyed on
        /// <see cref="Constants.DEFAULTACCOUNT"/>) at whatever id it resolved via <c>SetProfileUserId</c>. Under
        /// SQL Server, looking this method's user up by the literal <c>"admin"</c> username would create a second,
        /// distinct <see cref="IdentityUser"/> from the one <c>ValidateEncryptionAndCreateAdminUserAsync</c> later
        /// finds/creates under <see cref="Constants.DEFAULTUSERNAME"/>, and re-pointing <c>Profile.UserId</c> from
        /// this method's id to that one would violate the FK from every already-seeded
        /// <see cref="Page.CreatedByUserId"/>/<c>ModifiedByUserId</c>/<c>PageRevision.ModifiedByUserId</c> (and
        /// every other column referencing <c>Profile.UserId</c> across the <c>Pages</c>/<c>DeletedPages</c>/
        /// <c>DeletedPageRevisions</c>/<c>Users</c> schemas) - an UPDATE of a primary key still referenced by
        /// existing FK rows. Looking this method's user up by <see cref="Constants.DEFAULTUSERNAME"/> instead means
        /// <c>ValidateEncryptionAndCreateAdminUserAsync</c>'s later <c>FindByNameAsync</c> finds the very same
        /// Identity user this method already created, so its subsequent <c>SetProfileUserId</c> call is a no-op
        /// UPDATE (new value == existing value) rather than a genuine repoint, and no FK violation occurs. This has
        /// no SQLite-side equivalent to keep in sync with: the SQLite reference matches the admin
        /// <see cref="Users.Profile"/> row by <c>Navigation</c>/<c>AccountName</c> (<c>GetAdminUserId.sql</c>), not
        /// by Identity username, so it never re-points an existing profile's primary key the way the MSSQL/EF path
        /// does.
        /// </remarks>
        private async Task<Guid?> EnsureAdminUser(TightWikiDbContext context, UserManager<IdentityUser> userManager)
        {
            try
            {
                Guid adminUserId;

                var existingUser = await userManager.FindByNameAsync(Constants.DEFAULTUSERNAME);
                if (existingUser != null)
                {
                    adminUserId = Guid.Parse(existingUser.Id);
                }
                else
                {
                    var user = new IdentityUser { UserName = Constants.DEFAULTUSERNAME };
                    var result = await userManager.CreateAsync(user, PasswordGenerator.Generate(32));
                    if (!result.Succeeded)
                    {
                        Logger.LogError("Could not create the default admin user for seeding default wiki pages: {Errors}",
                            string.Join("; ", result.Errors.Select(e => e.Description)));
                        return null;
                    }

                    adminUserId = Guid.Parse(await userManager.GetUserIdAsync(user));
                }

                if (await context.Profiles.FindAsync(adminUserId) == null)
                {
                    var now = DateTime.UtcNow;
                    context.Profiles.Add(new UsersEntities.Profile
                    {
                        UserId = adminUserId,
                        AccountName = "admin",
                        Navigation = TwNavigation.Clean("admin"),
                        CreatedDate = now,
                        ModifiedDate = now,
                    });
                    await context.SaveChangesAsync();
                }

                return adminUserId;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while ensuring the existence of an admin user for seeding "
                    + "default wiki pages. Default wiki page seeding will be skipped.");
                return null;
            }
        }

        /// <summary>
        /// Shared helper for every default-data seed method that needs to preserve the seed package's own
        /// primary key values (<see cref="SeedConfigurations"/> for Config.ConfigurationGroup/ConfigurationEntry,
        /// <see cref="SeedMenuItems"/> for Config.MenuItem, <see cref="SeedEmojiAndCategories"/> for
        /// Emoji.Emoji/EmojiCategory, <see cref="SeedWikiPages"/> for Pages.Page) rather than letting SQL Server's
        /// identity column generate new ones - so that e.g. <c>ConfigurationGroup.Id</c>/<c>Page.Id</c> stay 1:1
        /// with the SQLite reference (Data\config.db / Data\emoji.db / Data\pages.db). Written generically
        /// (schema/table name + an arbitrary insert/update callback) so any future seed method needing the same
        /// treatment can reuse it too.
        /// </summary>
        /// <param name="context">The context <paramref name="insertAction"/> will call <c>SaveChangesAsync</c> on.</param>
        /// <param name="schema">The schema of the table being seeded, e.g. "Config".</param>
        /// <param name="table">The table being seeded, e.g. "ConfigurationGroup".</param>
        /// <param name="insertAction">
        /// Adds/updates entities (with explicit, non-default <c>Id</c> values on any newly-added ones) against
        /// <paramref name="context"/> and calls <c>SaveChangesAsync</c> - run with <c>SET IDENTITY_INSERT</c>
        /// enabled for <paramref name="table"/> so that those explicit <c>Id</c> values are actually written
        /// instead of being overwritten by a database-generated one.
        /// </param>
        /// <remarks>
        /// <c>SET IDENTITY_INSERT ... ON/OFF</c> must run on the exact same connection as the insert it wraps, so
        /// this opens (and, unless the caller already had one open, closes) an explicit ambient connection around
        /// <paramref name="insertAction"/> via <c>Database.OpenConnectionAsync</c>/<c>Database.CloseConnectionAsync</c>
        /// - same pattern as <see cref="OpenAdminConnection"/>.
        /// Once <paramref name="insertAction"/> completes and IDENTITY_INSERT is switched back off, the identity
        /// counter is reseeded to the table's current <c>MAX(Id)</c> (<c>DBCC CHECKIDENT ... RESEED</c>) so that
        /// the next ordinary, database-generated insert continues after the highest explicit value just written,
        /// rather than colliding with it.
        /// </remarks>
        private static async Task SeedWithExplicitIdentityAsync(
            TightWikiDbContext context, string schema, string table, Func<Task> insertAction)
        {
            var qualifiedTable = $"[{schema}].[{table}]";

            var connection = context.Database.GetDbConnection();
            var connectionWasClosed = connection.State != ConnectionState.Open;
            if (connectionWasClosed)
            {
                await context.Database.OpenConnectionAsync();
            }

            //Table/schema names can't be parameterized in T-SQL, and qualifiedTable is always built from this
            //method's own hardcoded caller-supplied literals ("Config"/"ConfigurationGroup" etc. - never
            //external/user input), so there is nothing here for the EF1002 SQL-injection analyzer to actually
            //protect against - suppressed per its own suggested "make sure the value is sanitized and suppress
            //the warning" (same reasoning/pattern as EfPageRepository.SaveChangesWithIdentityInsertAsync).
#pragma warning disable EF1002 // Possible SQL injection vulnerability.
            try
            {
                await context.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT {qualifiedTable} ON");
                try
                {
                    await insertAction();
                }
                finally
                {
                    await context.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT {qualifiedTable} OFF");
                }

                using var command = connection.CreateCommand();
                command.CommandText = $"SELECT ISNULL(MAX([Id]), 0) FROM {qualifiedTable}";
                var maxId = (int)(await command.ExecuteScalarAsync() ?? 0);
                await context.Database.ExecuteSqlRawAsync($"DBCC CHECKIDENT ('{qualifiedTable}', RESEED, {maxId})");
            }
            finally
            {
                if (connectionWasClosed)
                {
                    await context.Database.CloseConnectionAsync();
                }
            }
#pragma warning restore EF1002
        }

        /// <summary>
        /// Seeds Config.ConfigurationGroup and Config.ConfigurationEntry from <see cref="DefaultsRepository"/>.
        /// Mirrors MergeConfigurationGroup.sql/MergeConfigurationEntry.sql: existing rows (matched by their
        /// natural key) are updated in place rather than duplicated, except that an existing entry's
        /// <c>Value</c> is deliberately left untouched (see <see cref="ApplyAllSeedData"/>'s remarks). Newly
        /// inserted rows carry the seed package's own <see cref="TwDefaultConfiguration.ConfigurationGroupId"/>/
        /// <see cref="TwDefaultConfiguration.ConfigurationEntryId"/> instead of letting SQL Server generate a new
        /// one, via <see cref="SeedWithExplicitIdentityAsync"/>, so that <c>ConfigurationGroup.Id</c>/
        /// <c>ConfigurationEntry.Id</c> stay 1:1 with the SQLite reference (Data\config.db).
        /// </summary>
        private async Task SeedConfigurations(TightWikiDbContext context)
        {
            var defaultGroups = await DefaultsRepository.GetDefaultConfigurationGroups();
            var existingGroups = await context.ConfigurationGroups.ToDictionaryAsync(g => g.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var defaultGroup in defaultGroups)
            {
                if (existingGroups.TryGetValue(defaultGroup.ConfigurationGroupName, out var existingGroup))
                {
                    existingGroup.Description = defaultGroup.ConfigurationGroupDescription;
                }
                else
                {
                    var newGroup = new ConfigEntities.ConfigurationGroup
                    {
                        Id = defaultGroup.ConfigurationGroupId,
                        Name = defaultGroup.ConfigurationGroupName,
                        Description = defaultGroup.ConfigurationGroupDescription,
                    };
                    context.ConfigurationGroups.Add(newGroup);
                    existingGroups[defaultGroup.ConfigurationGroupName] = newGroup;
                }
            }

            //Need every group's Id before entries below can reference it - already known up front for newly
            //inserted groups (set above from the seed package), this just persists the rows.
            await SeedWithExplicitIdentityAsync(context, "Config", "ConfigurationGroup", () => context.SaveChangesAsync());

            var defaultEntries = await DefaultsRepository.GetDefaultConfigurations();
            var existingEntries = await context.ConfigurationEntries
                .ToDictionaryAsync(e => (e.ConfigurationGroupId, e.Name.ToUpperInvariant()));

            foreach (var defaultEntry in defaultEntries)
            {
                if (!existingGroups.TryGetValue(defaultEntry.ConfigurationGroupName, out var group))
                {
                    Logger.LogWarning("Skipped seeding configuration entry '{Entry}' - its configuration group "
                        + "'{Group}' was not found.", defaultEntry.ConfigurationEntryName, defaultEntry.ConfigurationGroupName);
                    continue;
                }

                var entryKey = (group.Id, defaultEntry.ConfigurationEntryName.ToUpperInvariant());
                if (existingEntries.TryGetValue(entryKey, out var existingEntry))
                {
                    //Value is intentionally not overwritten here - see MergeConfigurationEntry.sql / the remarks
                    //on ApplyAllSeedData.
                    existingEntry.Name = defaultEntry.ConfigurationEntryName;
                    existingEntry.DataTypeId = defaultEntry.DataTypeId;
                    existingEntry.Description = defaultEntry.ConfigurationEntryDescription;
                    existingEntry.IsEncrypted = defaultEntry.IsEncrypted;
                    existingEntry.IsRequired = defaultEntry.IsRequired;
                }
                else
                {
                    var newEntry = new ConfigEntities.ConfigurationEntry
                    {
                        Id = defaultEntry.ConfigurationEntryId,
                        ConfigurationGroupId = group.Id,
                        Name = defaultEntry.ConfigurationEntryName,
                        Value = defaultEntry.Value,
                        DataTypeId = defaultEntry.DataTypeId,
                        Description = defaultEntry.ConfigurationEntryDescription,
                        IsEncrypted = defaultEntry.IsEncrypted,
                        IsRequired = defaultEntry.IsRequired,
                    };
                    context.ConfigurationEntries.Add(newEntry);
                    existingEntries[entryKey] = newEntry;
                }
            }

            await SeedWithExplicitIdentityAsync(context, "Config", "ConfigurationEntry", () => context.SaveChangesAsync());
        }

        /// <summary>
        /// Seeds Config.Theme from <see cref="DefaultsRepository"/>. Mirrors MergeTheme.sql - existing rows
        /// (matched by <see cref="Entities.Config.Theme.Name"/>, the real primary key) are updated in place.
        /// </summary>
        private async Task SeedThemes(TightWikiDbContext context)
        {
            var defaultThemes = await DefaultsRepository.GetDefaultThemes();
            var existingThemes = await context.Themes.ToDictionaryAsync(t => t.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var defaultTheme in defaultThemes)
            {
                if (existingThemes.TryGetValue(defaultTheme.Name, out var existingTheme))
                {
                    existingTheme.DelimitedFiles = defaultTheme.DelimitedFiles;
                    existingTheme.ClassNavBar = defaultTheme.ClassNavBar;
                    existingTheme.ClassNavLink = defaultTheme.ClassNavLink;
                    existingTheme.ClassDropdown = defaultTheme.ClassDropdown;
                    existingTheme.ClassBranding = defaultTheme.ClassBranding;
                    existingTheme.EditorTheme = defaultTheme.EditorTheme;
                }
                else
                {
                    context.Themes.Add(new ConfigEntities.Theme
                    {
                        Name = defaultTheme.Name,
                        DelimitedFiles = defaultTheme.DelimitedFiles,
                        ClassNavBar = defaultTheme.ClassNavBar,
                        ClassNavLink = defaultTheme.ClassNavLink,
                        ClassDropdown = defaultTheme.ClassDropdown,
                        ClassBranding = defaultTheme.ClassBranding,
                        EditorTheme = defaultTheme.EditorTheme,
                    });
                }
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Seeds Pages.Page + Pages.PageRevision (revision 1 only) for the given default-wiki-page namespaces.
        /// Existing pages (matched by <see cref="Entities.Pages.Page.Navigation"/>) have their current revision's
        /// content overwritten in place rather than being duplicated or revision-bumped, mirroring the SQLite
        /// reference passing the existing page's Id into <c>UpsertPage</c> when one is found. Raw content only -
        /// derived search/tag/reference metadata is a separate pass, see the <paramref name="tightEngine"/>/
        /// <paramref name="localizer"/> remarks below.
        /// </summary>
        /// <remarks>
        /// Newly inserted Page rows carry the seed package's own <see cref="TwDefaultWikiPage.Id"/>/
        /// <see cref="TwDefaultWikiPage.CreatedDate"/>/<see cref="TwDefaultWikiPage.ModifiedDate"/> instead of
        /// letting SQL Server generate a new Id / stamping <c>DateTime.UtcNow</c>, via
        /// <see cref="SeedWithExplicitIdentityAsync"/>, so that <c>Page.Id</c>/<c>CreatedDate</c>/<c>ModifiedDate</c>
        /// stay 1:1 with the SQLite reference (Data\pages.db) - this matters for Id-order-dependent behavior such
        /// as "Similar"/"Backlinks"/"Related" page listings. Because the Id is known up front, the matching
        /// PageRevision row is added in the very same pass (no interim <c>SaveChangesAsync</c> to obtain a
        /// database-generated Id first) and both Page and PageRevision inserts/updates for every page in
        /// <paramref name="namespaces"/> are flushed together in a single <c>SaveChangesAsync</c> wrapped by
        /// <see cref="SeedWithExplicitIdentityAsync"/> - IDENTITY_INSERT only affects INSERTs against
        /// <c>Pages.Page</c>, so the updates below and the ordinary identity-generated <c>PageRevision.Id</c> are
        /// unaffected. Existing pages instead have <c>ModifiedDate</c> set to <see cref="TwDefaultWikiPage.ModifiedDate"/>
        /// (not <c>DateTime.UtcNow</c>) so that re-running the seed against an already-seeded database converges
        /// exactly to the reference state rather than drifting to "now" on every run; <c>CreatedDate</c> is left
        /// untouched on the existing-page path, same as the SQLite reference's merge behavior.
        /// </remarks>
        /// <param name="tightEngine">
        /// When non-null (the post-<c>Build()</c> <see cref="ApplyAllSeedData"/> call, which is the only one that
        /// ever reaches here - see <see cref="SeedContentDataAsync"/>'s remarks on why the pre-<c>Build()</c> call
        /// never seeds wiki pages at all), every page this call touches (inserted or updated) is immediately run
        /// through <see cref="PageRepository"/>'s own <see cref="EfPageRepository.RefreshPageMetadata"/> - the same
        /// method <see cref="EfPageRepository.UpsertPage"/> itself calls - to populate Pages.PageTag/
        /// PageProcessingInstruction/PageToken/PageReference from the page's <c>Body</c>. Without this, search
        /// (which reads PageToken), tag listings, and backlinks would be empty for every seeded page even though
        /// the pages themselves render fine - the raw insert above intentionally skips this (mirroring
        /// <c>UpsertPage</c>'s own two-step "save, then refresh metadata" split), so it has to happen here instead.
        /// Null on any call that lacks a built DI container to resolve <see cref="ITwEngine"/> from.
        /// </param>
        private async Task SeedWikiPages(TightWikiDbContext context, List<string> namespaces, Guid adminUserId,
            ITwEngine? tightEngine, ITwSharedLocalizationText? localizer)
        {
            var defaultPages = new List<TwDefaultWikiPage>();
            foreach (var namespaceName in namespaces)
            {
                defaultPages.AddRange(await DefaultsRepository.GetDefaultWikiPages(namespaceName));
            }

            var existingPages = await context.Pages_Pages.ToDictionaryAsync(p => p.Navigation, StringComparer.OrdinalIgnoreCase);
            var now = DateTime.UtcNow;

            foreach (var defaultPage in defaultPages)
            {
                if (existingPages.TryGetValue(defaultPage.Navigation, out var existingPage))
                {
                    existingPage.Name = defaultPage.Name;
                    existingPage.Namespace = defaultPage.Namespace;
                    existingPage.Description = defaultPage.Description;
                    existingPage.ModifiedByUserId = adminUserId;
                    existingPage.ModifiedDate = defaultPage.ModifiedDate;

                    var existingRevision = await context.Pages_PageRevisions.FindAsync(existingPage.Id, existingPage.Revision);
                    if (existingRevision != null)
                    {
                        existingRevision.Name = defaultPage.Name;
                        existingRevision.Namespace = defaultPage.Namespace;
                        existingRevision.Description = defaultPage.Description;
                        existingRevision.Body = defaultPage.Body;
                        existingRevision.ModifiedByUserId = adminUserId;
                        existingRevision.ModifiedDate = defaultPage.ModifiedDate;
                        existingRevision.DataHash = defaultPage.DataHash;
                    }
                }
                else
                {
                    var newPage = new PagesEntities.Page
                    {
                        Id = defaultPage.Id,
                        Name = defaultPage.Name,
                        Namespace = defaultPage.Namespace,
                        Navigation = defaultPage.Navigation,
                        Description = defaultPage.Description,
                        Revision = 1,
                        CreatedByUserId = adminUserId,
                        CreatedDate = defaultPage.CreatedDate,
                        ModifiedByUserId = adminUserId,
                        ModifiedDate = defaultPage.ModifiedDate,
                    };
                    context.Pages_Pages.Add(newPage);

                    context.Pages_PageRevisions.Add(new PagesEntities.PageRevision
                    {
                        PageId = defaultPage.Id,
                        Name = defaultPage.Name,
                        Namespace = defaultPage.Namespace,
                        Description = defaultPage.Description,
                        Body = defaultPage.Body,
                        Revision = 1,
                        ModifiedByUserId = adminUserId,
                        ModifiedDate = defaultPage.ModifiedDate,
                        DataHash = defaultPage.DataHash,
                    });

                    existingPages[defaultPage.Navigation] = newPage;
                }
            }

            await SeedWithExplicitIdentityAsync(context, "Pages", "Page", () => context.SaveChangesAsync());

            if (tightEngine != null && localizer != null)
            {
                foreach (var defaultPage in defaultPages)
                {
                    var seededPage = existingPages[defaultPage.Navigation];
                    var page = new TwPage
                    {
                        Id = seededPage.Id,
                        Name = defaultPage.Name,
                        Navigation = defaultPage.Navigation,
                        Description = defaultPage.Description,
                        Body = defaultPage.Body,
                        Revision = 1,
                        MostCurrentRevision = 1,
                        CreatedByUserId = adminUserId,
                        CreatedDate = now,
                        ModifiedByUserId = adminUserId,
                        ModifiedDate = now,
                    };
                    await PageRepository.RefreshPageMetadata(tightEngine, localizer, page);
                }
            }

            await SeedPageFileAttachments(context, namespaces, existingPages.Values, adminUserId);

            await SeedPageStatistics(context, existingPages.Values);
        }

        /// <summary>
        /// Seeds Pages.PageFile + Pages.PageFileRevision + Pages.PageRevisionAttachment (revision 1 only, matching
        /// <see cref="SeedWikiPages"/>'s own single-revision seeding) for whatever pages <see cref="SeedWikiPages"/>
        /// just seeded in <paramref name="namespaces"/> - e.g. the image embedded in the Home/Wiki About page
        /// bodies. Existing attachments (matched by (PageId, Navigation)) have their current revision's content
        /// overwritten in place, mirroring <see cref="SeedWikiPages"/>'s own update-in-place behavior for existing
        /// pages rather than duplicating/revision-bumping. An attachment whose page isn't in
        /// <paramref name="seededPages"/> (page namespace wasn't requested) is silently skipped, same tolerance as
        /// <see cref="SeedFeatureTemplates"/>'s unresolved <c>PageName</c> handling.
        /// </summary>
        private async Task SeedPageFileAttachments(TightWikiDbContext context, List<string> namespaces, IEnumerable<PagesEntities.Page> seededPages, Guid adminUserId)
        {
            var defaultAttachments = new List<TwDefaultPageFileAttachment>();
            foreach (var namespaceName in namespaces)
            {
                defaultAttachments.AddRange(await DefaultsRepository.GetDefaultPageFileAttachments(namespaceName));
            }

            if (defaultAttachments.Count == 0)
            {
                return;
            }

            var pageIdsByKey = seededPages.ToDictionary(p => $"{p.Name} {p.Namespace}", p => p.Id, StringComparer.OrdinalIgnoreCase);
            var existingFiles = await context.Pages_PageFiles.ToDictionaryAsync(f => $"{f.PageId} {f.Navigation}", StringComparer.OrdinalIgnoreCase);
            var now = DateTime.UtcNow;

            foreach (var defaultAttachment in defaultAttachments)
            {
                if (!pageIdsByKey.TryGetValue($"{defaultAttachment.PageName} {defaultAttachment.Namespace}", out var pageId))
                {
                    continue;
                }

                var dataHash = SecurityUtility.Crc32(defaultAttachment.Data);
                var fileKey = $"{pageId} {defaultAttachment.FileNavigation}";

                if (existingFiles.TryGetValue(fileKey, out var existingFile))
                {
                    var existingRevision = await context.Pages_PageFileRevisions.FindAsync(existingFile.Id, existingFile.Revision);
                    if (existingRevision != null)
                    {
                        existingRevision.ContentType = defaultAttachment.ContentType;
                        existingRevision.Size = defaultAttachment.Data.Length;
                        existingRevision.Data = defaultAttachment.Data;
                        existingRevision.DataHash = dataHash;
                        existingRevision.CreatedByUserId = adminUserId;
                        existingRevision.CreatedDate = now;
                    }
                }
                else
                {
                    var newFile = new PagesEntities.PageFile
                    {
                        PageId = pageId,
                        Name = defaultAttachment.FileName,
                        Navigation = defaultAttachment.FileNavigation,
                        Revision = 1,
                        CreatedDate = now,
                    };
                    context.Pages_PageFiles.Add(newFile);
                    await context.SaveChangesAsync(); //Need the generated Id - PageFileRevision.PageFileId is not a navigation.

                    context.Pages_PageFileRevisions.Add(new PagesEntities.PageFileRevision
                    {
                        PageFileId = newFile.Id,
                        ContentType = defaultAttachment.ContentType,
                        Size = defaultAttachment.Data.Length,
                        CreatedByUserId = adminUserId,
                        CreatedDate = now,
                        Data = defaultAttachment.Data,
                        Revision = 1,
                        DataHash = dataHash,
                    });

                    context.Pages_PageRevisionAttachments.Add(new PagesEntities.PageRevisionAttachment
                    {
                        PageId = pageId,
                        PageFileId = newFile.Id,
                        FileRevision = 1,
                        PageRevision = 1,
                    });

                    existingFiles[fileKey] = newFile;
                }
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Seeds Statistics.PageStatistics for whatever pages <see cref="SeedWikiPages"/> just seeded in its own
        /// call - <b>overwriting every column</b> to the reference values from
        /// <see cref="DefaultsRepository"/>.<see cref="EfDefaultsRepository.GetDefaultPageStatistics"/> (roughly 99
        /// of the 110 reference wiki pages - see <see cref="TwDefaultPageStatistic"/>'s own doc comment), and
        /// deleting any row for a seeded page that has none in the reference (the remaining ~11).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Must run <b>after</b> <see cref="SeedWikiPages"/>'s own <see cref="PageRepository.RefreshPageMetadata"/>
        /// loop (see the call site) - that loop's <c>wikifier.Transform</c> call triggers
        /// <c>TightWiki.Plugin.Default.EngineHandlers.HandleCompletion</c> (a
        /// <see cref="TightWiki.Plugin.Attributes.Handlers.TwCompletionPluginHandlerAttribute"/>-registered
        /// completion handler the wiki engine invokes on every transform, gated on
        /// <c>WikiConfiguration.RecordCompilationMetrics</c>, which defaults to enabled), which in turn calls
        /// <see cref="EfStatisticsRepository.MergePageCompilationStatistics"/> for <b>every</b> seeded page - not
        /// just the ~99 with a reference row - leaving generic, "just seeded" values
        /// (<c>TotalCompilationCount</c> incremented off of zero, <c>LastCompileDateTime</c> = now, etc.) behind
        /// for all 110 pages rather than the reference's 99/11 split. This method is the deliberate second pass
        /// that corrects that: it doesn't touch <see cref="EfPageRepository"/>/<see cref="EfStatisticsRepository"/>
        /// themselves (both stay generically correct for their actual runtime callers - real page
        /// views/compilations outside of seeding), it just re-asserts the reference values immediately afterward,
        /// the same "seed writes the authoritative values last" approach <see cref="SeedWikiPages"/> itself already
        /// takes for <c>Page.CreatedDate</c>/<c>ModifiedDate</c>.
        /// </para>
        /// <para>
        /// <see cref="EfStatisticsRepository.MergePageCompilationStatistics"/>/<c>IncrementPageViewCount</c> each
        /// open their own short-lived <see cref="TightWikiDbContext"/> (via <c>_createContext</c>) and are already
        /// long finished by the time this method runs (the whole <c>RefreshPageMetadata</c> loop above is awaited
        /// to completion first) - so there is no EF change-tracking conflict between this method's own updates
        /// (against <paramref name="context"/>, the instance <see cref="SeedWikiPages"/> has held open the whole
        /// call) and any row those methods touched earlier.
        /// </para>
        /// <para>
        /// Idempotent by construction: every column on a matched row is unconditionally overwritten with the
        /// reference value (not incremented/merged), so re-running the seed (which re-runs
        /// <see cref="PageRepository.RefreshPageMetadata"/> and therefore
        /// <see cref="EfStatisticsRepository.MergePageCompilationStatistics"/> again first) always converges back
        /// to the same reference state rather than drifting - unlike a real page compile, this never increments
        /// <c>TotalCompilationCount</c> relative to the reference value.
        /// </para>
        /// <para>
        /// <see cref="StatisticsEntities.PageStatistic.Id"/> is a plain surrogate key (the real natural/unique key
        /// is <see cref="StatisticsEntities.PageStatistic.PageId"/>, see that class's own doc comment) - newly
        /// inserted rows just let SQL Server generate it normally, no <see cref="SeedWithExplicitIdentityAsync"/>
        /// needed here (unlike <c>Page.Id</c>/<c>Emoji.Id</c>/etc., nothing outside this table ever references
        /// <c>PageStatistics.Id</c>).
        /// </para>
        /// </remarks>
        private async Task SeedPageStatistics(TightWikiDbContext context, IEnumerable<PagesEntities.Page> seededPages)
        {
            var defaultStatisticsByPageId = (await DefaultsRepository.GetDefaultPageStatistics())
                .ToDictionary(s => s.PageId);

            var seededPageIds = seededPages.Select(p => p.Id).ToHashSet();

            var existingStatistics = await context.PageStatistics
                .Where(ps => seededPageIds.Contains(ps.PageId))
                .ToDictionaryAsync(ps => ps.PageId);

            foreach (var pageId in seededPageIds)
            {
                if (defaultStatisticsByPageId.TryGetValue(pageId, out var defaultStatistic))
                {
                    if (existingStatistics.TryGetValue(pageId, out var existingStatistic))
                    {
                        ApplyDefaultPageStatistic(existingStatistic, defaultStatistic);
                    }
                    else
                    {
                        var newStatistic = new StatisticsEntities.PageStatistic { PageId = pageId };
                        ApplyDefaultPageStatistic(newStatistic, defaultStatistic);
                        context.PageStatistics.Add(newStatistic);
                    }
                }
                else if (existingStatistics.TryGetValue(pageId, out var staleStatistic))
                {
                    //RefreshPageMetadata left a generic row behind for a page that has no statistics history in
                    //the reference database (see this method's own doc comment) - remove it so the seeded
                    //database matches the reference's 99/11 split rather than having a row for all 110 pages.
                    context.PageStatistics.Remove(staleStatistic);
                }
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Overwrites every non-key column of <paramref name="target"/> with <paramref name="source"/>'s reference
        /// values - shared by both the "update existing row" and "populate newly inserted row" branches of
        /// <see cref="SeedPageStatistics"/>.
        /// </summary>
        private static void ApplyDefaultPageStatistic(StatisticsEntities.PageStatistic target, TwDefaultPageStatistic source)
        {
            target.LastCompileDateTime = source.LastCompileDateTime;
            target.TotalCompilationCount = source.TotalCompilationCount;
            target.LastWikifyTimeMs = source.LastWikifyTimeMs;
            target.TotalWikifyTimeMs = source.TotalWikifyTimeMs;
            target.LastMatchCount = source.LastMatchCount;
            target.LastErrorCount = source.LastErrorCount;
            target.LastOutgoingLinkCount = source.LastOutgoingLinkCount;
            target.LastTagCount = source.LastTagCount;
            target.LastProcessedBodySize = source.LastProcessedBodySize;
            target.LastBodySize = source.LastBodySize;
            target.TotalViewCount = source.TotalViewCount;
        }

        /// <summary>
        /// Seeds Pages.FeatureTemplate from <see cref="DefaultsRepository"/>. Mirrors MergeFeatureTemplate.sql:
        /// existing rows (matched by the composite (Name, Type) primary key) are updated in place, and
        /// <see cref="TwDefaultFeatureTemplate.PageName"/> is resolved against the just-seeded Pages.Page rows -
        /// left null (same as the SQL subquery returning no row) when no matching page exists.
        /// </summary>
        private async Task SeedFeatureTemplates(TightWikiDbContext context)
        {
            var defaultTemplates = await DefaultsRepository.GetDefaultFeatureTemplates();
            var existingTemplates = await context.FeatureTemplates.ToDictionaryAsync(t => (t.Name.ToUpperInvariant(), t.Type.ToUpperInvariant()));
            var pageIdsByName = await context.Pages_Pages.ToDictionaryAsync(p => p.Name, p => p.Id, StringComparer.OrdinalIgnoreCase);

            foreach (var defaultTemplate in defaultTemplates)
            {
                int? pageId = null;
                if (!string.IsNullOrEmpty(defaultTemplate.PageName) && pageIdsByName.TryGetValue(defaultTemplate.PageName, out var foundPageId))
                {
                    pageId = foundPageId;
                }

                var templateKey = (defaultTemplate.Name.ToUpperInvariant(), defaultTemplate.Type.ToUpperInvariant());
                if (existingTemplates.TryGetValue(templateKey, out var existingTemplate))
                {
                    existingTemplate.PageId = pageId;
                    existingTemplate.Description = defaultTemplate.Description;
                    existingTemplate.TemplateText = defaultTemplate.TemplateText;
                }
                else
                {
                    context.FeatureTemplates.Add(new PagesEntities.FeatureTemplate
                    {
                        Name = defaultTemplate.Name,
                        Type = defaultTemplate.Type,
                        PageId = pageId,
                        Description = defaultTemplate.Description,
                        TemplateText = defaultTemplate.TemplateText,
                    });
                }
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Seeds Config.MenuItem from <see cref="DefaultsRepository"/>. No SQLite merge script/natural unique
        /// constraint exists to mirror (SQLite never seeds this table - see
        /// <see cref="ITwDefaultsRepository.GetDefaultMenuItems"/>'s doc comment), so existing rows are matched
        /// by the (Name, Link) pair. Newly inserted rows carry the seed package's own <see cref="TwMenuItem.Id"/>
        /// instead of letting SQL Server generate a new one, via <see cref="SeedWithExplicitIdentityAsync"/>, so
        /// that <c>MenuItem.Id</c> stays 1:1 with the SQLite reference (Data\config.db).
        /// </summary>
        private async Task SeedMenuItems(TightWikiDbContext context)
        {
            var defaultMenuItems = await DefaultsRepository.GetDefaultMenuItems();
            var existingMenuItems = await context.MenuItems
                .ToDictionaryAsync(m => (m.Name.ToUpperInvariant(), m.Link.ToUpperInvariant()));

            foreach (var defaultMenuItem in defaultMenuItems)
            {
                var menuItemKey = (defaultMenuItem.Name.ToUpperInvariant(), defaultMenuItem.Link.ToUpperInvariant());
                if (existingMenuItems.TryGetValue(menuItemKey, out var existingMenuItem))
                {
                    existingMenuItem.Ordinal = defaultMenuItem.Ordinal;
                }
                else
                {
                    context.MenuItems.Add(new ConfigEntities.MenuItem
                    {
                        Id = defaultMenuItem.Id,
                        Name = defaultMenuItem.Name,
                        Link = defaultMenuItem.Link,
                        Ordinal = defaultMenuItem.Ordinal,
                    });
                }
            }

            await SeedWithExplicitIdentityAsync(context, "Config", "MenuItem", () => context.SaveChangesAsync());
        }

        /// <summary>
        /// Seeds Emoji.Emoji + Emoji.EmojiCategory from <see cref="DefaultsRepository"/>. Existing rows are
        /// matched by <see cref="EmojiEntities.Emoji.Name"/> (Emoji) / (EmojiId, Category) (EmojiCategory).
        /// </summary>
        /// <remarks>
        /// Two things this method has to do that no other seed helper here does:
        /// <list type="bullet">
        /// <item><description>Re-compress each image's bytes with GZip before writing them to
        /// <see cref="EmojiEntities.Emoji.ImageData"/> - the seed package stores emoji images uncompressed for
        /// diffability (see <see cref="EfDefaultsRepository"/>), but the runtime (<c>FileController.cs</c>,
        /// <see cref="Utility.Decompress"/>) always expects GZip-compressed bytes there, mirroring
        /// <see cref="Utility.Compress"/> (see Database-Providers-Plan.md chapter 4.6 / commit 7eb2c329).</description></item>
        /// <item><description>Track which emoji ids were actually seeded (<c>seededEmojiIds</c>), so an
        /// EmojiCategory row whose <see cref="TwDefaultEmojiCategory.EmojiId"/> doesn't resolve to any emoji can
        /// still be skipped with a warning instead of violating referential integrity.</description></item>
        /// </list>
        /// Newly inserted Emoji/EmojiCategory rows carry the seed package's own <see cref="TwDefaultEmoji.Id"/>/
        /// <see cref="TwDefaultEmojiCategory.Id"/> instead of letting SQL Server generate new ones, via
        /// <see cref="SeedWithExplicitIdentityAsync"/>, so that <c>Emoji.Id</c>/<c>EmojiCategory.Id</c> stay 1:1
        /// with the SQLite reference (Data\emoji.db) - <see cref="TwDefaultEmojiCategory.EmojiId"/> can therefore
        /// be used directly as the database <c>Emoji.Id</c> with no seed-id-to-database-id remapping needed.
        /// </remarks>
        private async Task SeedEmojiAndCategories(TightWikiDbContext context)
        {
            var defaultEmojis = await DefaultsRepository.GetDefaultEmojis();
            var existingEmojis = await context.Emojis.ToDictionaryAsync(e => e.Name, StringComparer.OrdinalIgnoreCase);

            var seededEmojiIds = new HashSet<int>();
            var hasNewEmojis = false;

            foreach (var defaultEmoji in defaultEmojis)
            {
                if (existingEmojis.TryGetValue(defaultEmoji.Name, out var existingEmoji))
                {
                    seededEmojiIds.Add(existingEmoji.Id);
                    continue;
                }

                var imageBytes = await DefaultsRepository.ReadEmojiImageBytes(defaultEmoji.ImageEntry);

                context.Emojis.Add(new EmojiEntities.Emoji
                {
                    Id = defaultEmoji.Id,
                    Name = defaultEmoji.Name,
                    MimeType = defaultEmoji.MimeType,
                    ImageData = Utility.Compress(imageBytes),
                });
                seededEmojiIds.Add(defaultEmoji.Id);
                hasNewEmojis = true;
            }

            if (hasNewEmojis)
            {
                //Need every new Emoji's Id persisted before EmojiCategory below can reference it.
                await SeedWithExplicitIdentityAsync(context, "Emoji", "Emoji", () => context.SaveChangesAsync());
            }

            var defaultCategories = await DefaultsRepository.GetDefaultEmojiCategories();
            var existingCategoryKeys = (await context.EmojiCategories.Select(c => new { c.EmojiId, c.Category }).ToListAsync())
                .Select(c => (c.EmojiId, c.Category.ToUpperInvariant()))
                .ToHashSet();

            foreach (var defaultCategory in defaultCategories)
            {
                if (!seededEmojiIds.Contains(defaultCategory.EmojiId))
                {
                    Logger.LogWarning("Skipped seeding emoji category '{Category}' - its emoji (id {EmojiId}) "
                        + "was not found.", defaultCategory.Category, defaultCategory.EmojiId);
                    continue;
                }

                var categoryKey = (defaultCategory.EmojiId, defaultCategory.Category.ToUpperInvariant());
                if (existingCategoryKeys.Add(categoryKey))
                {
                    context.EmojiCategories.Add(new EmojiEntities.EmojiCategory
                    {
                        Id = defaultCategory.Id,
                        EmojiId = defaultCategory.EmojiId,
                        Category = defaultCategory.Category,
                    });
                }
            }

            await SeedWithExplicitIdentityAsync(context, "Emoji", "EmojiCategory", () => context.SaveChangesAsync());
        }

        #region Database admin - ISpannedRepository / ITwDatabaseManager.

        /// <summary>
        /// The 8 schemas <see cref="TightWikiDbContext"/> spans (Database-Providers-Plan.md chapter 4.3), in the
        /// same order as the SQLite <c>DatabaseManager.Databases</c> array
        /// (<c>TightWiki.Repository/Helpers/DatabaseManager.cs</c>) that they replace one-for-one for the purposes
        /// of <see cref="GetDatabasePageCounts"/>/<see cref="GetDatabasePageSizes"/> (chapter 4.4, "Admin obrazovka
        /// „Database"" - "Doporučení: per schéma"). Does <b>not</b> include <c>ApplicationDbContext</c>'s own
        /// schema ("Users" is shared between the two contexts, see <see cref="SqlServerMigrationsHistory"/>) or the
        /// default "dbo" schema that only holds <see cref="TightWikiDbContext"/>'s migrations-history table.
        /// </summary>
        private static readonly string[] TargetSchemas =
        [
            "DeletedPageRevisions", "DeletedPages", "Pages", "Statistics", "Emoji", "Logging", "Users", "Config"
        ];

        /// <summary>
        /// SQL Server's on-disk page size. Unlike SQLite (where <c>PRAGMA page_size</c> is a per-file setting
        /// queried at runtime, see <c>GetDatabasePageSize.sql</c>), SQL Server's page size is a fixed architectural
        /// constant - 8 KB for every database, table, and index, on every supported edition and version - there is
        /// no per-schema or per-object override and nothing to query it from. See
        /// https://learn.microsoft.com/sql/relational-databases/pages-and-extents-architecture-guide.
        /// </summary>
        private const int SqlServerPageSizeBytes = 8192;

        /// <summary>
        /// Opens (and, via the <c>using</c> at each call site's <see langword="await using"/>, closes) a
        /// dedicated <see cref="TightWikiDbContext"/>-backed ADO.NET connection for the raw DBCC/DMV statements
        /// below - none of these are expressible as LINQ, and mixing raw ADO.NET access with EF Core's own
        /// connection management via <c>Database.OpenConnectionAsync</c>/<c>CloseConnectionAsync</c> (rather than
        /// opening the underlying <see cref="DbConnection"/> directly) is the documented-safe way to do that.
        /// </summary>
        private async Task<(TightWikiDbContext Context, DbConnection Connection)> OpenAdminConnection()
        {
            var context = CreateDbContext();
            await context.Database.OpenConnectionAsync();
            return (context, context.Database.GetDbConnection());
        }

        /// <summary>
        /// SQLite's <c>VACUUM</c> (<c>VacuumDatabase.sql</c>) rebuilds the entire file into a fresh copy,
        /// defragmenting storage and reclaiming free pages. SQL Server has no single-statement equivalent, so this
        /// implements the safer of the two realistic candidates:
        /// <list type="bullet">
        /// <item><description><c>ALTER INDEX ALL ... REBUILD</c> per table (chosen) - defragments and compacts
        /// storage in place, is safe to run online/regularly, and is what Microsoft's own maintenance guidance
        /// recommends for this purpose.</description></item>
        /// <item><description><c>DBCC SHRINKDATABASE</c> (deliberately <b>not</b> chosen) - physically returns
        /// free space to the OS, which sounds closer to SQLite's file-shrinking behavior, but Microsoft's own
        /// documentation recommends against it: it aggressively fragments every index in the database (the
        /// opposite of "optimized storage"), the reclaimed space is very likely to be needed again immediately
        /// (data files just grow back, which is itself an expensive operation), and it is explicitly discouraged
        /// as a routine/scheduled operation - see
        /// https://learn.microsoft.com/troubleshoot/sql/database-engine/database-file-operations/considerations-for-shrink-database-file.
        /// </description></item>
        /// </list>
        /// Runs across every table in <see cref="TargetSchemas"/> regardless of the <paramref name="databaseName"/>
        /// argument - after schema consolidation (chapter 4.3) there is exactly one physical MSSQL database, so
        /// there is nothing left for that argument to meaningfully select between; it is only still accepted to
        /// satisfy the shared <see cref="ISpannedRepository"/> signature that <c>AdminController</c> calls through.
        /// </summary>
        public async Task<string> VacuumDatabase(string databaseName)
        {
            var (context, connection) = await OpenAdminConnection();
            await using (context)
            {
                try
                {
                    var tables = new List<(string Schema, string Table)>();
                    var schemaList = string.Join(",", TargetSchemas.Select(s => $"'{s}'"));

                    using (var listCommand = connection.CreateCommand())
                    {
                        listCommand.CommandText = $"""
                            SELECT s.name AS SchemaName, t.name AS TableName
                            FROM sys.tables t
                            INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
                            WHERE t.is_ms_shipped = 0 AND s.name IN ({schemaList})
                            ORDER BY s.name, t.name;
                            """;
                        using var reader = await listCommand.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            tables.Add((reader.GetString(0), reader.GetString(1)));
                        }
                    }

                    foreach (var (schema, table) in tables)
                    {
                        using var rebuildCommand = connection.CreateCommand();
                        rebuildCommand.CommandTimeout = 300; //Index rebuilds can run long on larger tables.
                        rebuildCommand.CommandText = $"ALTER INDEX ALL ON [{schema}].[{table}] REBUILD;";
                        await rebuildCommand.ExecuteNonQueryAsync();
                    }

                    return $"Rebuilt all indexes on {tables.Count} table(s) across {TargetSchemas.Length} schema(s).";
                }
                finally
                {
                    await context.Database.CloseConnectionAsync();
                }
            }
        }

        /// <summary>
        /// SQLite's <c>PRAGMA optimize</c> (<c>OptimizeDatabase.sql</c>) refreshes query-planner statistics. The
        /// direct MSSQL equivalent is <c>sp_updatestats</c>, which updates statistics for every table/indexed view
        /// in the database in one call - simpler and safer to run unattended than issuing per-table
        /// <c>UPDATE STATISTICS</c> statements, and (like <c>PRAGMA optimize</c>) cheap enough to run routinely
        /// since it skips tables whose statistics are already up to date. Like <see cref="VacuumDatabase"/>, this
        /// always operates on the whole database - <paramref name="databaseName"/> is accepted only for interface
        /// compatibility.
        /// </summary>
        public async Task<string> OptimizeDatabase(string databaseName)
        {
            var (context, connection) = await OpenAdminConnection();
            await using (context)
            {
                try
                {
                    using var command = connection.CreateCommand();
                    command.CommandTimeout = 300;
                    command.CommandText = "EXEC sp_updatestats;";
                    await command.ExecuteNonQueryAsync();

                    return "sp_updatestats completed - statistics refreshed for every table in the database.";
                }
                finally
                {
                    await context.Database.CloseConnectionAsync();
                }
            }
        }

        /// <summary>
        /// SQLite's <c>PRAGMA integrity_check</c> (<c>IntegrityCheckDatabase.sql</c>) is the direct analogue of
        /// <c>DBCC CHECKDB</c> - both are read-only structural/corruption checks, deliberately run without any
        /// repair option (admin "Verify" action, Database-Providers-Plan.md chapter 4.4 point 1: "bez REPAIR").
        /// Unlike the SQLite <c>DatabaseManager.IntegrityCheckDatabase</c>, this does <b>not</b> also append
        /// <see cref="ForeignKeyCheck"/>'s result - that concatenation on the SQLite side is a pre-existing bug
        /// (it appends an unawaited <see cref="Task{TResult}"/>'s <see cref="object.ToString"/>, not the actual
        /// check result) that is out of scope to port here; <c>ForeignKeyCheck</c> is exposed as, and stays, its
        /// own <see cref="ISpannedRepository"/> member. <c>DBCC CHECKDB</c> reports structural corruption via a
        /// thrown <see cref="SqlException"/> (severity &gt;= 16), not a result set, so success/failure is
        /// distinguished by catching it - mirroring <c>PRAGMA integrity_check</c> returning descriptive text
        /// rather than throwing. Whole-database, like <see cref="VacuumDatabase"/> -
        /// <paramref name="databaseName"/> is accepted only for interface compatibility.
        /// </summary>
        public async Task<string> IntegrityCheckDatabase(string databaseName)
        {
            var (context, connection) = await OpenAdminConnection();
            await using (context)
            {
                try
                {
                    using var command = connection.CreateCommand();
                    command.CommandTimeout = 300; //DBCC CHECKDB can run long on larger databases.
                    command.CommandText = "DBCC CHECKDB WITH NO_INFOMSGS, ALL_ERRORMSGS;";

                    try
                    {
                        await command.ExecuteNonQueryAsync();
                        return "DBCC CHECKDB completed - no corruption or structural issues found.";
                    }
                    catch (SqlException ex)
                    {
                        var messages = ex.Errors.Cast<SqlError>().Select(e => e.Message);
                        return "DBCC CHECKDB reported issues:\r\n" + string.Join("\r\n", messages);
                    }
                }
                finally
                {
                    await context.Database.CloseConnectionAsync();
                }
            }
        }

        /// <summary>
        /// SQLite's <c>PRAGMA foreign_key_check</c> (<c>ForeignKeyCheck.sql</c>) lists rows that violate a foreign
        /// key. <c>DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS</c> is the MSSQL equivalent extended to also cover
        /// <c>CHECK</c> constraints (Database-Providers-Plan.md chapter 4.4 point 2: "ověří FK i CHECK constraints
        /// v celé databázi") - run with no table/constraint name argument, it checks every constraint in the
        /// current database, matching "v celé databázi". Unlike <c>DBCC CHECKDB</c>, violations are reported as an
        /// ordinary result set (columns "Table" / "Constraint" / "Where" - confirmed against a live LocalDB
        /// instance; despite what several online references claim, they are not "Table Name" / "Constraint
        /// Name"), not by throwing, so no try/catch is needed here. Whole-database, like the other three members -
        /// <paramref name="databaseName"/> is accepted only for interface compatibility.
        /// </summary>
        public async Task<string> ForeignKeyCheck(string databaseName)
        {
            var (context, connection) = await OpenAdminConnection();
            await using (context)
            {
                try
                {
                    using var command = connection.CreateCommand();
                    command.CommandTimeout = 300;
                    command.CommandText = "DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS, ALL_ERRORMSGS;";

                    var violations = new List<string>();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var tableName = reader["Table"]?.ToString() ?? string.Empty;
                            var constraintName = reader["Constraint"]?.ToString() ?? string.Empty;
                            var where = reader["Where"]?.ToString() ?? string.Empty;
                            violations.Add($"{tableName} / {constraintName}: {where}");
                        }
                    }

                    return violations.Count == 0
                        ? "DBCC CHECKCONSTRAINTS reported no foreign key or check constraint violations."
                        : string.Join("\r\n", violations);
                }
                finally
                {
                    await context.Database.CloseConnectionAsync();
                }
            }
        }

        /// <summary>
        /// SQLite's <c>GetDatabaseVersion.sql</c> (<c>SELECT SQLITE_VERSION();</c>) reports the same SQLite engine
        /// version for all 8 "databases" - there is no per-database/per-schema schema-version concept on that
        /// side either. For the EF providers, the more informative and genuinely per-"database" analogue is each
        /// <see cref="Microsoft.EntityFrameworkCore.DbContext"/>'s own applied-migrations history (Database-
        /// Providers-Plan.md chapter 4.4/task 2a.4: "u EF/MSSQL je ekvivalentní zdroj pravdy historie aplikovaných
        /// EF Core migrací") - read from the two distinct <c>__EFMigrationsHistory_*</c> tables
        /// (<see cref="SqlServerMigrationsHistory"/>, chapter 2a.2), reporting the most recently applied
        /// <c>MigrationId</c> for each of the two <see cref="Microsoft.EntityFrameworkCore.DbContext"/>s this
        /// driver manages (<c>ApplicationDbContext</c> for Identity, <c>TightWikiDbContext</c> for everything
        /// else). <c>MigrationId</c> is timestamp-prefixed (<c>yyyyMMddHHmmss_Name</c>), so ordering it as a string
        /// is equivalent to ordering chronologically. Returns one row per <see cref="TargetSchemas"/> entry - the
        /// same 8 schema names <see cref="GetDatabasePageCounts"/>/<see cref="GetDatabasePageSizes"/> use - so that
        /// <c>AdminController.Database()</c>'s <c>FirstOrDefault(o => o.Name == version.Name)</c> join across all
        /// three methods actually matches instead of silently falling through to a zeroed-out
        /// <c>PageCount</c>/<c>PageSize</c>/<c>DatabaseSize</c> for any row.
        /// </summary>
        /// <remarks>
        /// 7 of the 8 schemas (everything except "Users") are owned exclusively by
        /// <see cref="TightWikiDbContext"/>, so they all report the same value - the most recently applied
        /// <c>MigrationId</c> from <c>dbo.__EFMigrationsHistory_TightWikiDb</c>. "Users" is the one schema shared
        /// between <see cref="TightWikiDbContext"/> (which spans all 8 schemas, see
        /// <see cref="SqlServerMigrationsHistory.TightWikiDbTableName"/>) and
        /// <see cref="global::TightWiki.Library.ApplicationDbContext"/> (ASP.NET Core Identity) - each context has
        /// its own, independently versioned migration history, so that one row reports both rather than picking
        /// (and silently discarding information about) just one, formatted as
        /// <c>"TightWikiDb: &lt;migration&gt; / Identity: &lt;migration&gt;"</c>.
        /// </remarks>
        public async Task<List<(string Name, string Version)>> GetDatabaseVersions()
        {
            var (context, connection) = await OpenAdminConnection();
            await using (context)
            {
                try
                {
                    var applicationDbVersion = await GetLatestMigrationId(connection,
                        SqlServerMigrationsHistory.ApplicationDbSchema, SqlServerMigrationsHistory.ApplicationDbTableName);
                    var tightWikiDbVersion = await GetLatestMigrationId(connection,
                        "dbo", SqlServerMigrationsHistory.TightWikiDbTableName);
                    var usersVersion = $"TightWikiDb: {tightWikiDbVersion} / Identity: {applicationDbVersion}";

                    return TargetSchemas
                        .Select(schema => (schema, schema == "Users" ? usersVersion : tightWikiDbVersion))
                        .ToList();
                }
                finally
                {
                    await context.Database.CloseConnectionAsync();
                }
            }
        }

        private static async Task<string> GetLatestMigrationId(DbConnection connection, string schema, string table)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT TOP (1) [MigrationId] FROM [{schema}].[{table}] ORDER BY [MigrationId] DESC;";
            var result = await command.ExecuteScalarAsync();
            return result as string ?? string.Empty;
        }

        /// <summary>
        /// SQLite's <c>PRAGMA page_count</c> (<c>GetDatabasePageCount.sql</c>) reports the total page count of a
        /// whole database file. Post-consolidation (chapter 4.3) there is one physical MSSQL database, so this
        /// reports the per-<b>schema</b> equivalent instead (chapter 4.4, "Admin obrazovka „Database""), summing
        /// <c>sys.dm_db_partition_stats.reserved_page_count</c> (total pages reserved for a table's data + all its
        /// indexes, across all partitions - the closest MSSQL analogue of "space this schema's data occupies on
        /// disk") for every user table per schema in <see cref="TargetSchemas"/>. <c>LEFT JOIN</c>+<c>COALESCE</c>
        /// so a schema with no tables yet still gets a row with <c>PageCount = 0</c>, keeping the "8 rows" shape
        /// <c>AdminController</c> (unmodified) expects.
        /// </summary>
        public async Task<List<(string Name, int PageCount)>> GetDatabasePageCounts()
        {
            var (context, connection) = await OpenAdminConnection();
            await using (context)
            {
                try
                {
                    var schemaList = string.Join(",", TargetSchemas.Select(s => $"'{s}'"));

                    using var command = connection.CreateCommand();
                    command.CommandText = $"""
                        SELECT s.name AS SchemaName, COALESCE(SUM(ps.reserved_page_count), 0) AS PageCount
                        FROM sys.schemas s
                        LEFT JOIN sys.tables t ON t.schema_id = s.schema_id AND t.is_ms_shipped = 0
                        LEFT JOIN sys.dm_db_partition_stats ps ON ps.object_id = t.object_id
                        WHERE s.name IN ({schemaList})
                        GROUP BY s.name
                        ORDER BY s.name;
                        """;

                    var results = new List<(string, int)>();
                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        results.Add((reader.GetString(0), Convert.ToInt32(reader.GetInt64(1))));
                    }
                    return results;
                }
                finally
                {
                    await context.Database.CloseConnectionAsync();
                }
            }
        }

        /// <summary>
        /// SQLite's <c>PRAGMA page_size</c> (<c>GetDatabasePageSize.sql</c>) is a per-database-file setting.
        /// SQL Server has no equivalent to query - <see cref="SqlServerPageSizeBytes"/> is a fixed 8 KB constant
        /// for every database/table/index, so this just pairs it with each of the 8 <see cref="TargetSchemas"/>
        /// names, matching the "8 rows" shape <see cref="GetDatabasePageCounts"/> returns (so that
        /// <c>PageCount * PageSize</c>, as computed by <c>AdminController.Database()</c>, yields each schema's
        /// approximate on-disk size in bytes). No database round-trip is needed for a compile-time constant.
        /// </summary>
        public Task<List<(string Name, int PageSize)>> GetDatabasePageSizes()
            => Task.FromResult(TargetSchemas.Select(name => (name, SqlServerPageSizeBytes)).ToList());

        #endregion
    }
}
