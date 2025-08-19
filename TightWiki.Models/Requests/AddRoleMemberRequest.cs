namespace TightWiki.Models.Requests
{
    public class AddRoleMemberRequest
    {
        public int RoleId { get; set; }
        public Guid UserId { get; set; }
    }
}
