namespace TightWiki.Data.EfCore.Entities.DeletedPages
{
    /// <summary>
    /// A file attachment that belonged to a page before it was soft-deleted (DeletedPages.PageFile), moved here
    /// verbatim from Pages.PageFile.
    /// </summary>
    public class PageFile
    {
        /// <summary>
        /// The identifier of this file attachment (copied verbatim from the original - not database-generated).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The identifier of the deleted page this file was attached to.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The original, case-insensitive file name of the attachment.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The case-insensitive navigation path of the file attachment.
        /// </summary>
        public string Navigation { get; set; } = string.Empty;

        /// <summary>
        /// The revision number of the file attachment at the time of deletion.
        /// </summary>
        public int Revision { get; set; }

        /// <summary>
        /// The date and time this file attachment was first uploaded.
        /// </summary>
        public DateTime CreatedDate { get; set; }
    }
}
