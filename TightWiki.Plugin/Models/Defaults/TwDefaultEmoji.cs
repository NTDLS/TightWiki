namespace TightWiki.Plugin.Models.Defaults
{
    /// <summary>
    /// Represents a default emoji definition used to seed the database with the built-in emoji set on first run.
    /// This is metadata only - the image itself is not carried on this model. The SQLite provider gets its
    /// emoji "for free" via a full copy of Data\emoji.db and never populates this via
    /// ITwDefaultsRepository.GetDefaultEmojis. Providers that seed from the provider-neutral
    /// Seed\tightwiki.seed.zip package (see Database-Providers-Plan.md chapter 4.6) read this shape from
    /// Emoji.json, with the image bytes stored as a separate zip entry referenced by <see cref="ImageEntry"/>.
    /// </summary>
    public class TwDefaultEmoji
    {
        /// <summary>
        /// The unique identifier for this emoji record. Preserved across the seed package so that
        /// <see cref="TwDefaultEmojiCategory.EmojiId"/> can reference it without a name-based re-link.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The unique name used to reference this emoji in wiki markup.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The MIME type of the emoji image, such as "image/png" or "image/gif".
        /// </summary>
        public string MimeType { get; set; } = string.Empty;

        /// <summary>
        /// The zip-relative path to this emoji's image bytes within Seed\tightwiki.seed.zip (e.g.
        /// "Emoji/Images/12.png"). Empty when this model isn't backed by a seed package.
        /// </summary>
        public string ImageEntry { get; set; } = string.Empty;
    }
}
