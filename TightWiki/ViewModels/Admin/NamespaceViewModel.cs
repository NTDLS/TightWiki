using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Admin
{
    public class NamespaceViewModel
        : TwViewModel
    {
        public List<TwPage> Pages { get; set; } = new();
        public string Namespace { get; set; } = string.Empty;
        public int PaginationPageCount { get; set; }
    }
}
