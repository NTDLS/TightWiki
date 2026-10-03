namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Aggregate statistics for all page file attachments.
    /// </summary>
    public partial class TwPageAttachmentTotals
    {
        /// <summary>
        /// The total number of file revisions stored.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// The total size, in bytes, of all file revisions stored.
        /// </summary>
        public long TotalSize { get; set; }

        /// <summary>
        /// The number of file revisions that are not referenced by any page revision.
        /// </summary>
        public int OrphanedCount { get; set; }

        /// <summary>
        /// The total size, in bytes, of file revisions that are not referenced by any page revision.
        /// </summary>
        public long OrphanedSize { get; set; }
    }
}
