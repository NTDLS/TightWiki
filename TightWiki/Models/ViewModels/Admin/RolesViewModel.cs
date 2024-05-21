using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Admin
{
    public class RolesViewModel : ViewModelBase
    {
        public List<Role> Roles { get; set; } = new();
    }
}
