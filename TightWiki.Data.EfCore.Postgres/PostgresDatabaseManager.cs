using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
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
using UsersEntities = TightWiki.Data.EfCore.Entities.Users;

namespace TightWiki.Data.EfCore.Postgres
{
    /// <summary>
    /// PostgreSQL/EF Core implementation of <see cref="ITwDatabaseManager"/> and <see cref="ISpannedRepository"/> -
    /// the third, build-time-selectable implementation alongside the SQLite
    /// <c>TightWiki.Repository.Helpers.DatabaseManager</c> and <c>TightWiki.Data.EfCore.SqlServer.SqlServerDatabaseManager</c>
    /// (see Database-Providers-Plan.md chapter 4.2, phase 3). Will be selected instead of those two by
    /// <c>Program.cs</c> when built with <c>-p:DataProvider=Postgres</c> once the wiring lands (phase 3.7) - not
    /// referenced from <c>Program.cs</c> yet.
    /// </summary>
    /// <remarks>
    /// This is the phase 3.2 skeleton, extended in phase 3.5 and 3.6: the constructor, <see cref="Logger"/>, every
    /// repository property (wired directly to the shared, provider-agnostic <c>Ef*Repository</c> classes in
    /// <c>TightWiki.Data.EfCore</c> - already real, same as <see cref="TightWiki.Data.EfCore.SqlServer.SqlServerDatabaseManager"/>
    /// wires them), <see cref="CreateDbContext"/>/<see cref="CreateApplicationDbContext"/>, <see cref="InitializeSchema"/>,
    /// every <see cref="ISpannedRepository"/> member (phase 3.5 - vendor-native PostgreSQL maintenance
    /// operations, Database-Providers-Plan.md chapter 4.4/4.2), and <see cref="ApplyAllSeedData"/>/
    /// <see cref="SeedContentDataAsync"/> (phase 3.6 - copied from <c>SqlServerDatabaseManager</c>'s own phase
    /// 2a.5/2a.10 implementation, which is pure provider-agnostic LINQ against <see cref="TightWikiDbContext"/>/
    /// <see cref="EfDefaultsRepository"/> with no T-SQL - see each method's own doc comment) are real. Wiring this
    /// into <c>Program.cs</c> - including the pre-<c>Build()</c> <c>SeedContentDataAsync</c> call and the choice of
    /// which <see cref="TwDefaultDataType"/> flags to pass (the MSSQL build passes
    /// <see cref="TwDefaultDataType.BuiltinPages"/>, see <see cref="SeedContentDataAsync"/>'s doc comment for why)
    /// - is still phase 3.7, not this one; this method itself already handles <see cref="TwDefaultDataType.BuiltinPages"/>
    /// correctly once a caller passes it.
    /// </remarks>
    public sealed class PostgresDatabaseManager : ITwDatabaseManager, ISpannedRepository
    {
        /// <summary>
        /// See <see cref="ITwDatabaseManager.Logger"/>. A plain console logger for the whole lifetime of this
        /// phase 3.2 skeleton - unlike <c>SqlServerDatabaseManager</c>, this is deliberately <b>not</b> promoted
        /// to <see cref="TightWiki.Library.DatabaseLogger"/> after <see cref="LoggingRepository"/> is constructed
        /// below. <see cref="EfLoggingRepository"/> itself is real (shared, provider-agnostic, already
        /// implemented in <c>TightWiki.Data.EfCore</c> - see <see cref="LoggingRepository"/>'s wiring), but
        /// promoting <see cref="Logger"/> to write through it would mean every log call - including from
        /// <see cref="InitializeSchema"/> itself - immediately depends on <see cref="TightWikiDbContext"/>'s
        /// Logging schema actually existing and being migrated, which is exactly the ordering
        /// <c>SqlServerDatabaseManager</c> gets away with only because its own <c>InitializeSchema</c> has already
        /// been battle-tested end to end (phases 2a.1-2a.10). This driver's <see cref="InitializeSchema"/> has not
        /// - it is still unverified against a live PostgreSQL instance beyond construction/connectivity (see
        /// Database-Providers-Plan.md phase 3.2's task notes) - so staying on the always-available
        /// <see cref="ConsoleLogger"/> here avoids a bootstrap dependency this phase cannot yet fully exercise.
        /// The promotion can be revisited once phase 3.4's migrations and phase 3.6's seeding have proven
        /// <see cref="InitializeSchema"/> end to end, same as SQL Server's did.
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
        /// chapter 7, "Rozhodnuto" - "Connection string klíč: ConnectionStrings:TightWikiEfCore"), shared across
        /// every EF Core driver project including this one.
        /// </summary>
        private readonly string _connectionString;

        public PostgresDatabaseManager(IConfiguration configuration)
        {
            Logger = new ConsoleLogger();

            _connectionString = configuration.GetConnectionString("TightWikiEfCore")
                ?? throw new InvalidOperationException(
                    "Missing connection string 'ConnectionStrings:TightWikiEfCore', which is required when built with -p:DataProvider=Postgres.");

            //Every repository below is the same shared, provider-agnostic Ef*Repository class SqlServerDatabaseManager
            //wires (TightWiki.Data.EfCore) - none of it is SQL Server specific, so there is nothing left to stub
            //here even though this driver's own InitializeSchema/ISpannedRepository members are still incomplete.
            ConfigurationRepository = new EfConfigurationRepository(CreateDbContext, CreateApplicationDbContext);
            DefaultsRepository = new EfDefaultsRepository();
            LoggingRepository = new EfLoggingRepository(CreateDbContext, ConfigurationRepository);
            EmojiRepository = new EfEmojiRepository(CreateDbContext, ConfigurationRepository);
            PageRepository = new EfPageRepository(CreateDbContext, ConfigurationRepository);
            StatisticsRepository = new EfStatisticsRepository(CreateDbContext, ConfigurationRepository);
            UsersRepository = new EfUsersRepository(CreateDbContext, CreateApplicationDbContext, ConfigurationRepository);
        }

