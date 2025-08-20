namespace TightWiki.Models.Requests
{
    public class AddRolePermissionRequest
    {
        public int RoleId { get; set; }
        public int PermissionId { get; set; }
        public string PermissionDispositionId { get; set; } = string.Empty;
        public string? Namespace { get; set; }
        public string? PageId { get; set; }
    }
}
