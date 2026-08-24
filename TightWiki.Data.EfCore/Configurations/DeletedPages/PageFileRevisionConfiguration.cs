using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.DeletedPages;

namespace TightWiki.Data.EfCore.Configurations.DeletedPages
{
    /// <summary>
    /// Fluent configuration for <see cref="PageFileRevision"/> (DeletedPages.PageFileRevision).
    /// </summary>
    public class PageFileRevisionConfiguration : IEntityTypeConfiguration<PageFileRevision>
    {
        public void Configure(EntityTypeBuilder<PageFileRevision> builder)
        {
            builder.ToTable("PageFileRevision", schema: "DeletedPages");

            builder.HasKey(e => new { e.PageFileId, e.Revision });

            builder.Property(e => e.ContentType)
                .IsRequired()
                .UseCollation("NOCASE");

            //The real schema does declare COLLATE NOCASE for CreatedByUserId here too (matching
            //Pages.PageFileRevision), but it is dropped since the column is modeled as Guid, not string - see
            //Pages.PageFileRevisionConfiguration for the full rationale.

            builder.Property(e => e.Data).IsRequired();

            //CreatedByUserId is value-equal to Users.Profile.UserId (see Database-Providers-Plan.md chapter 4.3)
            //but not a real FOREIGN KEY - see Pages.PageConfiguration's remarks on CreatedByUser for the full
            //rationale.
            builder.HasOne(e => e.CreatedByUser)
                .WithMany(e => e.DeletedPages_PageFileRevisions)
                .HasForeignKey(e => e.CreatedByUserId)
                .HasPrincipalKey(e => e.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }
}
