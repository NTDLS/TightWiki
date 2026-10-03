namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a single revision of a page file attachment, along with where it is used. Used by the attachment administration screen.
    /// </summary>
    public partial class TwPageAttachmentSummary
    {
        /// <summary>
        /// The total number of pages available when paginating attachment lists.
        /// </summary>
        public int PaginationPageCount { get; set; }

        /// <summary>
        /// The unique identifier of the page file record.
        /// </summary>
        public int PageFileId { get; set; }

        /// <summary>
        /// The full name of the page this attachment belongs to, including namespace prefix if applicable.
        /// </summary>
        public string PageName { get; set; } = string.Empty;

        /// <summary>
        /// The namespace of the page this attachment belongs to.
        /// </summary>
        public string Namespace { get; set; } = string.Empty;

        /// <summary>
        /// The URL-safe navigation path of the page this attachment belongs to.
        /// </summary>
        public string PageNavigation { get; set; } = string.Empty;

        /// <summary>
        /// The file name of the attachment.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// The URL-safe navigation path used to locate this file attachment.
        /// </summary>
        public string FileNavigation { get; set; } = string.Empty;

        /// <summary>
        /// The MIME content type of the attachment.
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// The size of the file revision in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// The revision number of this file attachment.
        /// </summary>
        public int FileRevision { get; set; }

        /// <summary>
        /// The date this file revision was uploaded.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// The number of page revisions that reference this file revision.
        /// </summary>
        public int PageRevisionCount { get; set; }

        /// <summary>
        /// The number of times the current revision of the page references this file revision (zero or one).
        /// </summary>
        public int CurrentPageRevisionCount { get; set; }

        /// <summary>
        /// True if no page revision references this file revision.
        /// </summary>
        public bool IsOrphaned => PageRevisionCount == 0;

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
