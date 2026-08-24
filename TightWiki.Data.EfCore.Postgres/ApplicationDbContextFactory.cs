using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using TightWiki.Library;

namespace TightWiki.Data.EfCore.Postgres
{
    /// <summary>
    /// Design-time factory so that "dotnet ef migrations add"/"dotnet ef database update" can construct an
    /// <see cref="ApplicationDbContext"/> (ASP.NET Core Identity, see Database-Providers-Plan.md chapter 4.1.1)
    /// for this driver project without running the full TightWiki host - the Identity-model counterpart to
    /// <see cref="TightWikiDbContextFactory"/>. Uses the same <c>ConnectionStrings:TightWikiEfCore</c> key and
    /// provider as the rest of this driver project (chapter 7, "Rozhodnuto": "ApplicationDbContext používá
    /// stejný connection string ... a stejného providera jako aktivní driver"). Direct analogue of
    /// <c>TightWiki.Data.EfCore.SqlServer.ApplicationDbContextFactory</c>.
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            //Mirrors how the wider solution resolves configuration (see TightWiki/appsettings.json) but reads
            //it from this project's own output directory, since "dotnet ef" runs against this project rather
            //than the TightWiki web app.
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            //The single new configuration key introduced for the EF Core providers - see
            //Database-Providers-Plan.md chapter 7 ("Rozhodnuto") and chapter 4.2.
            var connectionString = configuration.GetConnectionString("TightWikiEfCore")
                ?? throw new InvalidOperationException(
                    "Missing connection string 'ConnectionStrings:TightWikiEfCore'. This is required to run " +
                    "'dotnet ef' commands against TightWiki.Data.EfCore.Postgres.");

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            //ApplicationDbContext itself lives in TightWiki.Library (shared with the SQLite driver), but its
            //PostgreSQL migrations belong in this driver project, not in TightWiki.Library - MigrationsAssembly
            //has to be pointed here explicitly, or "dotnet ef" defaults to (and "dotnet ef database update"
            //at runtime would look for its __EFMigrationsHistory rows in) the context's own assembly.
            //MigrationsHistoryTable is likewise explicit and distinct from TightWikiDbContext's - see the
            //matching comment on PostgresDatabaseManager.CreateApplicationDbContext for why two DbContexts
            //over one database must not share EF Core's default public.__EFMigrationsHistory table.
            optionsBuilder.UseNpgsql(connectionString,
                b => b.MigrationsAssembly(typeof(ApplicationDbContextFactory).Assembly.GetName().Name)
                      .MigrationsHistoryTable(PostgresMigrationsHistory.ApplicationDbTableName, PostgresMigrationsHistory.ApplicationDbSchema));

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
