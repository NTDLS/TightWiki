namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a cached image, storing its raw byte content and MIME type
    /// to avoid repeated database lookups for frequently accessed images such as avatars and emojis.
    /// </summary>
    public partial class TwImageCacheItem
    {
        /// <summary>
        /// The MIME content type of the cached image, such as "image/png" or "image/gif".
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// The raw byte content of the cached image.
        /// </summary>
        public byte[] Bytes { get; set; }

        /// <summary>
        /// Initializes a new image cache item with the specified byte content and MIME type.
        /// </summary>
        /// <param name="bytes">The raw byte content of the image.</param>
        /// <param name="contentType">The MIME content type of the image.</param>
        public TwImageCacheItem(byte[] bytes, string contentType)
        {
            Bytes = bytes;
            ContentType = contentType;
        }
    }
}