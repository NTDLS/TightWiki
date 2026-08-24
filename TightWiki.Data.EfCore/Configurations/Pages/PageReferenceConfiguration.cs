using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Pages;

namespace TightWiki.Data.EfCore.Configurations.Pages
{
    /// <summary>
    /// Fluent configuration for <see cref="PageReference"/> (Pages.PageReference).
    /// </summary>
    /// <remarks>
    /// Both relationships below target <see cref="Entities.Pages.Page"/> - a self-referencing association within
    /// the Pages schema (a page's outgoing references point back to other pages), not a cross-schema navigation.
    /// </remarks>
    public class PageReferenceConfiguration : IEntityTypeConfiguration<PageReference>
    {
        public void Configure(EntityTypeBuilder<PageReference> builder)
        {
            builder.ToTable("PageReference", schema: "Pages");

            builder.HasKey(e => new { e.PageId, e.ReferencesPageNavigation });

            builder.Property(e => e.ReferencesPageName)
                .IsRequired()
                .UseCollation("NOCASE");

            builder.Property(e => e.ReferencesPageNavigation).UseCollation("NOCASE");

            builder.HasOne(e => e.Page)
                .WithMany(e => e.PageReferencePages)
                .HasForeignKey(e => e.PageId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            builder.HasOne(e => e.ReferencesPage)
                .WithMany(e => e.PageReferenceReferencesPages)
                .HasForeignKey(e => e.ReferencesPageId);
        }
    }
}
