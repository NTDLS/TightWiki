namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a user role in the wiki's access control system,
    /// grouping users together for the purpose of assigning shared permissions.
    /// </summary>
    public partial class TwRole
    {
        /// <summary>
        /// The unique identifier for this role.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The unique name of this role.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// A human-readable description of the purpose and permissions associated with this role.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether this role is a built-in system role that cannot be deleted or renamed.
        /// </summary>
        public bool IsBuiltIn { get; set; }
    }
}