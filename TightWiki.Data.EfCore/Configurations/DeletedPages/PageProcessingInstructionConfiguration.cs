using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.DeletedPages;

namespace TightWiki.Data.EfCore.Configurations.DeletedPages
{
    /// <summary>
    /// Fluent configuration for <see cref="PageProcessingInstruction"/> (DeletedPages.PageProcessingInstruction).
    /// </summary>
    public class PageProcessingInstructionConfiguration : IEntityTypeConfiguration<PageProcessingInstruction>
    {
        public void Configure(EntityTypeBuilder<PageProcessingInstruction> builder)
        {
            builder.ToTable("PageProcessingInstruction", schema: "DeletedPages");

            builder.HasKey(e => new { e.PageId, e.Instruction });

            builder.Property(e => e.Instruction).UseCollation("NOCASE");
        }
    }
}
