using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Pages;

namespace TightWiki.Data.EfCore.Configurations.Pages
{
    /// <summary>
    /// Fluent configuration for <see cref="PageRevision"/> (Pages.PageRevision).
    /// </summary>
    public class PageRevisionConfiguration : IEntityTypeConfiguration<PageRevision>
    {
        public void Configure(EntityTypeBuilder<PageRevision> builder)
        {
            builder.ToTable("PageRevision", schema: "Pages");

            builder.HasKey(e => new { e.PageId, e.Revision });

            builder.Property(e => e.Name)
                .IsRequired()
                .UseCollation("NOCASE");

            builder.Property(e => e.Namespace).UseCollation("NOCASE");

            builder.Property(e => e.Description).IsRequired();
            builder.Property(e => e.Body).IsRequired();

            //The real schema also declares a UNIQUE("PageId","Revision") index ("IX_PageRevision_PageId_
            //Revision") with the exact same columns, in the exact same order, as the primary key itself -
            //fully redundant historical debt (verified via PRAGMA index_list/index_info), consolidated away
            //here, matching the Emoji precedent from the previous task.

            //No FOREIGN KEY constraint exists in the real schema for PageId - intentionally not modeled as a
            //navigation/relationship here (see the Page entity's own doc remarks).

            //ModifiedByUserId is value-equal to Users.Profile.UserId (see Database-Providers-Plan.md chapter
            //4.3) but not a real FOREIGN KEY - see PageConfiguration's remarks on CreatedByUser for the full
            //rationale. LEFT OUTER JOINed against Profile in GetPageRevisionsInfoByNavigationPaged.sql and
            //GetPageRevisionByNavigation.sql.
            builder.HasOne(e => e.ModifiedByUser)
                .WithMany(e => e.Pages_PageRevisions)
                .HasForeignKey(e => e.ModifiedByUserId)
                .HasPrincipalKey(e => e.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }
}
