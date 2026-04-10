namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents an emoji category association, linking an emoji to a named category
    /// and providing an aggregate count of emojis within that category.
    /// </summary>
    public class TwEmojiCategory
    {
        /// <summary>
        /// The unique identifier of the emoji associated with this category record.
        /// </summary>
        public int EmojiId { get; set; }

        /// <summary>
        /// The name of the category this emoji belongs to.
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// The total number of emojis in this category.
        /// </summary>
        public string EmojiCount { get; set; } = string.Empty;
    }
}