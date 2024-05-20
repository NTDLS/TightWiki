using System.Collections.Generic;
using TightWiki.DataModels;

namespace TightWiki.ViewModels.Admin
{
    public class RolesViewModel : ViewModelBase
    {
        public List<Role> Roles { get; set; } = new();
    }
}
