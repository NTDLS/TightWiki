namespace TightWiki.Data.EfCore.Entities.DeletedPages
{
    /// <summary>
    /// Associates a deleted file revision with a specific deleted page revision (DeletedPages.
    /// PageRevisionAttachment), moved here verbatim from Pages.PageRevisionAttachment.
    /// </summary>
    public class PageRevisionAttachment
    {
        /// <summary>
        /// The identifier of the deleted page. Part of the composite primary key.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The identifier of the deleted file attachment. Part of the composite primary key.
        /// </summary>
        public int PageFileId { get; set; }

        /// <summary>
        /// The file revision number that was attached. Part of the composite primary key.
        /// </summary>
        public int FileRevision { get; set; }

        /// <summary>
        /// The page revision number the file was attached at. Part of the composite primary key.
        /// </summary>
        public int PageRevision { get; set; }
    }
}
