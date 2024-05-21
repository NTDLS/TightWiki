using TightWiki.Shared.Models.Data;

namespace TightWiki.Models.ViewModels.Admin
{
    public class NamespacesViewModel : ViewModelBase
    {
        public List<NamespaceStat> Namespaces { get; set; } = new();
    }
}
