namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a single search token paired with a weight value,
    /// used to influence relevance scoring when ranking pages in search results.
    /// </summary>
    public class WeightedSearchToken
    {
        /// <summary>
        /// The search token string derived from a query or page content.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// The weight assigned to this token, used to adjust its contribution to the overall search relevance score.
        /// </summary>
        public double Weight { get; set; }
    }
}