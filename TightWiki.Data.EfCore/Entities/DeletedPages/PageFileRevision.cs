using TightWiki.Data.EfCore.Entities.Users;

namespace TightWiki.Data.EfCore.Entities.DeletedPages
{
    /// <summary>
    /// A revision (byte content) of a file attachment that belonged to a page before it was soft-deleted
    /// (DeletedPages.PageFileRevision), moved here verbatim from Pages.PageFileRevision.
    /// </summary>
    public class PageFileRevision
    {
        /// <summary>
        /// The identifier of the file this revision belongs to. Part of the composite primary key together with
        /// <see cref="Revision"/>.
        /// </summary>
        public int PageFileId { get; set; }

        /// <summary>
        /// The case-insensitive MIME content type of this file revision.
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// The size of this file revision's <see cref="Data"/>, in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// The identifier of the user who uploaded this file revision. Modeled as Guid, matching
        /// Pages.PageFileRevision.CreatedByUserId (this table receives the exact same values, copied verbatim
        /// by PageRepository.MovePageToDeletedById) - see that entity's Fluent configuration for the full
        /// rationale.
        /// </summary>
        public Guid CreatedByUserId { get; set; }

        /// <summary>
        /// The date and time this file revision was uploaded.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// The profile of the user who uploaded this file revision (cross-schema navigation to Users.Profile,
        /// via <see cref="CreatedByUserId"/>). Optional - see the remarks on <see cref="Page.CreatedByUser"/>. No
        /// dedicated query exists for this table today, so unlike Pages.PageFileRevision.CreatedByUser, this
        /// isn't backed by an existing raw SQL join - added for model completeness/consistency per
        /// Database-Providers-Plan.md chapter 4.3.
        /// </summary>
        public Profile? CreatedByUser { get; set; }

        /// <summary>
        /// The raw byte content of this file revision.
        /// </summary>
        public byte[] Data { get; set; } = [];

        /// <summary>
        /// The revision number of this file revision. Part of the composite primary key together with
        /// <see cref="PageFileId"/>.
        /// </summary>
        public int Revision { get; set; }

        /// <summary>
        /// A hash of <see cref="Data"/> used to detect duplicate uploads.
        /// </summary>
        public int DataHash { get; set; }
    }
}
