using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.DeletedPages;

namespace TightWiki.Data.EfCore.Configurations.DeletedPages
{
    /// <summary>
    /// Fluent configuration for <see cref="PageToken"/> (DeletedPages.PageToken).
    /// </summary>
    public class PageTokenConfiguration : IEntityTypeConfiguration<PageToken>
    {
        public void Configure(EntityTypeBuilder<PageToken> builder)
        {
            builder.ToTable("PageToken", schema: "DeletedPages");

            builder.HasKey(e => new { e.PageId, e.Token });

            builder.Property(e => e.Token).UseCollation("NOCASE");

            builder.Property(e => e.DoubleMetaphone)
                .IsRequired()
                .UseCollation("NOCASE");

            //Unlike Pages.PageToken, no additional search-ranking indexes (DoubleMetaphone/Token/PageId
            //composites) exist here - verified via PRAGMA index_list against the live Data/deletedpages.db.
        }
    }
}
