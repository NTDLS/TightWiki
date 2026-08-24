using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TightWiki.Data.EfCore.Postgres
{
    /// <summary>
    /// Design-time factory so that "dotnet ef migrations add"/"dotnet ef database update" can construct a
    /// <see cref="TightWikiDbContext"/> for this driver project without running the full TightWiki host
    /// (Database-Providers-Plan.md chapter 5, step 4: "Vyžaduje IDesignTimeDbContextFactory&lt;TightWikiDbContext&gt;
    /// v každém driver-projektu, jinak si dotnet ef bere connection string ze startup projektu"). Direct analogue
    /// of <c>TightWiki.Data.EfCore.SqlServer.TightWikiDbContextFactory</c>.
    /// </summary>
    public class TightWikiDbContextFactory : IDesignTimeDbContextFactory<TightWikiDbContext>
    {
        public TightWikiDbContext CreateDbContext(string[] args)
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

            var optionsBuilder = new DbContextOptionsBuilder<TightWikiDbContext>();
            //TightWikiDbContext itself lives in TightWiki.Data.EfCore (shared with every EF driver project), but
            //its PostgreSQL migrations belong in this driver project - MigrationsAssembly has to be pointed here
            //explicitly, or "dotnet ef" defaults to (and "dotnet ef database update" at runtime would look for
            //its __EFMigrationsHistory rows in) the context's own assembly.
            //MigrationsHistoryTable is likewise explicit and distinct from ApplicationDbContext's - see
            //PostgresMigrationsHistory for why two DbContexts over one database must not share EF Core's
            //default public.__EFMigrationsHistory table.
            optionsBuilder.UseNpgsql(connectionString,
                b => b.MigrationsAssembly(typeof(TightWikiDbContextFactory).Assembly.GetName().Name)
                      .MigrationsHistoryTable(PostgresMigrationsHistory.TightWikiDbTableName, PostgresMigrationsHistory.TightWikiDbSchema));

            return new TightWikiDbContext(optionsBuilder.Options);
        }
    }
}
