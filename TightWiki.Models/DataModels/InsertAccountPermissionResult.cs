namespace TightWiki.Models.DataModels
{
    public class InsertAccountPermissionResult
    {
        /// <summary>
        /// Id of the RolePermission table.
        /// </summary>
        public int Id { get; set; }
        public string Permission { get; set; } = string.Empty;
        public string PermissionDisposition { get; set; } = string.Empty;
        public string? ResourceName { get; set; }
        public string? Namespace { get; set; }
        public string? PageId { get; set; }
    }
}
