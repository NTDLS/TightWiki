using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Emoji;

namespace TightWiki.Data.EfCore.Configurations.Emoji
{
    /// <summary>
    /// Fluent configuration for <see cref="EmojiCategory"/> (Emoji.EmojiCategory).
    /// </summary>
    public class EmojiCategoryConfiguration : IEntityTypeConfiguration<EmojiCategory>
    {
        public void Configure(EntityTypeBuilder<EmojiCategory> builder)
        {
            builder.ToTable("EmojiCategory", schema: "Emoji");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Category)
                .IsRequired()
                .UseCollation("NOCASE");

            //The real database carries two functionally-redundant unique indexes on (EmojiId, Category) (an
            //explicit "IX_EmojiCategory" index plus an inline UNIQUE table constraint) - almost certainly
            //historical debt from a past schema change. Consolidated here to a single unique index rather than
            //reproduced twice.
            builder.HasIndex(e => new { e.EmojiId, e.Category }, "IX_EmojiCategory")
                .IsUnique();

            //No FK constraint exists in the real schema for EmojiId - intentionally not modeled as a
            //navigation/relationship here.
        }
    }
}
