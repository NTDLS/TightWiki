namespace TightWiki.Plugin.Models.Defaults
{
    /// <summary>
    /// Represents a default emoji-to-category association used to seed the database, linking a
    /// <see cref="TwDefaultEmoji"/> (by <see cref="EmojiId"/>) to one of the named categories it belongs to.
    /// As with <see cref="TwDefaultEmoji"/>, the SQLite provider never populates this via
    /// ITwDefaultsRepository.GetDefaultEmojiCategories - it exists for provider-neutral seeding from
    /// Seed\tightwiki.seed.zip (see Database-Providers-Plan.md chapter 4.6).
    /// </summary>
    public class TwDefaultEmojiCategory
    {
        /// <summary>
        /// The unique identifier for this emoji-category association record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The <see cref="TwDefaultEmoji.Id"/> of the emoji associated with this category record.
        /// </summary>
        public int EmojiId { get; set; }

        /// <summary>
        /// The name of the category this emoji belongs to.
        /// </summary>
        public string Category { get; set; } = string.Empty;
    }
}
