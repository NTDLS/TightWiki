namespace TightWiki.Data.EfCore.Entities.DeletedPages
{
    /// <summary>
    /// A tag that belonged to a page before it was soft-deleted (DeletedPages.PageTag), moved here (and kept
    /// searchable) from Pages.PageTag. Unlike Pages.PageTag, this table has no <c>Navigation</c> column.
    /// </summary>
    public class PageTag
    {
        /// <summary>
        /// The identifier of the deleted page. Part of the composite primary key together with <see cref="Tag"/>.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The case-insensitive tag text. Part of the composite primary key together with <see cref="PageId"/>.
        /// </summary>
        public string Tag { get; set; } = string.Empty;
    }
}
