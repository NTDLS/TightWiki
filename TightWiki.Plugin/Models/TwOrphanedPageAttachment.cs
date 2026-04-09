namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a page file attachment that is no longer associated with any active page revision,
    /// typically identified during maintenance operations for cleanup or purging.
    /// </summary>
    public partial class TwOrphanedPageAttachment
    {
        /// <summary>
        /// The total number of pages available when paginating orphaned attachment lists.
        /// </summary>
        public int PaginationPageCount { get; set; }

        /// <summary>
        /// The unique identifier of the orphaned page file record.
        /// </summary>
        public int PageFileId { get; set; }

        /// <summary>
        /// The full name of the page this attachment was originally associated with, including namespace prefix if applicable.
        /// </summary>
        public string PageName { get; set; } = string.Empty;

        /// <summary>
        /// The namespace of the page this attachment was originally associated with.
        /// </summary>
        public string Namespace { get; set; } = string.Empty;

        /// <summary>
        /// The URL-safe navigation path of the page this attachment was originally associated with.
        /// </summary>
        public string PageNavigation { get; set; } = string.Empty;

        /// <summary>
        /// The original file name of the orphaned attachment.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// The URL-safe navigation path used to locate this file attachment.
        /// </summary>
        public string FileNavigation { get; set; } = string.Empty;

        /// <summary>
        /// The size of the orphaned file in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// The revision number of this orphaned file attachment.
        /// </summary>
        public int FileRevision { get; set; }

        /// <summary>
        /// The display title of the page, derived from the page name by stripping the namespace prefix if present.
        /// </summary>
        public string PageTitle
        {
            get
            {
                if (PageName.Contains("::"))
                {
                    return PageName.Substring(PageName.IndexOf("::") + 2).Trim();
                }
                return PageName;
            }
        }
    }
}