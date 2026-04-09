namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents compilation and view statistics for a wiki page, tracking rendering performance,
    /// error counts, link counts, and view totals accumulated over all revisions and compilations.
    /// </summary>
    public class TwPageStatistics
    {
        /// <summary>
        /// The display title of the page, derived from the page name by stripping the namespace prefix if present.
        /// </summary>
        public string Title
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

        /// <summary>
        /// The full name of the page, including namespace prefix if applicable.
        /// </summary>
        public string PageName { get; set; } = string.Empty;

        /// <summary>
        /// The namespace this page belongs to, or an empty string if it has no namespace.
        /// </summary>
        public string Namespace { get; set; } = string.Empty;

        /// <summary>
        /// The URL-safe navigation path used to locate this page.
        /// </summary>
        public string Navigation { get; set; } = string.Empty;

        /// <summary>
        /// The unique identifier of the page these statistics apply to.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The total number of revisions recorded for this page.
        /// </summary>
        public int Revisions { get; set; }

        /// <summary>
        /// The date and time this page was most recently compiled.
        /// </summary>
        public DateTime LastCompileDateTime { get; set; }

        /// <summary>
        /// The total number of times this page has been compiled since it was created.
        /// </summary>
        public int TotalCompilationCount { get; set; }

        /// <summary>
        /// The rendering time in milliseconds for the most recent compilation.
        /// </summary>
        public decimal LastWikifyTimeMs { get; set; }

        /// <summary>
        /// The cumulative rendering time in milliseconds across all compilations.
        /// </summary>
        public decimal TotalWikifyTimeMs { get; set; }

        /// <summary>
        /// The total number of times this page has been viewed.
        /// </summary>
        public int TotalViewCount { get; set; }

        /// <summary>
        /// The number of markup matches found during the most recent compilation.
        /// </summary>
        public int LastMatchCount { get; set; }

        /// <summary>
        /// The number of errors encountered during the most recent compilation.
        /// </summary>
        public int LastErrorCount { get; set; }

        /// <summary>
        /// The number of outgoing links found during the most recent compilation.
        /// </summary>
        public int LastOutgoingLinkCount { get; set; }

        /// <summary>
        /// The number of tags applied to this page during the most recent compilation.
        /// </summary>
        public int LastTagCount { get; set; }

        /// <summary>
        /// The size in bytes of the processed body content from the most recent compilation.
        /// </summary>
        public int LastProcessedBodySize { get; set; }

        /// <summary>
        /// The size in bytes of the raw body content from the most recent compilation.
        /// </summary>
        public int LastBodySize { get; set; }

        /// <summary>
        /// The number of items per page used when paginating statistics lists.
        /// </summary>
        public int PaginationPageSize { get; set; }

        /// <summary>
        /// The total number of pages available when paginating statistics lists.
        /// </summary>
        public int PaginationPageCount { get; set; }
    }
}