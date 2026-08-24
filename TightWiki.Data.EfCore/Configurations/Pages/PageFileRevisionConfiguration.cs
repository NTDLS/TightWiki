using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Pages;

namespace TightWiki.Data.EfCore.Configurations.Pages
{
    /// <summary>
    /// Fluent configuration for <see cref="PageFileRevision"/> (Pages.PageFileRevision).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>CreatedByUserId</c>: a raw <c>dotnet ef dbcontext scaffold</c> run against the live Data/pages.db
    /// mapped this column to <c>string</c> (with a build warning: "should map to a property of type 'Guid', but
    /// its values are in an incompatible format") instead of <c>Guid</c>, unlike every other *ByUserId/UserId
    /// column in this schema. The actual stored value ("963f0b81-f2ac-488b-9b21-521852641ec4") is a perfectly
    /// valid GUID - just lowercase, whereas the single dev admin user's id happens to be stored uppercase in
    /// Page/PageComment/PageRevision. EF Core's scaffolder Guid-detection heuristic rejected it for that reason
    /// alone (verified by re-running scaffold locally). Since this column has exactly one sample value across
    /// the entire dev database, that heuristic isn't a reliable signal here. Application code always writes
    /// <c>Guid.ToString()</c> into it (PageRepository.UpsertPageFile), so it is modeled as Guid, matching
    /// Page/PageComment/PageRevision's user-id columns and the domain model (TightWiki.Plugin.Models.
    /// TwPageFileAttachment is populated from the same userId). COLLATE NOCASE, which the real DDL does declare
    /// for this specific column (unlike the others), is dropped accordingly - it does not apply to a Guid value.
    /// </para>
    /// <para>
    /// <c>Size</c>: modeled as <c>long</c> rather than the raw scaffold's <c>int</c> - the domain model
    /// (TightWiki.Plugin.Models.TwPageFileAttachment.Size) is already <c>long</c>, and the underlying SQLite
    /// column has INTEGER affinity (64-bit), so there is no reason to cap it at 32 bits for file sizes.
    /// </para>
    /// </remarks>
    public class PageFileRevisionConfiguration : IEntityTypeConfiguration<PageFileRevision>
    {
        public void Configure(EntityTypeBuilder<PageFileRevision> builder)
        {
            builder.ToTable("PageFileRevision", schema: "Pages");

            builder.HasKey(e => new { e.PageFileId, e.Revision });

            builder.Property(e => e.ContentType)
                .IsRequired()
                .UseCollation("NOCASE");

            builder.Property(e => e.Data).IsRequired();

            //The real schema also declares a UNIQUE("PageFileId","Revision") constraint ("UK_PageFileRevision")
            //identical (same columns, same order) to the primary key itself. SQLite already collapses this into
            //a single autoindex at the storage level (verified via PRAGMA index_list - only one index exists),
            //so there is nothing to reproduce here beyond the primary key.

            builder.HasOne(e => e.PageFile)
                .WithMany(e => e.PageFileRevisions)
                .HasForeignKey(e => e.PageFileId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            //CreatedByUserId is value-equal to Users.Profile.UserId (see Database-Providers-Plan.md chapter 4.3)
            //but not a real FOREIGN KEY - see PageConfiguration's remarks on CreatedByUser for the full
            //rationale. LEFT OUTER JOINed against Profile in
            //GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged.sql.
            builder.HasOne(e => e.CreatedByUser)
                .WithMany(e => e.Pages_PageFileRevisions)
                .HasForeignKey(e => e.CreatedByUserId)
                .HasPrincipalKey(e => e.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }
}
