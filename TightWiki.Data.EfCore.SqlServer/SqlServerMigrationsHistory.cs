namespace TightWiki.Data.EfCore.SqlServer
{
    /// <summary>
    /// Single source of truth for the (deliberately distinct) <c>MigrationsHistoryTable</c> name/schema used by
    /// each of the two <see cref="Microsoft.EntityFrameworkCore.DbContext"/>s this driver project migrates
    /// against the same MSSQL database (<c>ApplicationDbContext</c> and <c>TightWikiDbContext</c>).
    /// </summary>
    /// <remarks>
    /// EF Core's default <c>MigrationsHistoryTable</c> is <c>dbo.__EFMigrationsHistory</c> for every
    /// <see cref="Microsoft.EntityFrameworkCore.DbContext"/> that doesn't override it. With two independently
    /// versioned <c>DbContext</c>s pointed at one database, sharing that default is a documented EF Core
    /// anti-pattern: <c>MigrationId</c> (a second-granularity timestamp + migration class name) can collide
    /// between the two contexts' migration histories, which is exactly the risk when migrations for both are
    /// generated in the same run by <c>Generate-EfMigrations.ps1</c> (Database-Providers-Plan.md chapter 5).
    /// Every place that builds a <see cref="Microsoft.EntityFrameworkCore.DbContextOptionsBuilder"/> for either
    /// context (this project's two <c>IDesignTimeDbContextFactory</c> implementations,
    /// <see cref="SqlServerDatabaseManager"/>, and <c>TightWiki/Program.cs</c>'s <c>SQLSERVER_PROVIDER</c>
    /// branch) must reference these same two constants, or the design-time tool and the runtime host would
    /// disagree about which table records which context's applied migrations.
    /// </remarks>
    public static class SqlServerMigrationsHistory
    {
        /// <summary>
        /// <see cref="global::TightWiki.Library.ApplicationDbContext"/> (ASP.NET Core Identity) already lives in
        /// the "Users" schema (Database-Providers-Plan.md chapter 4.1.1), so its migrations-history table stays
        /// alongside it rather than in the default "dbo" schema.
        /// </summary>
        public const string ApplicationDbTableName = "__EFMigrationsHistory_ApplicationDb";
        public const string ApplicationDbSchema = "Users";

        /// <summary>
        /// <see cref="TightWikiDbContext"/> spans all 8 TightWiki schemas (Config, Pages, Users, Statistics,
        /// Emoji, Logging, DeletedPages, DeletedPageRevisions - chapter 4.3) with no single schema of its own, so
        /// its migrations-history table is left in the default "dbo" schema, distinguished from
        /// <see cref="ApplicationDbTableName"/> by name alone.
        /// </summary>
        public const string TightWikiDbTableName = "__EFMigrationsHistory_TightWikiDb";
    }
}
