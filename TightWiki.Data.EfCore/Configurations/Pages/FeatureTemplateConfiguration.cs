using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Pages;

namespace TightWiki.Data.EfCore.Configurations.Pages
{
    /// <summary>
    /// Fluent configuration for <see cref="FeatureTemplate"/> (Pages.FeatureTemplate).
    /// </summary>
    /// <remarks>
    /// Source of truth: Scripts/Initialization/Versions/2.23.1/^001^Pages^FeatureTemplate.sql (this table has a
    /// real CREATE TABLE script, unlike most of the Pages schema).
    /// </remarks>
    public class FeatureTemplateConfiguration : IEntityTypeConfiguration<FeatureTemplate>
    {
        public void Configure(EntityTypeBuilder<FeatureTemplate> builder)
        {
            builder.ToTable("FeatureTemplate", schema: "Pages");

            builder.HasKey(e => new { e.Name, e.Type });

            builder.Property(e => e.Name)
                .IsRequired()
                .UseCollation("NOCASE");

            //Nullable in the real schema despite being part of the primary key (SQLite allows this for
            //non-INTEGER composite keys) - EF requires key properties to be non-nullable, so this is modeled as
            //a plain required string, matching the reference scaffold.
            builder.Property(e => e.Type).UseCollation("NOCASE");

            builder.HasOne(e => e.Page)
                .WithMany(e => e.FeatureTemplates)
                .HasForeignKey(e => e.PageId);
        }
    }
}
