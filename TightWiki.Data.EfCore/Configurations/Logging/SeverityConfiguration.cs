using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Logging;

namespace TightWiki.Data.EfCore.Configurations.Logging
{
    /// <summary>
    /// Fluent configuration for <see cref="Severity"/> (Logging.Severity).
    /// </summary>
    public class SeverityConfiguration : IEntityTypeConfiguration<Severity>
    {
        public void Configure(EntityTypeBuilder<Severity> builder)
        {
            builder.ToTable("Severity", schema: "Logging");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Name)
                .IsRequired()
                .UseCollation("NOCASE");

            builder.HasIndex(e => e.Name)
                .IsUnique();

            //Static lookup, seeded via HasData per Database-Providers-Plan.md chapter 4.6a ("Statické číselníky
            //-> HasData v migraci"). Source of truth: Scripts/CreateSeverityTable.sql, a plain multi-row INSERT
            //(not UNION SELECT, so SQLite preserves this literal order) - values/Ids cross-checked against the
            //live Data/logging.db (Trace=1 .. None=7).
            builder.HasData(
                new Severity { Id = 1, Name = "Trace" },
                new Severity { Id = 2, Name = "Debug" },
                new Severity { Id = 3, Name = "Information" },
                new Severity { Id = 4, Name = "Warning" },
                new Severity { Id = 5, Name = "Error" },
                new Severity { Id = 6, Name = "Critical" },
                new Severity { Id = 7, Name = "None" });
        }
    }
}
