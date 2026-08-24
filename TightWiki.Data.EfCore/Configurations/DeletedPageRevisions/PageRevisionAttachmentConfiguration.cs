using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.DeletedPageRevisions;

namespace TightWiki.Data.EfCore.Configurations.DeletedPageRevisions
{
    /// <summary>
    /// Fluent configuration for <see cref="PageRevisionAttachment"/> (DeletedPageRevisions.
    /// PageRevisionAttachment).
    /// </summary>
    public class PageRevisionAttachmentConfiguration : IEntityTypeConfiguration<PageRevisionAttachment>
    {
        public void Configure(EntityTypeBuilder<PageRevisionAttachment> builder)
        {
            builder.ToTable("PageRevisionAttachment", schema: "DeletedPageRevisions");

            builder.HasKey(e => new { e.PageId, e.PageFileId, e.FileRevision, e.PageRevision });
        }
    }
}
