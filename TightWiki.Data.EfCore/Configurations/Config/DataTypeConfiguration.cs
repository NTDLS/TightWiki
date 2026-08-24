using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Config;

namespace TightWiki.Data.EfCore.Configurations.Config
{
    /// <summary>
    /// Fluent configuration for <see cref="DataType"/> (Config.DataType).
    /// </summary>
    public class DataTypeConfiguration : IEntityTypeConfiguration<DataType>
    {
        public void Configure(EntityTypeBuilder<DataType> builder)
        {
            builder.ToTable("DataType", schema: "Config");

            builder.HasKey(e => e.Id);

            //Nullable in the real schema - no NOT NULL constraint is declared for this column, only the
            //unique index below.
            builder.Property(e => e.Name)
                .UseCollation("NOCASE");

            builder.HasIndex(e => e.Name)
                .IsUnique();

            //Static lookup, seeded via HasData per Database-Providers-Plan.md chapter 4.6a ("Statické číselníky
            //-> HasData v migraci"). Unlike Severity/PermissionDisposition/Permission/Role, this table has no
            //CREATE TABLE/INSERT script anywhere under Scripts/Initialization/ - it is baseline schema+data
            //baked directly into the binary Data/config.db (Database-Providers-Plan.md chapter 2.1a), so the
            //values/Ids below were read directly from that live database (Integer=1, String=2, Boolean=3,
            //Decimal=4, Text=5) rather than from an init script.
            builder.HasData(
                new DataType { Id = 1, Name = "Integer" },
                new DataType { Id = 2, Name = "String" },
                new DataType { Id = 3, Name = "Boolean" },
                new DataType { Id = 4, Name = "Decimal" },
                new DataType { Id = 5, Name = "Text" });
        }
    }
}
