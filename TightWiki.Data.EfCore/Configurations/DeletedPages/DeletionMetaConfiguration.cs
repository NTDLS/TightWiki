using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.DeletedPages;

namespace TightWiki.Data.EfCore.Configurations.DeletedPages
{
    /// <summary>
    /// Fluent configuration for <see cref="DeletionMeta"/> (DeletedPages.DeletionMeta).
    /// </summary>
    /// <remarks>
    /// No CREATE TABLE script exists for this table (see Database-Providers-Plan.md, chapter 2.1a) - schema
    /// verified directly against the live Data/deletedpages.db via PRAGMA table_info/index_list. The table is
    /// entirely empty in the local dev database (no page has ever been soft-deleted there), so the raw scaffold's
    /// typing is based purely on the declared SQLite column affinity, not on any real data. DeletedByUserId/
    /// DeletedDate are declared INTEGER in the DDL but application code always writes a Guid and a DateTime into
    /// them respectively (PageRepository.MovePageToDeletedById/MovePageRevisionToDeletedById); SQLite's dynamic
    /// typing stores the actual TEXT values regardless of the column's declared INTEGER affinity. Modeled as
    /// Guid?/DateTime? accordingly, matching the domain model and the *ByUserId/*Date columns elsewhere in this
    /// schema, rather than the raw scaffold's naive int?/int?.
    /// </remarks>
    public class DeletionMetaConfiguration : IEntityTypeConfiguration<DeletionMeta>
    {
        public void Configure(EntityTypeBuilder<DeletionMeta> builder)
        {
            builder.ToTable("DeletionMeta", schema: "DeletedPages");

            builder.HasKey(e => e.PageId);

            //PageId is copied verbatim from the source page - not database-generated.
            builder.Property(e => e.PageId).ValueGeneratedNever();

            //DeletedByUserId is value-equal to Users.Profile.UserId (see Database-Providers-Plan.md chapter 4.3)
            //but not a real FOREIGN KEY - see Pages.PageConfiguration's remarks on CreatedByUser for the full
            //rationale. LEFT OUTER JOINed against Profile (as "DeletedUser") in GetAllDeletedPagesPaged.sql/
            //GetAllDeletedPagesByPageIdPaged.sql/GetDeletedPageById.sql. Already nullable, so IsRequired(false)
            //here only makes explicit what the nullable FK type already implies.
            builder.HasOne(e => e.DeletedByUser)
                .WithMany(e => e.DeletedPages_DeletionMetas)
                .HasForeignKey(e => e.DeletedByUserId)
                .HasPrincipalKey(e => e.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }
}
