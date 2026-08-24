using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Pages;

namespace TightWiki.Data.EfCore.Configurations.Pages
{
    /// <summary>
    /// Fluent configuration for <see cref="PageToken"/> (Pages.PageToken).
    /// </summary>
    public class PageTokenConfiguration : IEntityTypeConfiguration<PageToken>
    {
        public void Configure(EntityTypeBuilder<PageToken> builder)
        {
            builder.ToTable("PageToken", schema: "Pages");

            builder.HasKey(e => new { e.PageId, e.Token });

            builder.Property(e => e.Token).UseCollation("NOCASE");

            builder.Property(e => e.DoubleMetaphone)
                .IsRequired()
                .UseCollation("NOCASE");

            builder.HasIndex(e => new { e.DoubleMetaphone, e.PageId, e.Weight },
                "idx_PageToken_DoubleMetaphone_PageId_Weight");

            builder.HasIndex(e => e.PageId, "idx_PageToken_PageId");

            builder.HasIndex(e => new { e.Token, e.PageId, e.Weight }, "idx_PageToken_Token_PageId_Weight");

            builder.HasOne(e => e.Page)
                .WithMany(e => e.PageTokens)
                .HasForeignKey(e => e.PageId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }
}
