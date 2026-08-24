using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Pages;

namespace TightWiki.Data.EfCore.Configurations.Pages
{
    /// <summary>
    /// Fluent configuration for <see cref="PageTag"/> (Pages.PageTag).
    /// </summary>
    public class PageTagConfiguration : IEntityTypeConfiguration<PageTag>
    {
        public void Configure(EntityTypeBuilder<PageTag> builder)
        {
            builder.ToTable("PageTag", schema: "Pages");

            builder.HasKey(e => new { e.PageId, e.Tag });

            builder.Property(e => e.Tag).UseCollation("NOCASE");

            builder.Property(e => e.Navigation)
                .IsRequired()
                .HasDefaultValue("")
                .UseCollation("NOCASE");

            builder.HasOne(e => e.Page)
                .WithMany(e => e.PageTags)
                .HasForeignKey(e => e.PageId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }
}
