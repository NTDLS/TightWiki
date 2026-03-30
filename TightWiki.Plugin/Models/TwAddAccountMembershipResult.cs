namespace TightWiki.Plugin.Models
{
    public class TwAddAccountMembershipResult
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
