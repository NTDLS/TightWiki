using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.DeletedPageRevisions;

namespace TightWiki.Data.EfCore.Configurations.DeletedPageRevisions
{
    /// <summary>
    /// Fluent configuration for <see cref="DeletionMeta"/> (DeletedPageRevisions.DeletionMeta).
    /// </summary>
    /// <remarks>
    /// No CREATE TABLE script exists for this table (see Database-Providers-Plan.md, chapter 2.1a) - schema
    /// verified directly against the live Data/deletedpagerevisions.db via PRAGMA table_info/index_list. The
    /// table is entirely empty in the local dev database, so the raw scaffold's typing is based purely on the
    /// declared SQLite column affinity, not on any real data. DeletedByUserId/DeletedDate are declared INTEGER
    /// in the DDL but application code always writes a Guid and a DateTime into them respectively
    /// (PageRepository.MovePageRevisionToDeletedById); modeled as Guid?/DateTime? accordingly, matching
    /// DeletedPages.DeletionMeta.
    /// </remarks>
    public class DeletionMetaConfiguration : IEntityTypeConfiguration<DeletionMeta>
    {
        public void Configure(EntityTypeBuilder<DeletionMeta> builder)
        {
            builder.ToTable("DeletionMeta", schema: "DeletedPageRevisions");

            builder.HasKey(e => new { e.PageId, e.Revision });

            //Composite integer key - EF's value-generation-on-add convention only applies to single-property
            //integer keys, so no explicit ValueGeneratedNever() is needed here (unlike the single-column
            //DeletedPages.DeletionMeta.PageId).

            //DeletedByUserId is value-equal to Users.Profile.UserId (see Database-Providers-Plan.md chapter 4.3)
            //but not a real FOREIGN KEY - see Pages.PageConfiguration's remarks on CreatedByUser for the full
            //rationale. LEFT OUTER JOINed against Profile (as "DeletedUser") in GetDeletedPageRevisionById.sql/
            //GetDeletedPageRevisionsByIdPaged.sql. Already nullable, so IsRequired(false) here only makes
            //explicit what the nullable FK type already implies.
            builder.HasOne(e => e.DeletedByUser)
                .WithMany(e => e.DeletedPageRevisions_DeletionMetas)
                .HasForeignKey(e => e.DeletedByUserId)
                .HasPrincipalKey(e => e.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }
}
