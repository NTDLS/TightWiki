namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a single search token extracted from a wiki page's content,
    /// storing its phonetic encoding and weight for use in full-text and fuzzy search indexing.
    /// </summary>
    public class TwPageToken
    {
        /// <summary>
        /// The unique identifier of the page this token was extracted from.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The search token string derived from the page content.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// The Double Metaphone phonetic encoding of the token, used for fuzzy and sound-alike matching.
        /// </summary>
        public string DoubleMetaphone { get; set; } = string.Empty;

        /// <summary>
        /// The weight assigned to this token, used for relevance scoring in search results.
        /// </summary>
        public double Weight { get; set; }

        /// <summary>
        /// Returns true if the specified object is a <see cref="TwPageToken"/> with the same page ID and token value,
        /// using a case-insensitive token comparison.
        /// </summary>
        public override bool Equals(object? obj)
        {
            return obj is TwPageToken other
                && PageId == other.PageId
                && string.Equals(Token, other.Token, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Returns a hash code based on the page ID and lowercase token value.
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(PageId, Token.ToLowerInvariant());
        }
    }
}