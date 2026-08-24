using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Config;

namespace TightWiki.Data.EfCore.Configurations.Config
{
    /// <summary>
    /// Fluent configuration for <see cref="VersionState"/> (Config.VersionState).
    /// </summary>
    public class VersionStateConfiguration : IEntityTypeConfiguration<VersionState>
    {
        public void Configure(EntityTypeBuilder<VersionState> builder)
        {
            builder.ToTable("VersionState", schema: "Config");

            builder.HasKey(e => e.Id);

            //No COLLATE NOCASE on this column in the real schema.
            builder.Property(e => e.Name).IsRequired();

            builder.HasIndex(e => e.Name).IsUnique();

            builder.Property(e => e.Value).IsRequired();
        }
    }
}
