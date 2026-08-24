using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Pages;

namespace TightWiki.Data.EfCore.Configurations.Pages
{
    /// <summary>
    /// Fluent configuration for <see cref="PageRevisionAttachment"/> (Pages.PageRevisionAttachment).
    /// </summary>
    public class PageRevisionAttachmentConfiguration : IEntityTypeConfiguration<PageRevisionAttachment>
    {
        public void Configure(EntityTypeBuilder<PageRevisionAttachment> builder)
        {
            builder.ToTable("PageRevisionAttachment", schema: "Pages");

            builder.HasKey(e => new { e.PageId, e.PageFileId, e.FileRevision, e.PageRevision });

            //Genuinely distinct from the primary key (a 3-column subset omitting FileRevision) - enforces that
            //only one file revision may be attached per page/file/page-revision combination. Not redundant, kept
            //as-is.
            builder.HasIndex(e => new { e.PageId, e.PageFileId, e.PageRevision },
                    "IX_PageRevisionAttachment_PageId_PageFileId_PageRevision")
                .IsUnique();

            builder.HasOne(e => e.Page)
                .WithMany(e => e.PageRevisionAttachments)
                .HasForeignKey(e => e.PageId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            builder.HasOne(e => e.PageFile)
                .WithMany(e => e.PageRevisionAttachments)
                .HasForeignKey(e => e.PageFileId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }
}
