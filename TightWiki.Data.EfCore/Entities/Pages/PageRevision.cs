using TightWiki.Data.EfCore.Entities.Users;

namespace TightWiki.Data.EfCore.Entities.Pages
{
    /// <summary>
    /// A single revision of a wiki page's content and metadata (Pages.PageRevision). The real schema declares no
    /// FOREIGN KEY constraint back to <see cref="Page"/> for <see cref="PageId"/>.
    /// </summary>
    public class PageRevision
    {
        /// <summary>
        /// The identifier of the page this revision belongs to. Part of the composite primary key together with
        /// <see cref="Revision"/>. Not modeled as a foreign key - see the Fluent configuration.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The fully-qualified name of the page at the time of this revision.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The namespace prefix of the page at the time of this revision, if any.
        /// </summary>
        public string? Namespace { get; set; }

        /// <summary>
        /// The description of the page at the time of this revision.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The raw wiki markup body at this revision.
        /// </summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// The revision number. Part of the composite primary key together with <see cref="PageId"/>.
        /// </summary>
        public int Revision { get; set; }

        /// <summary>
        /// A brief, editor-provided summary of the changes made in this revision.
        /// </summary>
        public string? ChangeSummary { get; set; }

        /// <summary>
        /// The identifier of the user who made this revision. Value-equal to (but not a formal foreign key
        /// against) <see cref="Users.Profile.UserId"/> - see <see cref="ModifiedByUser"/>.
        /// </summary>
        public Guid ModifiedByUserId { get; set; }

        /// <summary>
        /// The date and time this revision was made.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        /// <summary>
        /// The profile of the user who made this revision (cross-schema navigation to Users.Profile, via
        /// <see cref="ModifiedByUserId"/>). Optional - see the remarks on <see cref="Page.CreatedByUser"/>.
        /// </summary>
        public Profile? ModifiedByUser { get; set; }

        /// <summary>
        /// A hash of <see cref="Body"/> used to detect changes between revisions.
        /// </summary>
        public int DataHash { get; set; }
    }
}
