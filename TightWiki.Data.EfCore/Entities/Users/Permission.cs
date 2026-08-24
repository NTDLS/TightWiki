namespace TightWiki.Data.EfCore.Entities.Users
{
    /// <summary>
    /// A permission definition in the wiki's access control system, identifying a specific action that can be
    /// granted or denied to users and roles (Users.Permission). A fixed set of five rows ("Read", "Edit",
    /// "Delete", "Moderate", "Create") is seeded at startup - see
    /// Scripts/Initialization/Versions/2.26.0/^003^Users^Permission.sql in TightWiki.Repository.
    /// </summary>
    public class Permission
    {
        /// <summary>
        /// The unique identifier for this permission.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The unique, case-insensitive name of this permission, such as "Read", "Edit", or "Delete".
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// A human-readable description of this permission, or null if not provided.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The account-level permission records granting or denying this permission.
        /// </summary>
        public ICollection<AccountPermission> AccountPermissions { get; set; } = [];

        /// <summary>
        /// The role-level permission records granting or denying this permission.
        /// </summary>
        public ICollection<RolePermission> RolePermissions { get; set; } = [];
    }
}
