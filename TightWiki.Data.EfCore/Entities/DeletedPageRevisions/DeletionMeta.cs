using TightWiki.Data.EfCore.Entities.Users;

namespace TightWiki.Data.EfCore.Entities.DeletedPageRevisions
{
    /// <summary>
    /// Deletion metadata (who/when) for a single soft-deleted page revision (DeletedPageRevisions.DeletionMeta).
    /// One row per deleted <see cref="PageRevision"/>.
    /// </summary>
    public class DeletionMeta
    {
        /// <summary>
        /// The identifier of the page the deleted revision belonged to. Part of the composite primary key
        /// together with <see cref="Revision"/>.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The revision number that was deleted. Part of the composite primary key together with
        /// <see cref="PageId"/>.
        /// </summary>
        public int Revision { get; set; }

        /// <summary>
        /// The identifier of the user who deleted the revision. Nullable in the real schema. Modeled as Guid
        /// rather than the raw scaffold's int? - see the Fluent configuration. Value-equal to (but not a formal
        /// foreign key against) <see cref="Users.Profile.UserId"/> - see <see cref="DeletedByUser"/>.
        /// </summary>
        public Guid? DeletedByUserId { get; set; }

        /// <summary>
        /// The date and time the revision was deleted. Nullable in the real schema. Modeled as DateTime rather
        /// than the raw scaffold's int? - see the Fluent configuration.
        /// </summary>
        public DateTime? DeletedDate { get; set; }

        /// <summary>
        /// The profile of the user who deleted the revision (cross-schema navigation to Users.Profile, via
        /// <see cref="DeletedByUserId"/>). Optional - <see cref="DeletedByUserId"/> is itself nullable, and the
        /// raw SQL this mirrors (GetDeletedPageRevisionById.sql/GetDeletedPageRevisionsByIdPaged.sql) LEFT OUTER
        /// JOINs Profile.
        /// </summary>
        public Profile? DeletedByUser { get; set; }
    }
}
