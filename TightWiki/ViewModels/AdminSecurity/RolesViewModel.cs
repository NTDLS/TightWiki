using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.AdminSecurity
{
    public class RolesViewModel
        : TwViewModel
    {
        public List<TwRole> Roles { get; set; } = new();
    }
}
