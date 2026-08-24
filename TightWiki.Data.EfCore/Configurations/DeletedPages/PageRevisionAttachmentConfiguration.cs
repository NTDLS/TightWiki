using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.DeletedPages;

namespace TightWiki.Data.EfCore.Configurations.DeletedPages
{
    /// <summary>
    /// Fluent configuration for <see cref="PageRevisionAttachment"/> (DeletedPages.PageRevisionAttachment).
    /// </summary>
    public class PageRevisionAttachmentConfiguration : IEntityTypeConfiguration<PageRevisionAttachment>
    {
        public void Configure(EntityTypeBuilder<PageRevisionAttachment> builder)
        {
            builder.ToTable("PageRevisionAttachment", schema: "DeletedPages");

            builder.HasKey(e => new { e.PageId, e.PageFileId, e.FileRevision, e.PageRevision });

            //Unlike Pages.PageRevisionAttachment, no additional UNIQUE(PageId, PageFileId, PageRevision) index
            //exists here - verified via PRAGMA index_list against the live Data/deletedpages.db.
        }
    }
}
