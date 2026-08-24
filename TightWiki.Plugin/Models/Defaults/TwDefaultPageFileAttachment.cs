namespace TightWiki.Plugin.Models.Defaults
{
    /// <summary>
    /// Represents a default file (typically an image) attached to a default wiki page, such as the icons and
    /// screenshots embedded in the built-in/help/include pages, used to seed the database on first run.
    /// </summary>
    public class TwDefaultPageFileAttachment
    {
        /// <summary>
        /// The name of the wiki page this file is attached to (matched against the just-seeded Pages.Page.Name).
        /// </summary>
        public string PageName { get; set; } = string.Empty;

        /// <summary>
        /// The namespace of the wiki page this file is attached to, or an empty string if it has no namespace.
        /// </summary>
        public string Namespace { get; set; } = string.Empty;

        /// <summary>
        /// The original, case-insensitive file name of the attachment.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// The case-insensitive, URL-safe navigation path used to locate this file attachment.
        /// </summary>
        public string FileNavigation { get; set; } = string.Empty;

        /// <summary>
        /// The case-insensitive MIME content type of this file.
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// The raw byte content of this file.
        /// </summary>
        public byte[] Data { get; set; } = [];
    }
}
