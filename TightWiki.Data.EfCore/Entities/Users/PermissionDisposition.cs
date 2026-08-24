namespace TightWiki.Data.EfCore.Entities.Users
{
    /// <summary>
    /// A permission disposition type in the wiki's access control system, defining whether a permission record
    /// grants or denies access to an action (Users.PermissionDisposition). A fixed set of two rows ("Allow",
    /// "Deny") is seeded at startup - see
    /// Scripts/Initialization/Versions/2.26.0/^001^Users^PermissionDisposition.sql in TightWiki.Repository.
    /// </summary>
    public class PermissionDisposition
    {
        /// <summary>
        /// The unique identifier for this permission disposition.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The unique, case-insensitive name of this permission disposition, such as "Allow" or "Deny".
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The account-level permission records using this disposition.
        /// </summary>
        public ICollection<AccountPermission> AccountPermissions { get; set; } = [];

        /// <summary>
        /// The role-level permission records using this disposition.
        /// </summary>
        public ICollection<RolePermission> RolePermissions { get; set; } = [];
    }
}
