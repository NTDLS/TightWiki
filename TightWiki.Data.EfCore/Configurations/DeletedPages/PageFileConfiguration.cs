using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.DeletedPages;

namespace TightWiki.Data.EfCore.Configurations.DeletedPages
{
    /// <summary>
    /// Fluent configuration for <see cref="PageFile"/> (DeletedPages.PageFile).
    /// </summary>
    public class PageFileConfiguration : IEntityTypeConfiguration<PageFile>
    {
        public void Configure(EntityTypeBuilder<PageFile> builder)
        {
            builder.ToTable("PageFile", schema: "DeletedPages");

            builder.HasKey(e => e.Id);

            //Id is copied verbatim from the source file - not database-generated.
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.Property(e => e.Name)
                .IsRequired()
                .UseCollation("NOCASE");

            builder.Property(e => e.Navigation)
                .IsRequired()
                .UseCollation("NOCASE");

            //No unique index on (PageId, Name, Revision) here, unlike Pages.PageFile - verified via PRAGMA
            //index_list against the live Data/deletedpages.db.
        }
    }
}
