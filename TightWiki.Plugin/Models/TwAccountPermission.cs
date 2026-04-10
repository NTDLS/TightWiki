namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents an account permission, including its scope, disposition, and pagination details.
    /// </summary>
    public partial class TwAccountPermission
    {
        /// <summary>
        /// The unique identifier for this account permission record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The name of the permission being granted or denied.
        /// </summary>
        public string Permission { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether this permission record allows or denies the action.
        /// </summary>
        public string PermissionDisposition { get; set; } = string.Empty;

        /// <summary>
        /// The name of the specific resource this permission applies to, or null if it applies globally.
        /// </summary>
        public string? ResourceName { get; set; }

        /// <summary>
        /// The namespace this permission is scoped to, or null if it applies across all namespaces.
        /// </summary>
        public string? Namespace { get; set; }

        /// <summary>
        /// The page ID this permission is scoped to, or null if it applies across all pages.
        /// </summary>
        public string? PageId { get; set; }

        /// <summary>
        /// The number of items per page used for paginating permission lists.
        /// </summary>
        public int PaginationPageSize { get; set; }

        /// <summary>
        /// The total number of pages available for paginating permission lists.
        /// </summary>
        public int PaginationPageCount { get; set; }
    }
}