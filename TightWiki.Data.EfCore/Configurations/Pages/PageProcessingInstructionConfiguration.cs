using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Pages;

namespace TightWiki.Data.EfCore.Configurations.Pages
{
    /// <summary>
    /// Fluent configuration for <see cref="PageProcessingInstruction"/> (Pages.PageProcessingInstruction).
    /// </summary>
    public class PageProcessingInstructionConfiguration : IEntityTypeConfiguration<PageProcessingInstruction>
    {
        public void Configure(EntityTypeBuilder<PageProcessingInstruction> builder)
        {
            builder.ToTable("PageProcessingInstruction", schema: "Pages");

            builder.HasKey(e => new { e.PageId, e.Instruction });

            builder.Property(e => e.Instruction).UseCollation("NOCASE");

            builder.HasOne(e => e.Page)
                .WithMany(e => e.PageProcessingInstructions)
                .HasForeignKey(e => e.PageId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }
}
