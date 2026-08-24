using Microsoft.EntityFrameworkCore;
using ConfigEntities = TightWiki.Data.EfCore.Entities.Config;
using DeletedPageRevisionsEntities = TightWiki.Data.EfCore.Entities.DeletedPageRevisions;
using DeletedPagesEntities = TightWiki.Data.EfCore.Entities.DeletedPages;
using EmojiEntities = TightWiki.Data.EfCore.Entities.Emoji;
using LoggingEntities = TightWiki.Data.EfCore.Entities.Logging;
using PagesEntities = TightWiki.Data.EfCore.Entities.Pages;
using StatisticsEntities = TightWiki.Data.EfCore.Entities.Statistics;
using UsersEntities = TightWiki.Data.EfCore.Entities.Users;

namespace TightWiki.Data.EfCore
{
    /// <summary>
    /// Provider-agnostic shared EF Core model for TightWiki. Consolidates the 8 physically separate SQLite
    /// databases (Config, DeletedPageRevisions, DeletedPages, Emoji, Logging, Pages, Statistics, Users) into one
    /// logical database with 8 schemas, per Database-Providers-Plan.md chapter 4.3 - <b>not</b> 8 independent
    /// islands, hence the cross-schema navigations declared on some of the entities below (see e.g.
    /// <see cref="PagesEntities.Page.CreatedByUser"/> / <see cref="UsersEntities.Profile.Pages_CreatedPages"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>DbSet</c> property names are the plain pluralized entity name (matching the <c>_Scaffold/</c>
    /// reference dump's convention) unless that name would collide across schemas - e.g. both Pages and
    /// DeletedPages declare a <c>Page</c>/<c>PageComment</c>/etc. entity - in which case the property is
    /// prefixed with the schema name (<c>DeletedPages_Pages</c>, <c>Pages_PageComments</c>, ...), matching the
    /// same disambiguation convention used for the reverse-navigation collections on
    /// <see cref="UsersEntities.Profile"/>.
    /// </para>
    /// <para>
    /// Fluent configuration is not hand-wired per entity here - <see cref="OnModelCreating"/> applies every
    /// <see cref="Microsoft.EntityFrameworkCore.IEntityTypeConfiguration{TEntity}"/> in this assembly
    /// (the <c>Configurations/</c> folder) via <c>ApplyConfigurationsFromAssembly</c>, the idiomatic EF Core way
    /// to register dozens of configuration classes without listing them one by one.
    /// </para>
    /// </remarks>
    public class TightWikiDbContext : DbContext
    {
        /// <summary>
        /// Accepts the non-generic <see cref="DbContextOptions"/> (rather than
        /// <see cref="DbContextOptions{TightWikiDbContext}"/>) so that per-provider driver projects can configure
        /// this same shared context - each driver project calls <c>UseSqlServer</c>/<c>UseNpgsql</c> against a
        /// plain <see cref="DbContextOptionsBuilder"/>, matching the pattern used by
        /// <see cref="Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext{TUser}"/> for the same
        /// reason. <c>Microsoft.Extensions.DependencyInjection</c>'s <c>AddDbContext&lt;TContext&gt;</c> supports
        /// either constructor shape.
        /// </summary>
        public TightWikiDbContext(DbContextOptions options)
            : base(options)
        {
        }

        //Config schema (7 entities) - no cross-schema navigations.
        public DbSet<ConfigEntities.ConfigurationEntry> ConfigurationEntries { get; set; } = null!;
        public DbSet<ConfigEntities.ConfigurationGroup> ConfigurationGroups { get; set; } = null!;
        public DbSet<ConfigEntities.CryptoCheck> CryptoChecks { get; set; } = null!;
        public DbSet<ConfigEntities.DataType> DataTypes { get; set; } = null!;
        public DbSet<ConfigEntities.MenuItem> MenuItems { get; set; } = null!;
        public DbSet<ConfigEntities.Theme> Themes { get; set; } = null!;
        public DbSet<ConfigEntities.VersionState> VersionStates { get; set; } = null!;

        //Emoji schema (2 entities) - no cross-schema navigations.
        public DbSet<EmojiEntities.Emoji> Emojis { get; set; } = null!;
        public DbSet<EmojiEntities.EmojiCategory> EmojiCategories { get; set; } = null!;

