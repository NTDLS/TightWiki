using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Pages;

namespace TightWiki.Data.EfCore.Configurations.Pages
{
    /// <summary>
    /// Fluent configuration for <see cref="PageFile"/> (Pages.PageFile).
    /// </summary>
    public class PageFileConfiguration : IEntityTypeConfiguration<PageFile>
    {
        public void Configure(EntityTypeBuilder<PageFile> builder)
        {
            builder.ToTable("PageFile", schema: "Pages");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Name)
                .IsRequired()
                .UseCollation("NOCASE");

            builder.Property(e => e.Navigation)
                .IsRequired()
                .UseCollation("NOCASE");

            //The real schema declares two unique indexes on the exact same 3-column set (PageId, Name,
            //Revision) - an inline UNIQUE("PageId","Name","Revision") table constraint ("UK_PageFile") and a
            //separately created index ("IX_PageFile_Id_Navigation_Revision", whose name is misleading - it is
            //actually built on Name/PageId/Revision, not Navigation). Column order differs between the two but
            //uniqueness enforcement is identical either way, so this is functionally-duplicate historical debt,
            //consolidated here to one unique index, matching the Emoji precedent from the previous task.
            builder.HasIndex(e => new { e.PageId, e.Name, e.Revision }, "IX_PageFile_PageId_Name_Revision")
                .IsUnique();

            builder.HasOne(e => e.Page)
                .WithMany(e => e.PageFiles)
                .HasForeignKey(e => e.PageId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }
}
