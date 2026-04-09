namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a permission disposition type in the wiki's access control system,
    /// defining whether a permission record grants or denies access to an action.
    /// </summary>
    public partial class TwPermissionDisposition
    {
        /// <summary>
        /// The unique identifier for this permission disposition.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The name of this permission disposition, such as "Allow" or "Deny".
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }
}