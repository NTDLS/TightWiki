namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a tag and the number of wiki pages associated with it,
    /// used for displaying tag clouds, related page discovery, and tag browsing.
    /// </summary>
    public class TwTagAssociation
    {
        /// <summary>
        /// The tag value shared across one or more wiki pages.
        /// </summary>
        public string Tag { get; set; } = string.Empty;

        /// <summary>
        /// The number of wiki pages that have this tag applied.
        /// </summary>
        public int PageCount { get; set; }
    }
}