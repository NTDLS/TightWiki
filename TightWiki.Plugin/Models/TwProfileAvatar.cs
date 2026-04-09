namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a user profile avatar image, storing the raw image content
    /// and its MIME type for serving directly to the client.
    /// </summary>
    public class TwProfileAvatar
    {
        /// <summary>
        /// The raw byte content of the avatar image, or null if no avatar has been uploaded.
        /// </summary>
        public byte[]? Bytes { get; set; }

        /// <summary>
        /// The MIME content type of the avatar image, such as "image/png" or "image/jpeg".
        /// </summary>
        public string ContentType { get; set; } = string.Empty;
    }
}