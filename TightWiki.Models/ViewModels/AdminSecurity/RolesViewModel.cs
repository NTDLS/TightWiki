using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.AdminSecurity
{
    public class RolesViewModel : ViewModelBase
    {
        public List<Role> Roles { get; set; } = new();
    }
}
