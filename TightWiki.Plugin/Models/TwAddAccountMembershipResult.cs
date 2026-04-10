namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents the result of adding an account membership, including the role identifier and name.
    /// </summary>
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
