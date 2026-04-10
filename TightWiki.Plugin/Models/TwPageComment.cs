namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a user comment posted on a wiki page,
    /// including the comment body, authorship details, and the page it was posted on.
    /// </summary>
    public partial class TwPageComment
    {
        /// <summary>
        /// The unique identifier for this comment record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The unique identifier of the page this comment was posted on.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The unique identifier of the user who posted this comment.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The text content of this comment.
        /// </summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// The account name of the user who posted this comment.
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// The URL-safe navigation path to the profile of the user who posted this comment.
        /// </summary>
        public string UserNavigation { get; set; } = string.Empty;

        /// <summary>
        /// The full name of the page this comment was posted on.
        /// </summary>
        public string PageName { get; set; } = string.Empty;

        /// <summary>
        /// The date and time this comment was posted.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// The total number of pages available when paginating comment lists.
        /// </summary>
        public int PaginationPageCount { get; set; }
    }
}