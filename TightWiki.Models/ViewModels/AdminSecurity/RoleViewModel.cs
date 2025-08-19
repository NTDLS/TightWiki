using TightWiki.Models.DataModels;
using static TightWiki.Library.Constants;

namespace TightWiki.Models.ViewModels.AdminSecurity
{
    public class RoleViewModel : ViewModelBase
    {
        public int Id { get; set; }

        public bool IsBuiltIn { get; set; }

        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Permissions assigned to this role.
        /// </summary>
        public List<RolePermission> AssignedPermissions { get; set; } = new();

        /// <summary>
        /// Members of this role.
        /// </summary>
        public List<AccountProfile> Members { get; set; } = new();

        /// <summary>
        /// All available permissions.
        /// </summary>
        public List<Permission> Permissions { get; set; } = new();

        /// <summary>
        /// All available permission dispositions.
        /// </summary>
        public List<PermissionDisposition> PermissionDispositions { get; set; } = new();

        public int PaginationPageCount { get; set; }
    }
}
