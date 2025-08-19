namespace TightWiki.Models.DataModels
{
    public class AddRoleMemberResult
    {
        public int Id { get; set; }  //ID of the role membership.
        public Guid UserId { get; set; }
        public string Navigation { get; set; } = string.Empty;//Account navigation
        public string AccountName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
}
