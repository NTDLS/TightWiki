namespace TightWiki.RequestModels
{
    public class AddAccountMembershipRequest
    {
        public int RoleId { get; set; }
        public Guid UserId { get; set; }
    }
}
