namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a membership record for an account role, including role identification and pagination details for
    /// membership lists.
    /// </summary>
    public class TwAccountRoleMembership
    {
        /// <summary>
        /// The unique identifier for this account role membership record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The name of the role this membership record refers to.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The unique identifier of the role this membership record refers to.
        /// </summary>
        public int RoleId { get; set; }

        /// <summary>
        /// The number of items per page used when paginating membership lists.
        /// </summary>
        public int PaginationPageSize { get; set; }

        /// <summary>
        /// The total number of pages available when paginating membership lists.
        /// </summary>
        public int PaginationPageCount { get; set; }
    }
}