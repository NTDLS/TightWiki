using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.AdminSecurity
{
    public class AccountRolesViewModel : ViewModelBase
    {
        public Guid Id { get; set; }

        public string AccountName { get; set; } = string.Empty;

        /// <summary>
        /// Permissions assigned to this role.
        /// </summary>
        public List<AccountPermission> AssignedPermissions { get; set; } = new();

        /// <summary>
        /// Members of this role.
        /// </summary>
        public List<AccountRoleMembership> Memberships { get; set; } = new();

        /// <summary>
        /// All available permissions.
        /// </summary>
        public List<Permission> Permissions { get; set; } = new();

        /// <summary>
        /// All available permission dispositions.
        /// </summary>
        public List<PermissionDisposition> PermissionDispositions { get; set; } = new();

        public int PaginationPageCount_Members { get; set; }
        public int PaginationPageCount_Permissions { get; set; }
    }
}
