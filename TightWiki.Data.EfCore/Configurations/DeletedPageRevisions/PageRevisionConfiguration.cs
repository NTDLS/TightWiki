using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.DeletedPageRevisions;

namespace TightWiki.Data.EfCore.Configurations.DeletedPageRevisions
{
    /// <summary>
    /// Fluent configuration for <see cref="PageRevision"/> (DeletedPageRevisions.PageRevision).
    /// </summary>
    /// <remarks>
    /// ModifiedByUserId/ModifiedDate are modeled as Guid/DateTime, matching Pages.PageRevision and the fact that
    /// MovePageRevisionToDeletedById.sql copies these columns verbatim from Pages.PageRevision, rather than the
    /// raw scaffold's naive string typing (a consequence of the table being empty locally).
    /// </remarks>
    public class PageRevisionConfiguration : IEntityTypeConfiguration<PageRevision>
    {
        public void Configure(EntityTypeBuilder<PageRevision> builder)
        {
            builder.ToTable("PageRevision", schema: "DeletedPageRevisions");

            builder.HasKey(e => new { e.PageId, e.Revision });

            builder.Property(e => e.Name)
                .IsRequired()
                .UseCollation("NOCASE");

            builder.Property(e => e.Namespace).UseCollation("NOCASE");

            builder.Property(e => e.Description).IsRequired();
            builder.Property(e => e.Body).IsRequired();

            //ModifiedByUserId is value-equal to Users.Profile.UserId (see Database-Providers-Plan.md chapter
            //4.3) but not a real FOREIGN KEY - see Pages.PageConfiguration's remarks on CreatedByUser for the
            //full rationale.
            builder.HasOne(e => e.ModifiedByUser)
                .WithMany(e => e.DeletedPageRevisions_PageRevisions)
                .HasForeignKey(e => e.ModifiedByUserId)
                .HasPrincipalKey(e => e.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }
}
