using TightWiki.Plugin.Models;

namespace TightWiki.Models.ViewModels.Admin
{
    public class NamespacesViewModel
        : ViewModelBase
    {
        public List<TwNamespaceStat> Namespaces { get; set; } = new();
        public int PaginationPageCount { get; set; }
    }
}
