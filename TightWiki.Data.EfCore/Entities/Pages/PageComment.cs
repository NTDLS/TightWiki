using TightWiki.Data.EfCore.Entities.Users;

namespace TightWiki.Data.EfCore.Entities.Pages
{
    /// <summary>
    /// A user comment posted on a wiki page (Pages.PageComment).
    /// </summary>
    public class PageComment
    {
        /// <summary>
        /// The unique identifier for this comment.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The identifier of the page this comment was posted on.
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
        /// The page this comment was posted on.
        /// </summary>
        public Page Page { get; set; } = null!;

        /// <summary>
        /// The profile of the user who posted this comment (cross-schema navigation to Users.Profile, via
        /// <see cref="UserId"/>). Optional - see the remarks on <see cref="Page.CreatedByUser"/>.
        /// </summary>
        public Profile? User { get; set; }
    }
}
