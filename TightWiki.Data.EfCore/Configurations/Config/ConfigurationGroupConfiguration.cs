using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Config;

namespace TightWiki.Data.EfCore.Configurations.Config
{
    /// <summary>
    /// Fluent configuration for <see cref="ConfigurationGroup"/> (Config.ConfigurationGroup).
    /// </summary>
    public class ConfigurationGroupConfiguration : IEntityTypeConfiguration<ConfigurationGroup>
    {
        public void Configure(EntityTypeBuilder<ConfigurationGroup> builder)
        {
            builder.ToTable("ConfigurationGroup", schema: "Config");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Name)
                .IsRequired()
                .UseCollation("NOCASE");

            builder.Property(e => e.Description)
                .UseCollation("NOCASE");

            builder.HasIndex(e => e.Name)
                .IsUnique();
        }
    }
}
