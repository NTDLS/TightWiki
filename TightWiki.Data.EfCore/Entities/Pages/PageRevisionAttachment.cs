namespace TightWiki.Data.EfCore.Entities.Pages
{
    /// <summary>
    /// Associates a <see cref="PageFile"/> revision with a specific page revision (Pages.PageRevisionAttachment).
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
        /// The page revision number the file was attached at. Part of the composite primary key.
        /// </summary>
        public int PageRevision { get; set; }

        /// <summary>
        /// The page this attachment association belongs to.
        /// </summary>
        public Page Page { get; set; } = null!;

        /// <summary>
        /// The file this attachment association references.
        /// </summary>
        public PageFile PageFile { get; set; } = null!;
    }
}
