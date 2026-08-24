using TightWiki.Data.EfCore.Entities.Users;

namespace TightWiki.Data.EfCore.Entities.DeletedPages
{
    /// <summary>
    /// A comment that was posted on a page before it was soft-deleted (DeletedPages.PageComment), moved here
    /// verbatim from Pages.PageComment.
    /// </summary>
    public class PageComment
    {
        /// <summary>
        /// The identifier of this comment (copied verbatim from the original comment - not database-generated).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The identifier of the deleted page this comment was posted on.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The date and time this comment was posted.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// The identifier of the user who posted this comment. Value-equal to (but not a formal foreign key
        /// against) <see cref="Users.Profile.UserId"/> - see <see cref="User"/>.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The text content of this comment.
        /// </summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// The profile of the user who posted this comment (cross-schema navigation to Users.Profile, via
        /// <see cref="UserId"/>). Optional - see the remarks on <see cref="Page.CreatedByUser"/>. No dedicated
        /// query exists for this table today (comments on soft-deleted pages aren't surfaced anywhere), so
        /// unlike most of the other cross-schema navigations here, this isn't backed by an existing raw SQL
        /// join - added for model completeness/consistency per Database-Providers-Plan.md chapter 4.3.
        /// </summary>
        public Profile? User { get; set; }
    }
}
