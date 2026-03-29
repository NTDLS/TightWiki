using TightWiki.Plugin.Models;

namespace TightWiki.Models.ViewModels.AdminSecurity
{
    public class RolesViewModel
        : ViewModelBase
    {
        public List<TwRole> Roles { get; set; } = new();
    }
}
