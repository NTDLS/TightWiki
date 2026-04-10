namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents an emoji available for use in wiki page content,
    /// including its image data, MIME type, shortcut syntax, and category associations.
    /// </summary>
    public class TwEmoji
    {
        /// <summary>
        /// The unique identifier for this emoji record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The unique name used to reference this emoji in wiki markup.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The shortcut syntax used to insert this emoji inline in wiki content.
        /// </summary>
        public string Shortcut { get; set; } = string.Empty;

        /// <summary>
        /// The total number of pages available when paginating emoji lists.
        /// </summary>
        public int PaginationPageCount { get; set; }

        /// <summary>
        /// A delimited string of category names this emoji belongs to.
        /// </summary>
        public string Categories { get; set; } = string.Empty;

        /// <summary>
        /// The raw image data for this emoji, or null if not yet loaded.
        /// </summary>
        public byte[]? ImageData { get; set; }

        /// <summary>
        /// The MIME type of the emoji image, such as "image/png" or "image/gif".
        /// </summary>
        public string MimeType { get; set; } = string.Empty;
    }
}