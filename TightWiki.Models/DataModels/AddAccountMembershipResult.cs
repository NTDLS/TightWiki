namespace TightWiki.Models.DataModels
{
    public class AddAccountMembershipResult
    {
        /// <summary>
        /// Id of the AccountRole table.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Name of the role.
        /// </summary>
        public string Name { get; set; } = string.Empty;//Account navigation
    }
}
