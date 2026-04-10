namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents the search scoring result for a single page against a set of search terms,
    /// used to rank pages in search results by relevance.
    /// </summary>
    public class TwPageSearchToken
    {
        /// <summary>
        /// The unique identifier of the page this search score applies to.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The relevance match score indicating how closely the page content matches the search terms.
        /// </summary>
        public double Match { get; set; }

        /// <summary>
        /// The weight assigned to this page, used to adjust its ranking in search results.
        /// </summary>
        public double Weight { get; set; }

        /// <summary>
        /// The composite search score combining match and weight, used to determine final ranking order.
        /// </summary>
        public double Score { get; set; }
    }
}