namespace TightWiki.Data.EfCore.Entities.Pages
{
    /// <summary>
    /// A tag associated with a wiki page (Pages.PageTag).
    /// </summary>
    public class PageTag
    {
        /// <summary>
        /// The identifier of the tagged page. Part of the composite primary key together with <see cref="Tag"/>.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The case-insensitive tag text. Part of the composite primary key together with <see cref="PageId"/>.
        /// </summary>
        public string Tag { get; set; } = string.Empty;

        /// <summary>
        /// The case-insensitive, URL-safe navigation path for this tag, derived from <see cref="Tag"/>.
        /// </summary>
        public string Navigation { get; set; } = string.Empty;

        /// <summary>
        /// The tagged page.
        /// </summary>
        public Page Page { get; set; } = null!;
    }
}
