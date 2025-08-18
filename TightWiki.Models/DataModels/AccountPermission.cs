namespace TightWiki.Models.DataModels
{
    public partial class AccountPermission
    {
        /// <summary>
        /// Id of the AccountPermission table.
        /// </summary>
        public int Id { get; set; }
        public string Permission { get; set; } = string.Empty;
        public string PermissionDisposition { get; set; } = string.Empty;
        public string? Namespace { get; set; }
        public string? PageId { get; set; }
    }
}
