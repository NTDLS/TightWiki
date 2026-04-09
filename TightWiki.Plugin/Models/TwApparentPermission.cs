namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents the effective permission for a user account, combining direct account permissions
    /// and inherited role permissions, used to determine what actions the user is allowed to perform.
    /// </summary>
    public partial class TwApparentPermission
    {
        /// <summary>
        /// The name of the permission being granted or denied.
        /// </summary>
        public string Permission { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether this permission record allows or denies the action.
        /// </summary>
        public string PermissionDisposition { get; set; } = string.Empty;

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