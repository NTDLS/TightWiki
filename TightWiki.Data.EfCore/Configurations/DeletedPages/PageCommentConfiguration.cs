using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.DeletedPages;

namespace TightWiki.Data.EfCore.Configurations.DeletedPages
{
    /// <summary>
    /// Fluent configuration for <see cref="PageComment"/> (DeletedPages.PageComment).
    /// </summary>
    public class PageCommentConfiguration : IEntityTypeConfiguration<PageComment>
    {
        public void Configure(EntityTypeBuilder<PageComment> builder)
        {
            builder.ToTable("PageComment", schema: "DeletedPages");

            builder.HasKey(e => e.Id);

            //Id is copied verbatim from the source comment - not database-generated.
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.Property(e => e.Body).IsRequired();

            //UserId is value-equal to Users.Profile.UserId (see Database-Providers-Plan.md chapter 4.3) but not
            //a real FOREIGN KEY - see Pages.PageConfiguration's remarks on CreatedByUser for the full rationale.
            builder.HasOne(e => e.User)
                .WithMany(e => e.DeletedPages_PageComments)
                .HasForeignKey(e => e.UserId)
                .HasPrincipalKey(e => e.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }
}
