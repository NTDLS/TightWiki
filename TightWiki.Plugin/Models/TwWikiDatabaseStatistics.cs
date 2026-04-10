namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents aggregate statistics about the wiki database, providing a snapshot
    /// of content volume across pages, users, attachments, and other key entities.
    /// </summary>
    public class TwWikiDatabaseStatistics
    {
        /// <summary>
        /// The total number of active pages in the wiki.
        /// </summary>
        public int Pages { get; set; }

        /// <summary>
        /// The total number of internal links between wiki pages.
        /// </summary>
        public int IntraLinks { get; set; }

        /// <summary>
        /// The total number of page revisions stored across all pages.
        /// </summary>
        public int PageRevisions { get; set; }

        /// <summary>
        /// The total number of file attachments across all pages.
        /// </summary>
        public int PageAttachments { get; set; }

        /// <summary>
        /// The total number of file attachment revisions stored across all attachments.
        /// </summary>
        public int PageAttachmentRevisions { get; set; }

        /// <summary>
        /// The total number of tags assigned across all pages.
        /// </summary>
        public int PageTags { get; set; }

        /// <summary>
        /// The total number of search tokens indexed across all pages.
        /// </summary>
        public int PageSearchTokens { get; set; }

        /// <summary>
        /// The total number of registered user accounts.
        /// </summary>
        public int Users { get; set; }

        /// <summary>
        /// The total number of user profiles in the wiki.
        /// </summary>
        public int Profiles { get; set; }

        /// <summary>
        /// The total number of distinct namespaces in use across all pages.
        /// </summary>
        public int Namespaces { get; set; }
    }
}
