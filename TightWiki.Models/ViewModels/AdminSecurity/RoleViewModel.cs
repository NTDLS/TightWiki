using TightWiki.Models.DataModels;
using static TightWiki.Library.Constants;

namespace TightWiki.Models.ViewModels.AdminSecurity
{
    public class RoleViewModel : ViewModelBase
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Permissions assigned to this role.
        /// </summary>
        public List<RolePermission> AssignedPermissions { get; set; } = new();

        /// <summary>
        /// Members of this role.
        /// </summary>
        public List<AccountProfile> Users { get; set; } = new();

        /// <summary>
        /// All available permissions.
        /// </summary>
        public List<WikiPermission> Permissions { get; set; } = new();

        /// <summary>
        /// All available permission dispositions.
        /// </summary>
        public List<PermissionDisposition> PermissionDispositions { get; set; } = new();

        public int PaginationPageCount { get; set; }
    }
}
