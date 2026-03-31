using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.AdminSecurity
{
    public class AccountRolesViewModel
        : TwViewModel
    {
        public Guid Id { get; set; }

        public string AccountName { get; set; } = string.Empty;

        /// <summary>
        /// Permissions assigned to this role.
        /// </summary>
        public List<TwAccountPermission> AssignedPermissions { get; set; } = new();

        /// <summary>
        /// Members of this role.
        /// </summary>
        public List<TwAccountRoleMembership> Memberships { get; set; } = new();

        /// <summary>
        /// All available permissions.
        /// </summary>
        public List<TwPermission> Permissions { get; set; } = new();

        /// <summary>
        /// All available permission dispositions.
        /// </summary>
        public List<TwPermissionDisposition> PermissionDispositions { get; set; } = new();

        public int PaginationPageCount_Members { get; set; }
        public int PaginationPageCount_Permissions { get; set; }
    }
}
