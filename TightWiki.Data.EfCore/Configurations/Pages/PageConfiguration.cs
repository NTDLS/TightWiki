using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Pages;
using TightWiki.Data.EfCore.Entities.Users;

namespace TightWiki.Data.EfCore.Configurations.Pages
{
    /// <summary>
    /// Fluent configuration for <see cref="Page"/> (Pages.Page).
    /// </summary>
    /// <remarks>
    /// The one-to-many relationships to the other Pages-schema tables (all backed by real FOREIGN KEY
    /// constraints, intra-schema) are configured from the dependent (child) entity's own configuration - see
    /// e.g. <see cref="PageCommentConfiguration"/> - matching the Log/Severity precedent from the previous task.
    /// The cross-schema navigations to Users.Profile (<see cref="Page.CreatedByUser"/>/
    /// <see cref="Page.ModifiedByUser"/>) are configured here too, on the dependent side, per
    /// Database-Providers-Plan.md chapter 4.3 - see the remarks below.
    /// </remarks>
    public class PageConfiguration : IEntityTypeConfiguration<Page>
    {
        public void Configure(EntityTypeBuilder<Page> builder)
        {
            builder.ToTable("Page", schema: "Pages");

            builder.HasKey(e => e.Id);

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

            //CreatedByUserId/ModifiedByUserId carry no COLLATE NOCASE in the real schema.

            //CreatedByUserId/ModifiedByUserId are value-equal to Users.Profile.UserId (not AspNetUsers.Id
            //directly - see Database-Providers-Plan.md chapter 4.3 and the remarks on Users.Profile.UserId), but
            //SQLite enforces no real FOREIGN KEY constraint across the two physically separate database files.
            //Modeled here as a genuine (optional) EF navigation now that both schemas live in the same
            //TightWikiDbContext - every raw SQL equivalent (e.g. GetAllPagesPaged.sql) LEFT OUTER JOINs Profile,
            //so IsRequired(false) mirrors that rather than assuming referential integrity that isn't enforced.
            builder.HasOne(e => e.CreatedByUser)
                .WithMany(e => e.Pages_CreatedPages)
                .HasForeignKey(e => e.CreatedByUserId)
                .HasPrincipalKey(e => e.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.ClientSetNull);

            builder.HasOne(e => e.ModifiedByUser)
                .WithMany(e => e.Pages_ModifiedPages)
                .HasForeignKey(e => e.ModifiedByUserId)
                .HasPrincipalKey(e => e.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.ClientSetNull);

            //Statistics.PageStatistics.PageId references this table (cross-schema, real UNIQUE index on the
            //Statistics side - see PageStatisticConfiguration) - a genuine one-to-one relationship, optional
            //from this side (a page has no statistics row until first compiled), configured from the dependent
            //(Statistics.PageStatistic) side per the same convention as the other cross-schema navigations here.

            //The real schema declares both a standalone UNIQUE(Name) index (IX_Page_Name) and a composite
            //UNIQUE(Namespace, Name) constraint (UK_Page). Since Name already includes the namespace prefix
            //(e.g. "Wiki Help :: Markup") and is unique on its own, the composite constraint is functionally
            //redundant - any two rows that would violate UNIQUE(Namespace, Name) would already violate
            //UNIQUE(Name) alone. Consolidated to the two genuinely distinct unique indexes (Name, Navigation)
            //rather than reproducing all three, matching the Emoji precedent from the previous task.
            builder.HasIndex(e => e.Name, "IX_Page_Name").IsUnique();
            builder.HasIndex(e => e.Navigation, "IX_Page_Navigation").IsUnique();

            //PageRevision.PageId carries no FOREIGN KEY constraint in the real schema - intentionally not
            //modeled as a navigation/relationship here.
        }
    }
}
