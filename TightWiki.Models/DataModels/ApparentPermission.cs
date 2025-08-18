namespace TightWiki.Models.DataModels
{
    /// <summary>
    /// Model used to obtain the permissions for a user account which will be used to determine what the user can do.
    /// </summary>
    public partial class ApparentPermission
    {
        public string Permission { get; set; } = string.Empty;
        public string PermissionDisposition { get; set; } = string.Empty;
        public string? Namespace { get; set; }
        public string? PageId { get; set; }
    }
}