        //Logging schema (2 entities) - no cross-schema navigations.
        public DbSet<LoggingEntities.Log> Logs { get; set; } = null!;
        public DbSet<LoggingEntities.Severity> Severities { get; set; } = null!;

        //Statistics schema (1 entity) - PageStatistic has a cross-schema navigation to Pages.Page.
        public DbSet<StatisticsEntities.PageStatistic> PageStatistics { get; set; } = null!;

        //Users schema (8 entities) - Profile carries the reverse side of every cross-schema *UserId navigation
        //declared on the Pages/DeletedPages/DeletedPageRevisions entities below.
        public DbSet<UsersEntities.AccountPermission> AccountPermissions { get; set; } = null!;
        public DbSet<UsersEntities.AccountRole> AccountRoles { get; set; } = null!;
        public DbSet<UsersEntities.AdminPwCheck> AdminPwChecks { get; set; } = null!;
        public DbSet<UsersEntities.Permission> Permissions { get; set; } = null!;
        public DbSet<UsersEntities.PermissionDisposition> PermissionDispositions { get; set; } = null!;
        public DbSet<UsersEntities.Profile> Profiles { get; set; } = null!;
        public DbSet<UsersEntities.Role> Roles { get; set; } = null!;
        public DbSet<UsersEntities.RolePermission> RolePermissions { get; set; } = null!;

        //Pages schema (12 entities). CurrentPageEditor/FeatureTemplate/PageReference are unique names and need
        //no prefix; the rest collide with DeletedPages (and, for PageRevision/PageRevisionAttachment, also with
        //DeletedPageRevisions), hence the "Pages_" prefix.
        public DbSet<PagesEntities.CurrentPageEditor> CurrentPageEditors { get; set; } = null!;
        public DbSet<PagesEntities.FeatureTemplate> FeatureTemplates { get; set; } = null!;
        public DbSet<PagesEntities.PageReference> PageReferences { get; set; } = null!;
        public DbSet<PagesEntities.Page> Pages_Pages { get; set; } = null!;
        public DbSet<PagesEntities.PageComment> Pages_PageComments { get; set; } = null!;
        public DbSet<PagesEntities.PageFile> Pages_PageFiles { get; set; } = null!;
        public DbSet<PagesEntities.PageFileRevision> Pages_PageFileRevisions { get; set; } = null!;
        public DbSet<PagesEntities.PageProcessingInstruction> Pages_PageProcessingInstructions { get; set; } = null!;
        public DbSet<PagesEntities.PageRevision> Pages_PageRevisions { get; set; } = null!;
        public DbSet<PagesEntities.PageRevisionAttachment> Pages_PageRevisionAttachments { get; set; } = null!;
        public DbSet<PagesEntities.PageTag> Pages_PageTags { get; set; } = null!;
        public DbSet<PagesEntities.PageToken> Pages_PageTokens { get; set; } = null!;

        //DeletedPages schema (10 entities). Every entity name here collides with either Pages or
        //DeletedPageRevisions, so all are prefixed with "DeletedPages_".
        public DbSet<DeletedPagesEntities.DeletionMeta> DeletedPages_DeletionMetas { get; set; } = null!;
        public DbSet<DeletedPagesEntities.Page> DeletedPages_Pages { get; set; } = null!;
        public DbSet<DeletedPagesEntities.PageComment> DeletedPages_PageComments { get; set; } = null!;
        public DbSet<DeletedPagesEntities.PageFile> DeletedPages_PageFiles { get; set; } = null!;
        public DbSet<DeletedPagesEntities.PageFileRevision> DeletedPages_PageFileRevisions { get; set; } = null!;
        public DbSet<DeletedPagesEntities.PageProcessingInstruction> DeletedPages_PageProcessingInstructions { get; set; } = null!;
        public DbSet<DeletedPagesEntities.PageRevision> DeletedPages_PageRevisions { get; set; } = null!;
        public DbSet<DeletedPagesEntities.PageRevisionAttachment> DeletedPages_PageRevisionAttachments { get; set; } = null!;
        public DbSet<DeletedPagesEntities.PageTag> DeletedPages_PageTags { get; set; } = null!;
        public DbSet<DeletedPagesEntities.PageToken> DeletedPages_PageTokens { get; set; } = null!;

