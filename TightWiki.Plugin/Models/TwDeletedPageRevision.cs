namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a soft-deleted page revision, extending <see cref="TwPage"/> with
    /// pagination metadata for use in paged lists of deleted revisions.
    /// </summary>
    public class TwDeletedPageRevision
        : TwPage
    {
        /// <summary>
        /// The number of items per page used when paginating deleted revision lists.
        /// </summary>
        public int PaginationPageSize { get; set; }
    }
}