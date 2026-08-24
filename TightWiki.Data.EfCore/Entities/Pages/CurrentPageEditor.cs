using TightWiki.Data.EfCore.Entities.Users;

namespace TightWiki.Data.EfCore.Entities.Pages
{
    /// <summary>
    /// Tracks a user who is currently editing a page, for the "someone else is editing this page" UI
    /// (Pages.CurrentPageEditors). Rows older than a sliding window are periodically purged by application code.
    /// </summary>
    public class CurrentPageEditor
    {
        /// <summary>
        /// The identifier of the page being edited. Part of the composite primary key together with
        /// <see cref="UserId"/>.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The identifier of the user editing the page. Part of the composite primary key together with
        /// <see cref="PageId"/>. Value-equal to (but not a formal foreign key against)
        /// <see cref="Users.Profile.UserId"/> - see <see cref="User"/>.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The account name of the editing user, denormalized here to avoid a join.
        /// </summary>
        public string AccountName { get; set; } = string.Empty;

        /// <summary>
        /// The profile of the user editing the page (cross-schema navigation to Users.Profile, via
        /// <see cref="UserId"/>). Optional - see the remarks on <see cref="Page.CreatedByUser"/>. Unlike the
        /// other cross-schema navigations in this schema, no existing raw SQL actually joins this column against
        /// Profile (AccountName is denormalized here instead, see the class remarks) - added for model
        /// completeness/consistency per Database-Providers-Plan.md chapter 4.3, not because a join needs it.
        /// </summary>
        public Profile? User { get; set; }

        /// <summary>
        /// The UTC date/time this editing session was last refreshed. Column name in the real schema is
        /// "UTCDate".
        /// </summary>
        public DateTime UtcDate { get; set; }
    }
}
