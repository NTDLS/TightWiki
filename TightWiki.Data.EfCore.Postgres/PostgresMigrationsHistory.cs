namespace TightWiki.Data.EfCore.Postgres
{
    /// <summary>
    /// Single source of truth for the (deliberately distinct) <c>MigrationsHistoryTable</c> name/schema used by
    /// each of the two <see cref="Microsoft.EntityFrameworkCore.DbContext"/>s this driver project migrates
    /// against the same PostgreSQL database (<c>ApplicationDbContext</c> and <c>TightWikiDbContext</c>). Direct
    /// analogue of <c>TightWiki.Data.EfCore.SqlServer.SqlServerMigrationsHistory</c> - see that class's doc
    /// comment for the full rationale, which applies unchanged here.
    /// </summary>
    /// <remarks>
    /// EF Core's default <c>MigrationsHistoryTable</c> is <c>__EFMigrationsHistory</c> (in the provider's default
    /// schema) for every <see cref="Microsoft.EntityFrameworkCore.DbContext"/> that doesn't override it. With two
    /// independently versioned <c>DbContext</c>s pointed at one database, sharing that default is a documented EF
    /// Core anti-pattern: <c>MigrationId</c> (a second-granularity timestamp + migration class name) can collide
    /// between the two contexts' migration histories - exactly the risk when migrations for both are generated in
    /// the same run (Database-Providers-Plan.md chapter 5). Every place that builds a
    /// <see cref="Microsoft.EntityFrameworkCore.DbContextOptionsBuilder"/> for either context (this project's
    /// design-time factories added alongside its EF Core migrations, <see cref="PostgresDatabaseManager"/>, and
    /// <c>TightWiki/Program.cs</c>'s eventual <c>POSTGRES_PROVIDER</c> branch) must reference these same
    /// constants, or the design-time tool and the runtime host would disagree about which table records which
    /// context's applied migrations.
    /// </remarks>
    public static class PostgresMigrationsHistory
    {
        /// <summary>
        /// <see cref="global::TightWiki.Library.ApplicationDbContext"/> (ASP.NET Core Identity) already lives in
        /// the "Users" schema (Database-Providers-Plan.md chapter 4.1.1, <c>ApplicationDbContext.OnModelCreating</c>'s
        /// <c>HasDefaultSchema("Users")</c> - unaffected by which relational provider is active), so its
        /// migrations-history table stays alongside it rather than in Postgres's default "public" schema. Same
        /// convention as <c>SqlServerMigrationsHistory.ApplicationDbSchema</c>.
        /// </summary>
        public const string ApplicationDbTableName = "__EFMigrationsHistory_ApplicationDb";
        public const string ApplicationDbSchema = "Users";

        /// <summary>
        /// <see cref="TightWikiDbContext"/> spans all 8 TightWiki schemas (Config, Pages, Users, Statistics,
        /// Emoji, Logging, DeletedPages, DeletedPageRevisions - chapter 4.3) with no single schema of its own, so
        /// its migrations-history table is left in Postgres's default "public" schema - the Postgres analogue of
        /// SQL Server's default "dbo" schema that <c>SqlServerMigrationsHistory.TightWikiDbTableName</c> relies
        /// on - distinguished from <see cref="ApplicationDbTableName"/> by name alone.
        /// </summary>
        public const string TightWikiDbTableName = "__EFMigrationsHistory_TightWikiDb";
        public const string TightWikiDbSchema = "public";
    }
}
