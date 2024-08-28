using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Admin
{
    public class NamespacesViewModel : ViewModelBase
    {
        public List<NamespaceStat> Namespaces { get; set; } = new();
        public int PaginationPageCount { get; set; }
    }
}
