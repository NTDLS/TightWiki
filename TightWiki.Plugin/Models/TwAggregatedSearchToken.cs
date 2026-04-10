namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents an aggregated search token with its associated weight and phonetic encoding,
    /// used for scoring and fuzzy matching during page searches.
    /// </summary>
    public class TwAggregatedSearchToken
    {
        /// <summary>
        /// The search token string derived from page content.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// The weight assigned to this token, used for relevance scoring in search results.
        /// </summary>
        public double Weight { get; set; }

        /// <summary>
        /// The Double Metaphone phonetic encoding of the token, used for fuzzy and sound-alike matching.
        /// </summary>
        public string DoubleMetaphone { get; set; } = string.Empty;
    }
}