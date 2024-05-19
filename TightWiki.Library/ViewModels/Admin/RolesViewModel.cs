using System.Collections.Generic;
using TightWiki.Library.DataModels;

namespace TightWiki.Library.ViewModels.Admin
{
    public class RolesViewModel : ViewModelBase
    {
        public List<Role> Roles { get; set; } = new();
    }
}
