namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents the result of inserting a new account permission record,
    /// returning the identity and details of the permission that was created.
    /// </summary>
    public class TwInsertAccountPermissionResult
    {
        /// <summary>
        /// The unique identifier of the account permission record that was created.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The name of the permission that was granted or denied.
        /// </summary>
        public string Permission { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the permission record allows or denies the action.
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
    }
}