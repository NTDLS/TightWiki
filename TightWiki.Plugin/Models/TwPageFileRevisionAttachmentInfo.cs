namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents lightweight revision metadata for a page file attachment,
    /// used to identify and compare file revisions without loading the full file content.
    /// </summary>
    public class TwPageFileRevisionAttachmentInfo
    {
        /// <summary>
        /// The revision number of this file attachment.
        /// </summary>
        public int Revision { get; set; }

        /// <summary>
        /// The MIME content type of the attached file.
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// The size of the attached file in bytes.
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// A hash of the file content used to detect changes between revisions.
        /// </summary>
        public int DataHash { get; set; }

        /// <summary>
        /// The unique identifier of the page this file attachment belongs to.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The unique identifier of the page file record this revision belongs to.
        /// </summary>
        public int PageFileId { get; set; }
    }
}