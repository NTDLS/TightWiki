namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents metadata for a single revision of a wiki page, including authorship,
    /// modification details, and change summary information. Does not include the page body content.
    /// </summary>
    public class TwPageRevision
    {
        /// <summary>
        /// The number of items per page used when paginating revision history lists.
        /// </summary>
        public int PaginationPageSize { get; set; }

        /// <summary>
        /// The total number of pages available when paginating revision history lists.
        /// </summary>
        public int PaginationPageCount { get; set; }

        /// <summary>
        /// The unique identifier of the page this revision belongs to.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The account name of the user who made this revision.
        /// </summary>
        public string ModifiedByUserName { get; set; } = string.Empty;

        /// <summary>
        /// The unique identifier of the user who made this revision.
        /// </summary>
        public Guid ModifiedByUserId { get; set; }

        /// <summary>
        /// The date and time this revision was made.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        /// <summary>
        /// The full name of the page at the time of this revision, including namespace prefix if applicable.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The description of the page at the time of this revision.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The revision number of this page revision.
        /// </summary>
        public int Revision { get; set; }

        /// <summary>
        /// The number of revisions newer than this revision.
        /// </summary>
        public int HigherRevisionCount { get; set; }

        /// <summary>
        /// The highest revision number currently available for this page.
        /// </summary>
        public int HighestRevision { get; set; }

        /// <summary>
        /// The URL-safe navigation path of the page at the time of this revision.
        /// </summary>
        public string Navigation { get; set; } = string.Empty;

        /// <summary>
        /// The unique identifier of the user who originally created this page.
        /// </summary>
        public Guid CreatedByUserId { get; set; }

        /// <summary>
        /// The account name of the user who originally created this page.
        /// </summary>
        public string CreatedByUserName { get; set; } = string.Empty;

        /// <summary>
        /// The date and time this page was originally created.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// A brief summary of the changes made in this revision, provided by the editor.
        /// </summary>
        public string ChangeSummary { get; set; } = string.Empty;

        /// <summary>
        /// An automated analysis of the differences introduced by this revision.
        /// </summary>
        public string ChangeAnalysis { get; set; } = string.Empty;
    }
}