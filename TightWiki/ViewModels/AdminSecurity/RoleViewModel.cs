using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.AdminSecurity
{
    public class RoleViewModel
        : TwViewModel
    {
        public int Id { get; set; }

        public bool IsBuiltIn { get; set; }

        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Permissions assigned to this role.
        /// </summary>
        public List<TwRolePermission> AssignedPermissions { get; set; } = new();

        /// <summary>
        /// Members of this role.
        /// </summary>
        public List<TwAccountProfile> Members { get; set; } = new();

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
