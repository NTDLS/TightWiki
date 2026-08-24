namespace TightWiki.Data.EfCore.Entities.DeletedPageRevisions
{
    /// <summary>
    /// Associates a file revision with a specific soft-deleted page revision (DeletedPageRevisions.
    /// PageRevisionAttachment), moved here verbatim from Pages.PageRevisionAttachment by
    /// PageRepository.MovePageRevisionToDeletedById.
    /// </summary>
    public class PageRevisionAttachment
    {
        /// <summary>
        /// The identifier of the page. Part of the composite primary key.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The identifier of the attached file. Part of the composite primary key.
        /// </summary>
        public int PageFileId { get; set; }

        /// <summary>
        /// The file revision number that was attached. Part of the composite primary key.
        /// </summary>
        public int FileRevision { get; set; }

        /// <summary>
        /// The (deleted) page revision number the file was attached at. Part of the composite primary key.
        /// </summary>
        public int PageRevision { get; set; }
    }
}
