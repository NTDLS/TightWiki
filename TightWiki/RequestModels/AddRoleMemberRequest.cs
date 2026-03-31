namespace TightWiki.RequestModels
{
    public class AddRoleMemberRequest
    {
        public int RoleId { get; set; }
        public Guid UserId { get; set; }
    }
}
