namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents the data required to insert or update an emoji record,
    /// including its name, categories, and image content.
    /// </summary>
    public class TwUpsertEmoji
    {
        /// <summary>
        /// The unique identifier of the emoji to update, or null to insert a new emoji.
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// The unique name used to reference this emoji in wiki markup.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The list of category names this emoji belongs to.
        /// </summary>
        public List<string> Categories { get; set; } = new();

        /// <summary>
        /// The raw image data for this emoji, or null if the image is not being updated.
        /// </summary>
        public byte[]? ImageData { get; set; }

        /// <summary>
        /// The MIME type of the emoji image, such as "image/png" or "image/gif".
        /// </summary>
        public string MimeType { get; set; } = string.Empty;
    }
}