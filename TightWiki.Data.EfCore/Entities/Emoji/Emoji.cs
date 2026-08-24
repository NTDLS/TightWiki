namespace TightWiki.Data.EfCore.Entities.Emoji
{
    /// <summary>
    /// A single emoji, including its compressed image bytes (Emoji.Emoji).
    /// </summary>
    public class Emoji
    {
        /// <summary>
        /// The unique identifier for this emoji.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The unique, case-insensitive shortcut name of this emoji.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The (compressed) image bytes for this emoji.
        /// </summary>
        public byte[]? ImageData { get; set; }

        /// <summary>
        /// The MIME type of the image data (e.g. "image/gif", "image/png").
        /// </summary>
        public string? MimeType { get; set; }
    }
}