        //DeletedPageRevisions schema (3 entities). All three names collide with Pages/DeletedPages, so all are
        //prefixed with "DeletedPageRevisions_".
        public DbSet<DeletedPageRevisionsEntities.DeletionMeta> DeletedPageRevisions_DeletionMetas { get; set; } = null!;
        public DbSet<DeletedPageRevisionsEntities.PageRevision> DeletedPageRevisions_PageRevisions { get; set; } = null!;
        public DbSet<DeletedPageRevisionsEntities.PageRevisionAttachment> DeletedPageRevisions_PageRevisionAttachments { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(TightWikiDbContext).Assembly);

            StripNonSqliteNoCaseCollation(modelBuilder);
        }

        /// <summary>
        /// Every <see cref="Microsoft.EntityFrameworkCore.Metadata.Builders.IEntityTypeConfiguration{TEntity}"/>
        /// under <c>Configurations/</c> unconditionally calls <c>.UseCollation("NOCASE")</c> on the columns that
        /// carry SQLite's case-insensitive collation today - that stays a correct, literal port for a future
        /// SQLite-EF driver (Database-Providers-Plan.md chapter 4.5: "Kolace - .UseCollation("NOCASE") je
        /// legitimní volba i pro SQLite provider"). But <c>NOCASE</c> is a SQLite-only collation name - it does
        /// not exist on SQL Server/Postgres, and a migration/runtime call would fail there.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Per chapter 4.4 ("MSSQL má CI collation obvykle jako DB default") and the open question in chapter 8,
        /// the chosen approach for non-SQLite providers is to <b>not</b> set an explicit collation at all and
        /// rely on the server/database default collation being case-insensitive (LocalDB and a stock SQL Server
        /// install both default to <c>SQL_Latin1_General_CP1_CI_AS</c>). Rather than sprinkling per-provider
        /// checks across ~30 configuration files (one per entity), the switch is centralized here: every
        /// <c>NOCASE</c> collation applied above is stripped again unless the active provider is SQLite. This
        /// is deliberately phrased as "keep it only for Sqlite", not "strip it for SqlServer" - so a future
        /// Postgres driver (phase 3) inherits "no NOCASE" automatically and can layer its own citext/lower()
        /// convention on top later without this method needing another provider-specific branch.
        /// </para>
        /// <para>
        /// Phase 3 (Postgres) is that layered-on-top branch: Npgsql doesn't know the <c>NOCASE</c> collation
        /// name either, so it is stripped the same as for SqlServer, but on top of that every stripped
        /// <c>string</c> column is switched to the <c>citext</c> column type - Postgres's case-insensitive text
        /// type, the closest match to SQLite's <c>NOCASE</c> semantics (the extension itself is enabled by
        /// <c>PostgresDatabaseManager.InitializeSchema</c>, not here - this project has no Npgsql package
        /// reference and must not gain one; <c>SetColumnType</c> is plain
        /// <c>Microsoft.EntityFrameworkCore.Relational</c> API). Two of the 49 <c>NOCASE</c> columns are
        /// <see cref="Guid"/>, not <c>string</c> (<c>Profile.UserId</c>, <c>AccountRole.UserId</c> - SQLite
        /// stores <see cref="Guid"/> as TEXT, hence <c>NOCASE</c> made sense there) - <c>citext</c> is a text
        /// type, so those are deliberately excluded via the <c>ClrType == typeof(string)</c> check below.
        /// </para>
        /// </remarks>
        private void StripNonSqliteNoCaseCollation(ModelBuilder modelBuilder)
        {
            if (Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
            {
                return; //SQLite (present or future TightWiki.Data.EfCore.Sqlite driver) keeps NOCASE as configured.
            }

            var isNpgsql = Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.GetCollation() == "NOCASE")
                    {
                        property.SetCollation(null);

                        if (isNpgsql && property.ClrType == typeof(string))
                        {
                            property.SetColumnType("citext");
                        }
                    }
                }
            }
        }
    }
}
