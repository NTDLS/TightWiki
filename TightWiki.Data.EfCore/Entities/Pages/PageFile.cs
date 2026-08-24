namespace TightWiki.Data.EfCore.Entities.Pages
{
    /// <summary>
    /// A file attached to a wiki page (Pages.PageFile). The actual byte content lives per-revision in
    /// <see cref="PageFileRevision"/>.
    /// </summary>
    public class PageFile
    {
        /// <summary>
        /// The unique identifier for this file attachment.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The identifier of the page this file is attached to.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The original, case-insensitive file name of the attachment.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The case-insensitive, URL-safe navigation path used to locate this file attachment.
        /// </summary>
        public string Navigation { get; set; } = string.Empty;

        /// <summary>
        /// The current revision number of this file attachment.
        /// </summary>
        public int Revision { get; set; }

        /// <summary>
        /// The date and time this file attachment was first uploaded.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// The page this file is attached to.
        /// </summary>
        public Page Page { get; set; } = null!;

        /// <summary>
        /// The revisions (byte content) of this file attachment.
        /// </summary>
        public ICollection<PageFileRevision> PageFileRevisions { get; set; } = [];

        /// <summary>
        /// The page-revision associations for this file attachment.
        /// </summary>
        public ICollection<PageRevisionAttachment> PageRevisionAttachments { get; set; } = [];
    }
}
