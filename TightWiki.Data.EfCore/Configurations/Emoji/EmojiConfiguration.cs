using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EmojiEntity = TightWiki.Data.EfCore.Entities.Emoji.Emoji;

namespace TightWiki.Data.EfCore.Configurations.Emoji
{
    /// <summary>
    /// Fluent configuration for <see cref="EmojiEntity"/> (Emoji.Emoji).
    /// </summary>
    /// <remarks>
    /// The entity type is aliased to <c>EmojiEntity</c> in this file because both the entity's namespace
    /// (<c>...Entities.Emoji</c>) and this configuration's own namespace (<c>...Configurations.Emoji</c>) end in
    /// a segment named "Emoji", which shadows the unqualified type name "Emoji" (CS0118).
    /// </remarks>
    public class EmojiConfiguration : IEntityTypeConfiguration<EmojiEntity>
    {
        public void Configure(EntityTypeBuilder<EmojiEntity> builder)
        {
            builder.ToTable("Emoji", schema: "Emoji");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Name)
                .IsRequired()
                .UseCollation("NOCASE");

            builder.Property(e => e.MimeType)
                .UseCollation("NOCASE");

            //The real database carries two functionally-redundant unique indexes on Name (an explicit
            //"IX_Emoji" index plus an inline UNIQUE table constraint) - almost certainly historical debt from
            //a past schema change. Consolidated here to a single unique index rather than reproduced twice.
            builder.HasIndex(e => e.Name, "IX_Emoji")
                .IsUnique();
        }
    }
}
