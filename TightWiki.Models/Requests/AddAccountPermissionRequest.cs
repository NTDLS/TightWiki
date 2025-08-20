namespace TightWiki.Models.Requests
{
    public class AddAccountPermissionRequest
    {
        public Guid UserId { get; set; }
        public int PermissionId { get; set; }
        public string PermissionDispositionId { get; set; } = string.Empty;
        public string? Namespace { get; set; }
        public string? PageId { get; set; }
    }
}
