namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a reference to a wiki page that does not yet exist,
    /// identified by scanning outbound links from existing pages. Used to surface
    /// missing pages for content maintenance and broken link reporting.
    /// </summary>
    public class TwNonexistentPage
    {
        /// <summary>
        /// The unique identifier of the page that contains the broken link.
        /// </summary>
        public int SourcePageId { get; set; }

        /// <summary>
        /// The full name of the page that contains the broken link.
        /// </summary>
        public string SourcePageName { get; set; } = string.Empty;

        /// <summary>
        /// The URL-safe navigation path of the page that contains the broken link.
        /// </summary>
        public string SourcePageNavigation { get; set; } = string.Empty;

        /// <summary>
        /// The full name of the page that is referenced but does not exist.
        /// </summary>
        public string TargetPageName { get; set; } = string.Empty;

        /// <summary>
        /// The URL-safe navigation path of the page that is referenced but does not exist.
        /// </summary>
        public string TargetPageNavigation { get; set; } = string.Empty;

        /// <summary>
        /// The total number of pages available when paginating missing page lists.
        /// </summary>
        public int PaginationPageCount { get; set; }
    }
}