        /// <summary>
        /// Creates a new <see cref="TightWikiDbContext"/> configured against the PostgreSQL connection string.
        /// Callers are responsible for disposing the returned context.
        /// </summary>
        private TightWikiDbContext CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<TightWikiDbContext>();
            //Migrations for this shared context will live in this driver project's own assembly (phase 3.4), not
            //TightWiki.Data.EfCore's, and its MigrationsHistoryTable is explicit and distinct from
            //ApplicationDbContext's (PostgresMigrationsHistory) - same reasoning as the SQL Server driver's
            //CreateDbContext/TightWikiDbContextFactory.
            optionsBuilder.UseNpgsql(_connectionString,
                b => b.MigrationsAssembly(typeof(PostgresDatabaseManager).Assembly.GetName().Name)
                      .MigrationsHistoryTable(PostgresMigrationsHistory.TightWikiDbTableName, PostgresMigrationsHistory.TightWikiDbSchema));
            return new TightWikiDbContext(optionsBuilder.Options);
        }

        /// <summary>
        /// Creates a new <see cref="ApplicationDbContext"/> (ASP.NET Core Identity, Database-Providers-Plan.md
        /// chapter 4.1.1) configured against the same PostgreSQL connection string as <see cref="CreateDbContext"/>.
        /// Callers are responsible for disposing the returned context.
        /// </summary>
        private ApplicationDbContext CreateApplicationDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            //Migrations for this shared context will live in this driver project's own assembly (phase 3.4), not
            //TightWiki.Library's, and its MigrationsHistoryTable is explicit and distinct from TightWikiDbContext's
            //(PostgresMigrationsHistory) - same reasoning as the SQL Server driver's CreateApplicationDbContext/
            //ApplicationDbContextFactory.
            optionsBuilder.UseNpgsql(_connectionString,
                b => b.MigrationsAssembly(typeof(PostgresDatabaseManager).Assembly.GetName().Name)
                      .MigrationsHistoryTable(PostgresMigrationsHistory.ApplicationDbTableName, PostgresMigrationsHistory.ApplicationDbSchema));
            return new ApplicationDbContext(optionsBuilder.Options);
        }

        /// <summary>
        /// EF Core implementation of <see cref="ITwDatabaseManager.InitializeSchema"/>: applies any pending EF Core
        /// migrations via <see cref="Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.MigrateAsync"/>
        /// (Database-Providers-Plan.md chapter 4.2), same two-context ordering as
        /// <c>SqlServerDatabaseManager.InitializeSchema</c> - see its remarks for why
        /// <see cref="ApplicationDbContext"/> (Identity) migrates before <see cref="TightWikiDbContext"/>.
        /// </summary>
        /// <remarks>
        /// One PostgreSQL-specific step not present on the SQL Server driver: immediately before
        /// <see cref="TightWikiDbContext"/>'s migrations run, this ensures the <c>citext</c> extension exists
        /// (<c>CREATE EXTENSION IF NOT EXISTS citext;</c>, idempotent - safe even if this runs on every startup
        /// with nothing pending). <c>citext</c> is required by the case-insensitive-text columns phase 3.3 adds to
        /// the shared EF model; without the extension present first, phase 3.4's migrations (which create those
        /// columns) would fail the first time they run against a fresh database. No migrations exist yet as of
        /// phase 3.2 - <see cref="Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade.GetPendingMigrationsAsync"/>
        /// against an empty <see cref="Microsoft.EntityFrameworkCore.Migrations.MigrationsAssemblyAttribute"/>
        /// simply reports none pending, so <see cref="Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade.MigrateAsync"/>
        /// - and therefore the <c>CREATE EXTENSION</c> statement below - is not yet reachable in practice; this
        /// exists so it is already wired correctly once phase 3.4 adds the first migration.
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
                //Must run before MigrateAsync below - see this method's remarks. IF NOT EXISTS makes this safe to
                //repeat on every upgrade that has pending TightWikiDbContext migrations, not just the very first.
                await context.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS citext;");

                await context.Database.MigrateAsync();
            }

            return wasIdentitySchemaUpgraded || wasWikiSchemaUpgraded;
        }

        /// <summary>
        /// EF Core implementation of <see cref="ITwDatabaseManager.ApplyAllSeedData"/> (Database-Providers-Plan.md
        /// chapter 4.6/phase 3.6). Reads the provider-neutral content seed (<see cref="DefaultsRepository"/> over
        /// "Seed\tightwiki.seed.zip") and writes it directly into <see cref="TightWikiDbContext"/> via
        /// <c>Add</c>/<c>AddRange</c> + <c>SaveChangesAsync</c> - deliberately <b>not</b> through
        /// <see cref="ConfigurationRepository"/>/<see cref="PageRepository"/>, matching the shared, provider-agnostic
        /// pattern <c>SqlServerDatabaseManager.ApplyAllSeedData</c> (phase 2a.5/2a.10) already established: this
        /// entire seed-import block (this method, <see cref="SeedContentDataAsync"/>, <see cref="EnsureAdminUser"/>,
        /// <see cref="SeedConfigurations"/>, <see cref="SeedThemes"/>, <see cref="SeedWikiPages"/>,
        /// <see cref="SeedFeatureTemplates"/>, <see cref="SeedMenuItems"/>, <see cref="SeedEmojiAndCategories"/>) is
        /// copied verbatim from <c>SqlServerDatabaseManager</c> - it is pure LINQ against the shared
        /// <see cref="TightWikiDbContext"/>/<see cref="EfDefaultsRepository"/>, with no T-SQL and nothing SQL
        /// Server-specific to adapt for PostgreSQL.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Kept split the same way as the MSSQL driver (phase 2a.10): everything except <see cref="EnsureAdminUser"/>
        /// lives in <see cref="SeedContentDataAsync"/>, which this method is now a thin wrapper over - ensure the
        /// admin user exists, then delegate. It still exists (rather than every caller using the split halves
        /// directly) because it is the <see cref="ITwDatabaseManager"/> interface member, whose signature could not
        /// change (see <see cref="SeedContentDataAsync"/>'s doc comment for why the split exists and how the two
        /// calls in <c>Program.cs</c> divide the work on the MSSQL build - the same division will apply here once
        /// phase 3.7 wires this driver into <c>Program.cs</c>).
        /// </para>
        /// <para>
        /// Mirrors the SQLite reference (<c>TightWiki.Repository.Helpers.DatabaseManager.ApplyAllSeedData</c>) in
        /// structure and in which <paramref name="defaultDataTypes"/> flag gates which section - Configurations,
        /// Themes, {Help,Include,Builtin}Pages, FeatureTemplates. Config.MenuItem and the whole Emoji schema have
        /// no corresponding <see cref="TwDefaultDataType"/> flag and are seeded unconditionally instead: on
        /// SQLite these tables are never populated through this method at all - they arrive "for free" via a
        /// full copy of the shipped, pre-populated Data\config.db/emoji.db files (see
        /// <see cref="DefaultsRepository"/>'s and <c>DefaultsRepository.GetDefaultEmojis</c>'s doc comments) - but
        /// PostgreSQL has no such file-copy shortcut, so this is the only path that ever populates them.
        /// </para>
        /// <para>
        /// Idempotency mirrors the SQLite reference's "MERGE ... ON CONFLICT DO UPDATE" scripts
        /// (Scripts\Defaults\Merge\*.sql): each row is looked up by its natural key first, existing rows are
        /// updated in place (except <c>ConfigurationEntry.Value</c>, deliberately preserved on conflict so a
        /// re-run never clobbers an administrator's tuned setting - see <c>MergeConfigurationEntry.sql</c> and
        /// <see cref="SeedConfigurations"/>), and missing rows are inserted. In practice this only matters if
        /// this method (or <see cref="SeedContentDataAsync"/>) is ever invoked more than once against the same
        /// database - both callers (<c>Program.cs</c>, once phase 3.7 wires this driver in) will gate on
        /// <see cref="InitializeSchema"/> having just performed the very first migration, same as the MSSQL build.
        /// </para>
        /// <para>
        /// Bootstrapping the admin account needed for <c>Page.CreatedByUserId</c>/<c>ModifiedByUserId</c>
        /// (<see cref="EnsureAdminUser"/>) talks to <paramref name="userManager"/> (real ASP.NET Core Identity) and
        /// writes a matching <c>Users.Profile</c> row directly - not through
        /// <c>ITwUsersRepository.CreateProfile</c>/<c>UpsertUserClaims</c>, since that repository's profile-creation
        /// path is not what this bootstrap step uses on the MSSQL driver either. Claims (first/last name) are
        /// therefore not seeded here; that is purely cosmetic and out of scope for seeding wiki page ownership.
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
        /// The DI-free half of <see cref="ApplyAllSeedData"/> (Database-Providers-Plan.md phase 3.6, mirroring
        /// <c>SqlServerDatabaseManager</c>'s own phase 2a.10 split): seeds everything <see cref="ApplyAllSeedData"/>
        /// does except <see cref="EnsureAdminUser"/> - i.e. <see cref="SeedConfigurations"/>, <see cref="SeedThemes"/>,
        /// <see cref="SeedWikiPages"/>, <see cref="SeedFeatureTemplates"/>, <see cref="SeedMenuItems"/>,
        /// <see cref="SeedEmojiAndCategories"/>. Takes only what it needs to open a <see cref="TightWikiDbContext"/>
        /// (nothing - it reuses <see cref="CreateDbContext"/> like every other method here) and
        /// <paramref name="defaultDataTypes"/>; deliberately does <b>not</b> take a <see cref="UserManager{TUser}"/>,
        /// so it is safe to call before ASP.NET Core's DI container exists.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Exists for the same reason the MSSQL driver's does: <c>Program.cs</c> constructs
        /// <c>WikiConfigurationManager</c> before <c>WebApplicationBuilder.Build()</c>, and that constructor eagerly
        /// reads Config.Theme (<c>WikiConfigurationManager.ReloadAll</c>: <c>.Single(o => o.Name == themeName)</c>)
        /// - which is empty on a freshly migrated-but-unseeded PostgreSQL database (unlike SQLite, where Config.Theme
        /// ships pre-seeded inside the shipped <c>config.db</c> file), crashing the app with "Sequence contains no
        /// matching element" before Kestrel ever starts listening. On the MSSQL build, <c>Program.cs</c> calls the
        /// equivalent method directly (via an explicit cast - it is intentionally not part of
        /// <see cref="ITwDatabaseManager"/>) right after <see cref="InitializeSchema"/>, gated on the same "was the
        /// schema just upgraded" condition as the later <see cref="ApplyAllSeedData"/> call; wiring the same call for
        /// this driver is phase 3.7, not this one - see this method's own doc comment on <see cref="ApplyAllSeedData"/>
        /// for what remains out of scope here.
        /// </para>
        /// <para>
        /// Because no <see cref="UserManager{TUser}"/> is available yet at that point in <c>Program.cs</c>, this
        /// method cannot create the admin user itself. Instead of skipping <see cref="SeedWikiPages"/> outright,
        /// it looks up whatever admin <c>Users.Profile</c> row already exists (there is none on the very first,
        /// pre-<c>Build()</c> call) and only seeds wiki pages if one is found. The later, post-<c>Build()</c> call
        /// to <see cref="ApplyAllSeedData"/> runs <see cref="EnsureAdminUser"/> first and then calls this method
        /// again - by then the admin profile exists, so that second call is what actually seeds wiki pages; every
        /// other section it repeats (Configurations, Themes, FeatureTemplates, MenuItems, Emoji) is a no-op the
        /// second time, per this method's idempotency (see <see cref="ApplyAllSeedData"/>'s remarks).
        /// </para>
        /// <para>
        /// <see cref="TwDefaultDataType.BuiltinPages"/> - the fallback "Wiki Page Does Not Exist"/"Wiki Page
        /// Revision Does Not Exist" pages a brand new install's very first request (including the home page) needs
        /// to avoid an unhandled exception - is handled by this method exactly like every other namespace flag
        /// below (<c>if (defaultDataTypes.Contains(TwDefaultDataType.BuiltinPages)) namespaces.Add("Builtin");</c>).
        /// It is copied here unmodified from <c>SqlServerDatabaseManager.SeedContentDataAsync</c>; whether
        /// <c>Program.cs</c> actually passes <see cref="TwDefaultDataType.BuiltinPages"/> for this driver is phase
        /// 3.7's job, not this one - see this class's own top-of-file remarks.
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
        /// Finds or creates the built-in admin <see cref="IdentityUser"/> (looked up/created by
        /// <see cref="Constants.DEFAULTUSERNAME"/>, not the literal string <c>"admin"</c> - see remarks) together
        /// with its matching <c>Users.Profile</c> row (still keyed on the literal <c>"admin"</c>
        /// <c>Users.Profile.AccountName</c>/<c>Navigation</c>, which is an independent, TightWiki-owned value
        /// unrelated to the Identity username), so that <see cref="SeedWikiPages"/> has a valid
        /// <c>Page.CreatedByUserId</c>/<c>ModifiedByUserId</c>. Returns null (logging the failure) if no admin user
        /// could be found or created, in which case the caller skips wiki page seeding entirely - same fallback
        /// behavior as the SQLite reference.
        /// </summary>
        /// <remarks>
        /// The SQLite reference's inline bootstrap (<c>DatabaseManager.ApplyAllSeedData</c>) finds/creates the
        /// Identity user by the literal username <c>"admin"</c> instead. This intentionally diverges from that -
        /// copied unmodified from <c>SqlServerDatabaseManager.EnsureAdminUser</c>, which fixed exactly this
        /// divergence after it was found by smoke-testing a fresh SQL Server install (phase 2b.14): this method
        /// runs (via <see cref="ApplyAllSeedData"/>) immediately before
        /// <c>EfUsersRepository.ValidateEncryptionAndCreateAdminUserAsync</c> - called moments later from the same
        /// <c>Program.cs</c> DI scope - which independently finds/creates the Identity user by
        /// <see cref="Constants.DEFAULTUSERNAME"/> and then re-points the existing <c>Profile</c> row (keyed on
        /// <see cref="Constants.DEFAULTACCOUNT"/>) at whatever id it resolved via <c>SetProfileUserId</c>. Looking
        /// this method's user up by the literal <c>"admin"</c> username instead would create a second, distinct
        /// <see cref="IdentityUser"/> from the one <c>ValidateEncryptionAndCreateAdminUserAsync</c> later
        /// finds/creates under <see cref="Constants.DEFAULTUSERNAME"/>, and re-pointing <c>Profile.UserId</c> from
        /// this method's id to that one would violate the FK from every already-seeded
        /// <c>Page.CreatedByUserId</c>/<c>ModifiedByUserId</c>/<c>PageRevision.ModifiedByUserId</c> (and every other
        /// column referencing <c>Profile.UserId</c> across the <c>Pages</c>/<c>DeletedPages</c>/
        /// <c>DeletedPageRevisions</c>/<c>Users</c> schemas) - an UPDATE of a primary key still referenced by
        /// existing FK rows. Looking this method's user up by <see cref="Constants.DEFAULTUSERNAME"/> instead means
        /// <c>ValidateEncryptionAndCreateAdminUserAsync</c>'s later <c>FindByNameAsync</c> finds the very same
        /// Identity user this method already created, so its subsequent <c>SetProfileUserId</c> call is a no-op
        /// UPDATE (new value == existing value) rather than a genuine repoint, and no FK violation occurs. This has
        /// no SQLite-side equivalent to keep in sync with: the SQLite reference matches the admin
        /// <c>Users.Profile</c> row by <c>Navigation</c>/<c>AccountName</c> (<c>GetAdminUserId.sql</c>), not by
        /// Identity username, so it never re-points an existing profile's primary key the way this MSSQL/PostgreSQL
        /// EF path does.
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
        /// Emoji.Emoji/EmojiCategory) rather than letting PostgreSQL's identity column generate new ones - so that
        /// e.g. <c>ConfigurationGroup.Id</c> stays 1:1 with the SQLite reference (Data\config.db / Data\emoji.db).
        /// Written generically (schema/table name + an arbitrary insert/update callback) so a future Page-seeding
        /// pass (out of scope here) can reuse it too.
        /// </summary>
        /// <param name="context">The context <paramref name="insertAction"/> will call <c>SaveChangesAsync</c> on.</param>
        /// <param name="schema">The schema of the table being seeded, e.g. "Config".</param>
        /// <param name="table">The table being seeded, e.g. "ConfigurationGroup".</param>
        /// <param name="insertAction">
        /// Adds/updates entities (with explicit, non-default <c>Id</c> values on any newly-added ones) against
        /// <paramref name="context"/> and calls <c>SaveChangesAsync</c>.
        /// </param>
        /// <remarks>
        /// Unlike SQL Server's identity columns, <paramref name="table"/>'s <c>Id</c> column is
        /// <c>GENERATED BY DEFAULT AS IDENTITY</c> (see <c>TightWikiDbContextModelSnapshot</c>'s
        /// <c>UseIdentityByDefaultColumn</c> calls), which lets an explicit <c>INSERT</c> value through with no
        /// special syntax - no equivalent of SQL Server's <c>SET IDENTITY_INSERT</c> is needed, so
        /// <paramref name="insertAction"/> just runs directly. All that is left afterwards is moving the
        /// underlying sequence past whatever explicit values were just written (<c>setval</c> against
        /// <c>pg_get_serial_sequence</c>'s current <c>MAX("Id")</c>), so that the next ordinary,
        /// database-generated insert continues after them instead of colliding with them.
        /// </remarks>
        private static async Task SeedWithExplicitIdentityAsync(
            TightWikiDbContext context, string schema, string table, Func<Task> insertAction)
        {
            await insertAction();

            var qualifiedTable = $"\"{schema}\".\"{table}\"";

            //Table/schema names can't be parameterized in this statement, and qualifiedTable is always built from
            //this method's own hardcoded caller-supplied literals ("Config"/"ConfigurationGroup" etc. - never
            //external/user input), so there is nothing here for the EF1002 SQL-injection analyzer to actually
            //protect against - suppressed per its own suggested "make sure the value is sanitized and suppress
            //the warning" (same reasoning/pattern as EfPageRepository.SaveChangesWithIdentityInsertAsync).
#pragma warning disable EF1002 // Possible SQL injection vulnerability.
            await context.Database.ExecuteSqlRawAsync($"""
                SELECT setval(pg_get_serial_sequence('{qualifiedTable}', 'Id'), COALESCE((SELECT MAX("Id") FROM {qualifiedTable}), 1));
                """);
#pragma warning restore EF1002
        }

        /// <summary>
        /// Seeds Config.ConfigurationGroup and Config.ConfigurationEntry from <see cref="DefaultsRepository"/>.
        /// Mirrors MergeConfigurationGroup.sql/MergeConfigurationEntry.sql: existing rows (matched by their
        /// natural key) are updated in place rather than duplicated, except that an existing entry's
        /// <c>Value</c> is deliberately left untouched (see <see cref="ApplyAllSeedData"/>'s remarks). Newly
        /// inserted rows carry the seed package's own <see cref="TwDefaultConfiguration.ConfigurationGroupId"/>/
        /// <see cref="TwDefaultConfiguration.ConfigurationEntryId"/> instead of letting PostgreSQL generate a new
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
                    existingPage.ModifiedDate = now;

                    var existingRevision = await context.Pages_PageRevisions.FindAsync(existingPage.Id, existingPage.Revision);
                    if (existingRevision != null)
                    {
                        existingRevision.Name = defaultPage.Name;
                        existingRevision.Namespace = defaultPage.Namespace;
                        existingRevision.Description = defaultPage.Description;
                        existingRevision.Body = defaultPage.Body;
                        existingRevision.ModifiedByUserId = adminUserId;
                        existingRevision.ModifiedDate = now;
                        existingRevision.DataHash = defaultPage.DataHash;
                    }
                }
                else
                {
                    var newPage = new PagesEntities.Page
                    {
                        Name = defaultPage.Name,
                        Namespace = defaultPage.Namespace,
                        Navigation = defaultPage.Navigation,
                        Description = defaultPage.Description,
                        Revision = 1,
                        CreatedByUserId = adminUserId,
                        CreatedDate = now,
                        ModifiedByUserId = adminUserId,
                        ModifiedDate = now,
                    };
                    context.Pages_Pages.Add(newPage);
                    await context.SaveChangesAsync(); //Need the generated Id - PageRevision.PageId is not a navigation.

                    context.Pages_PageRevisions.Add(new PagesEntities.PageRevision
                    {
                        PageId = newPage.Id,
                        Name = defaultPage.Name,
                        Namespace = defaultPage.Namespace,
                        Description = defaultPage.Description,
                        Body = defaultPage.Body,
                        Revision = 1,
                        ModifiedByUserId = adminUserId,
                        ModifiedDate = now,
                        DataHash = defaultPage.DataHash,
                    });

                    existingPages[defaultPage.Navigation] = newPage;
                }
            }

            await context.SaveChangesAsync();

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

            var pageIdsByKey = seededPages.ToDictionary(p => $"{p.Name} {p.Namespace}", p => p.Id, StringComparer.OrdinalIgnoreCase);
            var existingFiles = await context.Pages_PageFiles.ToDictionaryAsync(f => $"{f.PageId} {f.Navigation}", StringComparer.OrdinalIgnoreCase);
            var now = DateTime.UtcNow;

            foreach (var defaultAttachment in defaultAttachments)
            {
                if (!pageIdsByKey.TryGetValue($"{defaultAttachment.PageName} {defaultAttachment.Namespace}", out var pageId))
                {
                    continue;
                }

                var dataHash = SecurityUtility.Crc32(defaultAttachment.Data);
                var fileKey = $"{pageId} {defaultAttachment.FileNavigation}";

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
        /// instead of letting PostgreSQL generate a new one, via <see cref="SeedWithExplicitIdentityAsync"/>, so
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
        /// <see cref="Utility.Compress"/> (see Database-Providers-Plan.md chapter 4.6 / commit 7eb2c329) - same as
        /// the MSSQL driver, copied unmodified.</description></item>
        /// <item><description>Track which emoji ids were actually seeded (<c>seededEmojiIds</c>), so an
        /// EmojiCategory row whose <see cref="TwDefaultEmojiCategory.EmojiId"/> doesn't resolve to any emoji can
        /// still be skipped with a warning instead of violating referential integrity.</description></item>
        /// </list>
        /// Newly inserted Emoji/EmojiCategory rows carry the seed package's own <see cref="TwDefaultEmoji.Id"/>/
        /// <see cref="TwDefaultEmojiCategory.Id"/> instead of letting PostgreSQL generate new ones, via
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
        /// The 8 schemas <see cref="TightWikiDbContext"/> spans (Database-Providers-Plan.md chapter 4.3) - the same
        /// 8 names, in the same order, as <c>SqlServerDatabaseManager.TargetSchemas</c> and confirmed against this
        /// driver's own migration (<c>Migrations\TightWikiDb\*_InitialCreate.cs</c>'s <c>EnsureSchema</c> calls).
        /// Does <b>not</b> include <c>ApplicationDbContext</c>'s own migrations-history table's schema ("Users" is
        /// shared between the two contexts, see <see cref="PostgresMigrationsHistory"/>) or the default "public"
        /// schema that only holds <see cref="TightWikiDbContext"/>'s migrations-history table.
        /// </summary>
        private static readonly string[] TargetSchemas =
        [
            "DeletedPageRevisions", "DeletedPages", "Pages", "Statistics", "Emoji", "Logging", "Users", "Config"
        ];

        /// <summary>
        /// Opens (and, via each call site's <see langword="await using"/>, closes) a dedicated
        /// <see cref="TightWikiDbContext"/>-backed ADO.NET connection for the raw admin statements below - none of
        /// these are expressible as LINQ, and mixing raw ADO.NET access with EF Core's own connection management
        /// via <c>Database.OpenConnectionAsync</c>/<c>CloseConnectionAsync</c> (rather than opening the underlying
        /// <see cref="DbConnection"/> directly) is the documented-safe way to do that. Direct analogue of
        /// <c>SqlServerDatabaseManager.OpenAdminConnection</c>.
        /// </summary>
        /// <remarks>
        /// Opening the connection this way does not start any ambient transaction - <c>OpenConnectionAsync</c> only
        /// calls <see cref="DbConnection.OpenAsync()"/> under the hood, nothing more. That matters specifically for
        /// <see cref="VacuumDatabase"/>: PostgreSQL refuses to run <c>VACUUM</c> "inside a transaction block". Every
        /// admin method here issues one statement per <see cref="DbCommand.ExecuteNonQueryAsync()"/>/
        /// <see cref="DbCommand.ExecuteReaderAsync()"/> call rather than concatenating several ';'-separated
        /// statements into one <see cref="DbCommand.CommandText"/> - PostgreSQL's simple query protocol implicitly
        /// wraps a *multi-statement* message in a single transaction (as if <c>BEGIN</c> had been issued), which is
        /// the actual, commonly-hit cause of "VACUUM cannot run inside a transaction block" from ADO.NET/ORM code,
        /// not connection-opening by itself. As long as that one-statement-per-command discipline is kept, the
        /// plain autocommit connection this method returns needs no further configuration (no explicit
        /// <c>SET AUTOCOMMIT</c> concept exists in PostgreSQL/Npgsql - autocommit is simply "no open transaction").
        /// </remarks>
        private async Task<(TightWikiDbContext Context, DbConnection Connection)> OpenAdminConnection()
        {
            var context = CreateDbContext();
            await context.Database.OpenConnectionAsync();
            return (context, context.Database.GetDbConnection());
        }

        /// <summary>
        /// SQLite's <c>VACUUM</c> (<c>VacuumDatabase.sql</c>) rebuilds the entire file into a fresh copy,
        /// reclaiming free space and defragmenting storage; SQL Server's driver runs <c>ALTER INDEX ALL REBUILD</c>
        /// per table for the same purpose (no single-statement whole-database equivalent exists there either).
        /// PostgreSQL, uniquely among the three, has an actual <c>VACUUM</c> statement of its own, so this runs
        /// <c>VACUUM (ANALYZE) "schema"."table";</c> for every table across all 8 <see cref="TargetSchemas"/> - one
        /// statement per table (see <see cref="OpenAdminConnection"/>'s remarks for why that matters), reclaiming
        /// dead-tuple space left behind by updates/deletes and refreshing planner statistics in the same pass
        /// (<c>ANALYZE</c> option), same spirit as SQLite's <c>VACUUM</c> and MSSQL's index rebuild. Deliberately
        /// not <c>VACUUM FULL</c>: that variant requires an exclusive table lock and rewrites the table to a new
        /// file to return space to the OS, which is exactly the kind of disruptive, not-safe-to-run-routinely
        /// operation the SQL Server driver's own doc comment explains why <c>DBCC SHRINKDATABASE</c> was rejected
        /// for - plain <c>VACUUM</c> is safe to run online/regularly and is what PostgreSQL's own maintenance
        /// guidance recommends for this purpose. Runs across every table in <see cref="TargetSchemas"/> regardless
        /// of the <paramref name="databaseName"/> argument - after schema consolidation there is exactly one
        /// physical PostgreSQL database, so there is nothing left for that argument to meaningfully select between;
        /// it is only still accepted to satisfy the shared <see cref="ISpannedRepository"/> signature.
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
                            SELECT n.nspname AS SchemaName, c.relname AS TableName
                            FROM pg_class c
                            JOIN pg_namespace n ON n.oid = c.relnamespace
                            WHERE c.relkind = 'r' AND n.nspname IN ({schemaList})
                            ORDER BY n.nspname, c.relname;
                            """;
                        using var reader = await listCommand.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            tables.Add((reader.GetString(0), reader.GetString(1)));
                        }
                    }

                    foreach (var (schema, table) in tables)
                    {
                        //One statement per ExecuteNonQueryAsync call - see OpenAdminConnection's remarks on why
                        //VACUUM needs that (never concatenate these into one multi-statement CommandText).
                        using var vacuumCommand = connection.CreateCommand();
                        vacuumCommand.CommandTimeout = 300; //VACUUM can run long on larger tables.
                        vacuumCommand.CommandText = $"VACUUM (ANALYZE) \"{schema}\".\"{table}\";";
                        await vacuumCommand.ExecuteNonQueryAsync();
                    }

                    return $"VACUUM (ANALYZE) completed on {tables.Count} table(s) across {TargetSchemas.Length} schema(s).";
                }
                finally
                {
                    await context.Database.CloseConnectionAsync();
                }
            }
        }

        /// <summary>
        /// SQLite's <c>PRAGMA optimize</c> (<c>OptimizeDatabase.sql</c>) refreshes query-planner statistics; the
        /// MSSQL driver runs <c>sp_updatestats</c> for the same purpose. The PostgreSQL equivalent is a bare
        /// <c>ANALYZE;</c> with no table argument, which collects statistics for every table in the current
        /// database in one call - simpler and safer to run unattended than issuing per-table <c>ANALYZE</c>
        /// statements, and (like <c>PRAGMA optimize</c>/<c>sp_updatestats</c>) cheap enough to run routinely.
        /// <see cref="VacuumDatabase"/> already runs <c>ANALYZE</c> as part of its own <c>VACUUM (ANALYZE)</c> per
        /// table, so this is somewhat redundant with it in practice - it is kept as its own
        /// <see cref="ISpannedRepository"/> member regardless, matching the shape of the interface/MSSQL driver
        /// (an admin action to refresh statistics without also paying for a full <c>VACUUM</c> pass). Like
        /// <see cref="VacuumDatabase"/>, this always operates on the whole database - <paramref name="databaseName"/>
        /// is accepted only for interface compatibility.
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
                    command.CommandText = "ANALYZE;";
                    await command.ExecuteNonQueryAsync();

                    return "ANALYZE completed - planner statistics refreshed for every table in the database.";
                }
                finally
                {
                    await context.Database.CloseConnectionAsync();
                }
            }
        }

        /// <summary>
        /// SQLite's <c>PRAGMA integrity_check</c> (<c>IntegrityCheckDatabase.sql</c>) and MSSQL's <c>DBCC CHECKDB</c>
        /// are both read-only, whole-database structural/corruption checks. PostgreSQL has <b>no</b> built-in
        /// statement that does the same thing for the whole database - there is no <c>DBCC CHECKDB</c> analogue
        /// here. The closest available tool is the first-party <c>amcheck</c> contrib extension
        /// (<c>CREATE EXTENSION IF NOT EXISTS amcheck;</c>, idempotent), which only verifies the internal structural
        /// consistency of individual B-tree indexes (<c>bt_index_check</c>, with <c>heapallindexed := true</c> to
        /// also cross-check that every heap tuple is represented in the index) - it does <b>not</b> check heap/table
        /// data integrity, TOAST consistency, or anything beyond B-tree indexes the way <c>DBCC CHECKDB</c> checks
        /// an entire database. This is deliberately documented as a narrower, structural *index* check, not a full
        /// <c>DBCC CHECKDB</c> equivalent - none exists on PostgreSQL. Every B-tree index across all 8
        /// <see cref="TargetSchemas"/> is checked, one <c>bt_index_check</c> call per index (never concatenated -
        /// same one-statement-per-command discipline as <see cref="VacuumDatabase"/>, though <c>bt_index_check</c>
        /// itself has no VACUUM-style transaction restriction; consistency is kept for uniformity/error isolation).
        /// If <c>amcheck</c> cannot be created (not installed on the server, or insufficient privilege to run
        /// <c>CREATE EXTENSION</c>), this returns a descriptive message explaining that instead of letting the
        /// underlying exception propagate uncontextualized - matching <c>PRAGMA integrity_check</c>/
        /// <c>DBCC CHECKDB</c> both returning descriptive text on failure rather than throwing. Whole-database, like
        /// <see cref="VacuumDatabase"/> - <paramref name="databaseName"/> is accepted only for interface
        /// compatibility.
        /// </summary>
        public async Task<string> IntegrityCheckDatabase(string databaseName)
        {
            var (context, connection) = await OpenAdminConnection();
            await using (context)
            {
                try
                {
                    try
                    {
                        using var createExtension = connection.CreateCommand();
                        createExtension.CommandText = "CREATE EXTENSION IF NOT EXISTS amcheck;";
                        await createExtension.ExecuteNonQueryAsync();
                    }
                    catch (Exception ex)
                    {
                        //Most commonly a PostgresException with SqlState 42501 (insufficient_privilege) if the
                        //connection's role isn't allowed to CREATE EXTENSION, or an "extension ... is not
                        //available" error if amcheck's control file isn't installed on the server at all - either
                        //way, return a message instead of an uncontextualized exception (see this method's remarks).
                        return "Integrity check skipped - the 'amcheck' extension is not available and could not "
                            + "be created on this PostgreSQL server (missing server-side installation, or the "
                            + $"connection's role lacks CREATE EXTENSION privilege). Underlying error: {ex.Message}";
                    }

                    var indexes = new List<(string Schema, string Table, string Index, long Oid)>();
                    var schemaList = string.Join(",", TargetSchemas.Select(s => $"'{s}'"));

                    using (var listCommand = connection.CreateCommand())
                    {
                        listCommand.CommandText = $"""
                            SELECT n.nspname, t.relname, i.relname, i.oid::bigint
                            FROM pg_index ix
                            JOIN pg_class i ON i.oid = ix.indexrelid
                            JOIN pg_class t ON t.oid = ix.indrelid
                            JOIN pg_namespace n ON n.oid = t.relnamespace
                            JOIN pg_am am ON am.oid = i.relam
                            WHERE am.amname = 'btree' AND n.nspname IN ({schemaList})
                            ORDER BY n.nspname, t.relname, i.relname;
                            """;
                        using var reader = await listCommand.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            indexes.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetInt64(3)));
                        }
                    }

                    var issues = new List<string>();
                    foreach (var (schema, table, index, oid) in indexes)
                    {
                        using var checkCommand = connection.CreateCommand();
                        checkCommand.CommandTimeout = 300; //bt_index_check can run long on larger indexes.
                        //oid comes from pg_catalog (system-generated, not user input) - safe to interpolate. The
                        //explicit ::regclass cast is required because bt_index_check's parameter type is regclass,
                        //not oid; the two are binary-compatible so the cast never fails for a valid oid.
                        checkCommand.CommandText = $"SELECT bt_index_check(index => {oid}::regclass, heapallindexed => true);";

                        try
                        {
                            await checkCommand.ExecuteNonQueryAsync();
                        }
                        catch (PostgresException ex)
                        {
                            issues.Add($"{schema}.{table} / {index}: {ex.MessageText}");
                        }
                    }

                    return issues.Count == 0
                        ? $"amcheck (bt_index_check) completed - no corruption found across {indexes.Count} B-tree "
                            + $"index(es) in {TargetSchemas.Length} schema(s). Note: this is a structural check of "
                            + "B-tree indexes only, not a full-database check - PostgreSQL has no DBCC CHECKDB equivalent."
                        : "amcheck (bt_index_check) reported issues:\r\n" + string.Join("\r\n", issues);
                }
                finally
                {
                    await context.Database.CloseConnectionAsync();
                }
            }
        }

        /// <summary>
        /// SQLite's <c>PRAGMA foreign_key_check</c> (<c>ForeignKeyCheck.sql</c>) lists rows that violate a foreign
        /// key; MSSQL's driver runs <c>DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS</c> for both foreign key and check
        /// constraints. The PostgreSQL equivalent queries <c>pg_constraint</c> directly for foreign key (<c>'f'</c>)
        /// and check (<c>'c'</c>) constraints whose <c>convalidated</c> flag is false - a constraint that exists but
        /// has never been (or is currently being) validated against existing data, PostgreSQL's own record of "this
        /// constraint might not actually hold for every row". Every <see cref="TargetSchemas"/> schema is covered by
        /// filtering on the constrained table's namespace. Because every constraint EF Core migrations create is
        /// created as <c>VALID</c> by default (<c>NOT VALID</c> is an opt-in migration option nothing in this
        /// codebase uses), the expected result against a normally-migrated database is always an empty list - this
        /// is not searching for "violations" the way <c>PRAGMA foreign_key_check</c>/<c>DBCC CHECKCONSTRAINTS</c> do
        /// (neither of which PostgreSQL's catalog can answer directly without literally re-scanning every row of
        /// every constrained table, which is what <c>ALTER TABLE ... VALIDATE CONSTRAINT</c> does) - it reports
        /// constraints PostgreSQL itself doesn't yet trust, which normally means none. Whole-database, like the
        /// other members - <paramref name="databaseName"/> is accepted only for interface compatibility.
        /// </summary>
        public async Task<string> ForeignKeyCheck(string databaseName)
        {
            var (context, connection) = await OpenAdminConnection();
            await using (context)
            {
                try
                {
                    var schemaList = string.Join(",", TargetSchemas.Select(s => $"'{s}'"));

                    using var command = connection.CreateCommand();
                    command.CommandText = $"""
                        SELECT n.nspname AS SchemaName, c.relname AS TableName, con.conname AS ConstraintName
                        FROM pg_constraint con
                        JOIN pg_class c ON c.oid = con.conrelid
                        JOIN pg_namespace n ON n.oid = con.connamespace
                        WHERE con.contype IN ('f','c') AND NOT con.convalidated AND n.nspname IN ({schemaList})
                        ORDER BY n.nspname, c.relname, con.conname;
                        """;

                    var violations = new List<string>();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            violations.Add($"{reader.GetString(0)}.{reader.GetString(1)} / {reader.GetString(2)}");
                        }
                    }

                    return violations.Count == 0
                        ? "No unvalidated foreign key or check constraints found (EF Core migrations create every "
                            + "constraint as VALID, so this is expected on a normally-migrated database)."
                        : "Unvalidated foreign key or check constraints found:\r\n" + string.Join("\r\n", violations);
                }
                finally
                {
                    await context.Database.CloseConnectionAsync();
                }
            }
        }

        /// <summary>
        /// SQLite's <c>GetDatabaseVersion.sql</c> (<c>SELECT SQLITE_VERSION();</c>) reports the same engine version
        /// for all 8 "databases" - there is no per-database/per-schema schema-version concept on that side. Like the
        /// MSSQL driver, the more informative and genuinely per-"database" analogue for the EF providers is each
        /// <see cref="Microsoft.EntityFrameworkCore.DbContext"/>'s own applied-migrations history - read from the
        /// two distinct <c>__EFMigrationsHistory_*</c> tables (<see cref="PostgresMigrationsHistory"/>), reporting
        /// the most recently applied <c>MigrationId</c> for each of the two contexts this driver manages
        /// (<c>ApplicationDbContext</c> for Identity, <see cref="TightWikiDbContext"/> for everything else).
        /// <c>MigrationId</c> is timestamp-prefixed (<c>yyyyMMddHHmmss_Name</c>), so ordering it as text is
        /// equivalent to ordering chronologically. Returns one row per <see cref="TargetSchemas"/> entry - the same
        /// 8 schema names <see cref="GetDatabasePageCounts"/>/<see cref="GetDatabasePageSizes"/> use - so that
        /// <c>AdminController.Database()</c>'s join across all three methods by schema name actually matches.
        /// </summary>
        /// <remarks>
        /// 7 of the 8 schemas (everything except "Users") are owned exclusively by <see cref="TightWikiDbContext"/>,
        /// so they all report the same value - the most recently applied <c>MigrationId</c> from
        /// <c>public.__EFMigrationsHistory_TightWikiDb</c>. "Users" is the one schema shared between
        /// <see cref="TightWikiDbContext"/> (which spans all 8 schemas) and
        /// <see cref="global::TightWiki.Library.ApplicationDbContext"/> (ASP.NET Core Identity) - each context has
        /// its own, independently versioned migration history, so that one row reports both rather than picking
        /// just one, formatted as <c>"TightWikiDb: &lt;migration&gt; / Identity: &lt;migration&gt;"</c> - same
        /// format as the MSSQL driver's.
        /// </remarks>
        public async Task<List<(string Name, string Version)>> GetDatabaseVersions()
        {
            var (context, connection) = await OpenAdminConnection();
            await using (context)
            {
                try
                {
                    var applicationDbVersion = await GetLatestMigrationId(connection,
                        PostgresMigrationsHistory.ApplicationDbSchema, PostgresMigrationsHistory.ApplicationDbTableName);
                    var tightWikiDbVersion = await GetLatestMigrationId(connection,
                        PostgresMigrationsHistory.TightWikiDbSchema, PostgresMigrationsHistory.TightWikiDbTableName);
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
            command.CommandText = $"SELECT \"MigrationId\" FROM \"{schema}\".\"{table}\" ORDER BY \"MigrationId\" DESC LIMIT 1;";
            var result = await command.ExecuteScalarAsync();
            return result as string ?? string.Empty;
        }

        /// <summary>
        /// PostgreSQL's on-disk page (block) size, queried at runtime via <c>SHOW block_size;</c> rather than
        /// hardcoded. Unlike SQL Server (a fixed architectural 8 KB constant on every edition/version, see
        /// <c>SqlServerDatabaseManager.SqlServerPageSizeBytes</c>), PostgreSQL's <c>block_size</c> is a
        /// compile-time server build option - it defaults to 8192 bytes for virtually every distributed build, but
        /// is not a guaranteed constant the way SQL Server's page size is, so it is queried here instead of assumed.
        /// Shared by <see cref="GetDatabasePageCounts"/> and <see cref="GetDatabasePageSizes"/>.
        /// </summary>
        private static async Task<int> GetBlockSize(DbConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SHOW block_size;";
            var result = await command.ExecuteScalarAsync();
            return int.Parse((string)result!);
        }

        /// <summary>
        /// SQLite's <c>PRAGMA page_count</c> (<c>GetDatabasePageCount.sql</c>) reports the total page count of a
        /// whole database file. Post-consolidation there is one physical PostgreSQL database, so - like the MSSQL
        /// driver - this reports the per-<b>schema</b> equivalent instead: for every schema in
        /// <see cref="TargetSchemas"/>, sums <c>pg_total_relation_size(oid)</c> (a table's heap + all its indexes +
        /// TOAST + free space map, the closest PostgreSQL analogue of "space this schema's data occupies on disk")
        /// across every regular table (<c>relkind = 'r'</c>) in that schema, then divides the byte total by
        /// <see cref="GetBlockSize"/> to turn it into a page count comparable to SQLite's/MSSQL's. <c>LEFT JOIN</c>
        /// so a schema with no tables yet still gets a row with <c>PageCount = 0</c>, keeping the "8 rows" shape
        /// <c>AdminController</c> expects.
        /// </summary>
        public async Task<List<(string Name, int PageCount)>> GetDatabasePageCounts()
        {
            var (context, connection) = await OpenAdminConnection();
            await using (context)
            {
                try
                {
                    var blockSize = await GetBlockSize(connection);
                    var schemaList = string.Join(",", TargetSchemas.Select(s => $"'{s}'"));

                    using var command = connection.CreateCommand();
                    command.CommandText = $"""
                        SELECT n.nspname AS SchemaName, COALESCE(SUM(pg_total_relation_size(c.oid)), 0) AS TotalBytes
                        FROM pg_namespace n
                        LEFT JOIN pg_class c ON c.relnamespace = n.oid AND c.relkind = 'r'
                        WHERE n.nspname IN ({schemaList})
                        GROUP BY n.nspname
                        ORDER BY n.nspname;
                        """;

                    var results = new List<(string, int)>();
                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var totalBytes = reader.GetInt64(1);
                        results.Add((reader.GetString(0), (int)(totalBytes / blockSize)));
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
        /// SQLite's <c>PRAGMA page_size</c> (<c>GetDatabasePageSize.sql</c>) is a per-database-file setting, queried
        /// at runtime. Unlike the MSSQL driver (which pairs every schema with a fixed 8 KB compile-time constant),
        /// PostgreSQL's page size is itself queried at runtime via <see cref="GetBlockSize"/> (see its doc comment
        /// for why it isn't hardcoded) and paired with each of the 8 <see cref="TargetSchemas"/> names, matching the
        /// "8 rows" shape <see cref="GetDatabasePageCounts"/> returns (so that <c>PageCount * PageSize</c>, as
        /// computed by <c>AdminController.Database()</c>, yields each schema's approximate on-disk size in bytes).
        /// </summary>
        public async Task<List<(string Name, int PageSize)>> GetDatabasePageSizes()
        {
            var (context, connection) = await OpenAdminConnection();
            await using (context)
            {
                try
                {
                    var blockSize = await GetBlockSize(connection);
                    return TargetSchemas.Select(name => (name, blockSize)).ToList();
                }
                finally
                {
                    await context.Database.CloseConnectionAsync();
                }
            }
        }

        #endregion
    }
}
