using System.Collections.Generic;
using TightWiki.Shared.Models.Data;

namespace TightWiki.Library.ViewModels.Admin
{
    public class NamespacesViewModel : ViewModelBase
    {
        public List<NamespaceStat> Namespaces { get; set; } = new();
    }
}
