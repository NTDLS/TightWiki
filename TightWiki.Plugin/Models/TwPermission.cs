namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a permission definition in the wiki's access control system,
    /// identifying a specific action that can be granted or denied to users and roles.
    /// </summary>
    public partial class TwPermission
    {
        /// <summary>
        /// The unique identifier for this permission.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The name of this permission, such as "Read", "Edit", or "Delete".
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }
}