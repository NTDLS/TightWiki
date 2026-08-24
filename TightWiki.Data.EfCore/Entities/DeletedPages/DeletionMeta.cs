using TightWiki.Data.EfCore.Entities.Users;

namespace TightWiki.Data.EfCore.Entities.DeletedPages
{
    /// <summary>
    /// Deletion metadata (who/when) for a soft-deleted page (DeletedPages.DeletionMeta). One row per deleted
    /// <see cref="Page"/>, keyed by the page's original identifier.
    /// </summary>
    public class DeletionMeta
    {
        /// <summary>
        /// The identifier of the deleted page (copied verbatim from the page's original identifier in the Pages
        /// schema - not database-generated).
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The identifier of the user who deleted the page. Nullable in the real schema. Modeled as Guid rather
        /// than the raw scaffold's int? - see the Fluent configuration. Value-equal to (but not a formal foreign
        /// key against) <see cref="Users.Profile.UserId"/> - see <see cref="DeletedByUser"/>.
        /// </summary>
        public Guid? DeletedByUserId { get; set; }

        /// <summary>
        /// The date and time the page was deleted. Nullable in the real schema. Modeled as DateTime rather than
        /// the raw scaffold's int? - see the Fluent configuration.
        /// </summary>
        public DateTime? DeletedDate { get; set; }

        /// <summary>
        /// The profile of the user who deleted the page (cross-schema navigation to Users.Profile, via
        /// <see cref="DeletedByUserId"/>). Optional - <see cref="DeletedByUserId"/> is itself nullable, and the
        /// raw SQL this mirrors (e.g. GetAllDeletedPagesPaged.sql) LEFT OUTER JOINs Profile.
        /// </summary>
        public Profile? DeletedByUser { get; set; }
    }
}
