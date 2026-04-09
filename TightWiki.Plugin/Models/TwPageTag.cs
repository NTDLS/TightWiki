namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a single tag associated with a wiki page,
    /// used for categorization, search filtering, and related page discovery.
    /// </summary>
    public partial class TwPageTag
    {
        /// <summary>
        /// The unique identifier for this page tag record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The unique identifier of the page this tag is associated with.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The tag value assigned to the page.
        /// </summary>
        public string Tag { get; set; } = string.Empty;
    }
}