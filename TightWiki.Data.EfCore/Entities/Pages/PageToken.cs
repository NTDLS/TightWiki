namespace TightWiki.Data.EfCore.Entities.Pages
{
    /// <summary>
    /// A single search token extracted from a wiki page's content, used for full-text and fuzzy search
    /// (Pages.PageToken).
    /// </summary>
    public class PageToken
    {
        /// <summary>
        /// The identifier of the page this token was extracted from. Part of the composite primary key together
        /// with <see cref="Token"/>.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The case-insensitive search token text. Part of the composite primary key together with
        /// <see cref="PageId"/>.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// The weight assigned to this token, used for relevance scoring in search results.
        /// </summary>
        public double Weight { get; set; }

        /// <summary>
        /// The case-insensitive Double Metaphone phonetic encoding of the token, used for fuzzy/sound-alike
        /// matching.
        /// </summary>
        public string DoubleMetaphone { get; set; } = string.Empty;

        /// <summary>
        /// The page this token was extracted from.
        /// </summary>
        public Page Page { get; set; } = null!;
    }
}
