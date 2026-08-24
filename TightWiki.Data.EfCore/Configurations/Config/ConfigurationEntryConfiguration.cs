using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Config;

namespace TightWiki.Data.EfCore.Configurations.Config
{
    /// <summary>
    /// Fluent configuration for <see cref="ConfigurationEntry"/> (Config.ConfigurationEntry).
    /// </summary>
    public class ConfigurationEntryConfiguration : IEntityTypeConfiguration<ConfigurationEntry>
    {
        public void Configure(EntityTypeBuilder<ConfigurationEntry> builder)
        {
            builder.ToTable("ConfigurationEntry", schema: "Config");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Name)
                .IsRequired()
                .UseCollation("NOCASE");

            builder.Property(e => e.Description)
                .UseCollation("NOCASE");

            //Unique together, matches the real UNIQUE (ConfigurationGroupId, Name) table constraint.
            builder.HasIndex(e => new { e.ConfigurationGroupId, e.Name })
                .IsUnique();

            //No FK constraint exists in the real schema for ConfigurationGroupId/DataTypeId - intentionally
            //not modeled as a navigation/relationship here.
        }
    }
}
