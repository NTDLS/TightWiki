using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.DeletedPages;

namespace TightWiki.Data.EfCore.Configurations.DeletedPages
{
    /// <summary>
    /// Fluent configuration for <see cref="Page"/> (DeletedPages.Page).
    /// </summary>
    /// <remarks>
    /// No CREATE TABLE script exists for this table - schema verified against the live Data/deletedpages.db
    /// (empty in the local dev database). CreatedByUserId/ModifiedByUserId/CreatedDate/ModifiedDate are modeled
    /// as Guid/DateTime, matching Pages.Page and the fact that MovePageToDeletedById.sql copies these columns
    /// verbatim from Pages.Page, rather than the raw scaffold's naive string typing (a consequence of the table
    /// being empty locally, so the scaffolder never had sample data to type-detect against).
    /// </remarks>
    public class PageConfiguration : IEntityTypeConfiguration<Page>
    {
        public void Configure(EntityTypeBuilder<Page> builder)
        {
            builder.ToTable("Page", schema: "DeletedPages");

            builder.HasKey(e => e.Id);

            //Id is copied verbatim from the source page - not database-generated.
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.Property(e => e.Name)
                .IsRequired()
                .UseCollation("NOCASE");

            builder.Property(e => e.Namespace)
                .IsRequired()
                .UseCollation("NOCASE");

            builder.Property(e => e.Navigation)
                .IsRequired()
                .UseCollation("NOCASE");

            builder.Property(e => e.Description).IsRequired();

            //No unique indexes/constraints exist on this table in the real schema (unlike Pages.Page) - once a
            //page is soft-deleted, its old Name/Navigation no longer need to stay unique (e.g. a new page could
            //reuse the same name).

            //CreatedByUserId/ModifiedByUserId are value-equal to Users.Profile.UserId (see
            //Database-Providers-Plan.md chapter 4.3) but not a real FOREIGN KEY across the physically separate
            //SQLite database files - see Pages.PageConfiguration's remarks on CreatedByUser for the full
            //rationale. LEFT OUTER JOINed against Profile in GetAllDeletedPagesPaged.sql/
            //GetAllDeletedPagesByPageIdPaged.sql/GetDeletedPageById.sql.
            builder.HasOne(e => e.CreatedByUser)
                .WithMany(e => e.DeletedPages_CreatedPages)
                .HasForeignKey(e => e.CreatedByUserId)
                .HasPrincipalKey(e => e.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.ClientSetNull);

            builder.HasOne(e => e.ModifiedByUser)
                .WithMany(e => e.DeletedPages_ModifiedPages)
                .HasForeignKey(e => e.ModifiedByUserId)
                .HasPrincipalKey(e => e.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }
}
