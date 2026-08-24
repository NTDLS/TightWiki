namespace TightWiki.Data.EfCore.Entities.Users
{
    /// <summary>
    /// A user role in the wiki's access control system, grouping users together for the purpose of assigning
    /// shared permissions (Users.Role). Five built-in roles (Administrator, Member, Contributor, Moderator,
    /// Anonymous) are seeded at startup - see
    /// Scripts/Initialization/Versions/2.26.0/^002^Users^Role.sql in TightWiki.Repository.
    /// </summary>
    public class Role
    {
        /// <summary>
        /// The unique identifier for this role.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The unique, case-insensitive name of this role.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// A human-readable description of the purpose and permissions associated with this role, or null if not
        /// provided.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Indicates whether this role is a built-in system role that cannot be deleted or renamed.
        /// </summary>
        public bool IsBuiltIn { get; set; }

        /// <summary>
        /// The account role memberships (user &lt;-&gt; role assignments) referencing this role.
        /// </summary>
        public ICollection<AccountRole> AccountRoles { get; set; } = [];

        /// <summary>
        /// The permission records assigned to this role.
        /// </summary>
        public ICollection<RolePermission> RolePermissions { get; set; } = [];
    }
}
