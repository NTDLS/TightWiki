namespace TightWiki.Data.EfCore.Entities.Emoji
{
    /// <summary>
    /// A category tag associated with an <see cref="Emoji"/> (Emoji.EmojiCategory).
    /// </summary>
    public class EmojiCategory
    {
        /// <summary>
        /// The unique identifier for this emoji/category association.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The identifier of the <see cref="Emoji"/> this category applies to. Not modeled as a navigation
        /// property here - the real schema declares no foreign key constraint for this column.
        /// </summary>
        public int EmojiId { get; set; }

        /// <summary>
        /// The case-insensitive category name, unique together with <see cref="EmojiId"/>.
        /// </summary>
        public string Category { get; set; } = string.Empty;
    }
}
