using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.DeletedPages;

namespace TightWiki.Data.EfCore.Configurations.DeletedPages
{
    /// <summary>
    /// Fluent configuration for <see cref="PageTag"/> (DeletedPages.PageTag).
    /// </summary>
    public class PageTagConfiguration : IEntityTypeConfiguration<PageTag>
    {
        public void Configure(EntityTypeBuilder<PageTag> builder)
        {
            builder.ToTable("PageTag", schema: "DeletedPages");

            builder.HasKey(e => new { e.PageId, e.Tag });

            builder.Property(e => e.Tag).UseCollation("NOCASE");
        }
    }
}
